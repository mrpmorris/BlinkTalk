using BlinkTalk.Application;
using BlinkTalk.Application.Abstractions;
using BlinkTalk.Application.Input;
using BlinkTalk.Application.Persistence;
using BlinkTalk.Application.Text;
using BlinkTalk.Resources;
using BlinkTalk.Services;
using Microsoft.AspNetCore.Components;
using System.Globalization;

namespace BlinkTalk.Components.Pages;

public partial class Settings
{
	/// <summary>
	/// The languages offered in the dropdown, ordered by the name each one calls itself so the list
	/// reads to the person choosing rather than to us. Taken from
	/// <see cref="AppLanguage.OfferedCultureCodes"/> — which is the set the app is translated into,
	/// region entries and all — rather than by filtering every culture the device knows: enumerating
	/// cannot tell a region we translated from one that merely shares a word list, so "pt-BR" was
	/// dropped either by <see cref="CultureTypes.NeutralCultures"/> excluding it or by grouping it in
	/// with "pt". Codes this device does not recognise fall out here.
	/// </summary>
	private static readonly IReadOnlyList<CultureInfo> Languages =
		AppLanguage.OfferedCultureCodes
			.Select(AppLanguage.FindSupported)
			.OfType<CultureInfo>()
			.OrderBy(culture => culture.NativeName, StringComparer.CurrentCultureIgnoreCase)
			.ToList();

	private static readonly IReadOnlyList<KeyboardLayoutStyle> KeyboardLayoutStyles =
		Enum.GetValues<KeyboardLayoutStyle>();

	private double ScanSpeed { get; set; }

	/// <summary>
	/// The voices installed for the current language, reloaded whenever that language changes. Empty
	/// on a device whose engine reports nothing nameable, which is normal on stock Android — the
	/// dropdown then offers the system default alone.
	/// </summary>
	private IReadOnlyList<SpeechVoiceOption> Voices { get; set; } = Array.Empty<SpeechVoiceOption>();

	/// <summary>Empty string is the system-default option, matching <c>&lt;option value=""&gt;</c>.</summary>
	private string SelectedVoiceId { get; set; } = string.Empty;

	/// <summary>
	/// Written through to settings as it changes, like the scan speed: there is no database to seed
	/// off the back of it, so there is nothing to defer until the person leaves.
	/// </summary>
	private KeyboardLayoutStyle SelectedKeyboardLayout
	{
		get => KeyboardLayouts.Style;
		set => KeyboardLayouts.Style = value;
	}

	/// <summary>
	/// Read from the culture rather than the keyboard: this page switches language as soon as the
	/// dropdown changes, before the keyboard for it has been asked for.
	/// </summary>
	private string TextDirection => AppLanguage.Current.TextInfo.IsRightToLeft ? "rtl" : "ltr";

	private string SelectedLanguage { get; set; } = string.Empty;

	private bool IsDownloading { get; set; }

	private double? DownloadProgress { get; set; }

	private string? DownloadError { get; set; }

	private CancellationTokenSource? DownloadCts;

	/// <summary>
	/// The language on the way in, so leaving can tell whether it changed. A different language means
	/// a different alphabet and a different dictionary, so whatever was being composed is thrown away.
	/// </summary>
	private Language LanguageOnEntry;

	private readonly CameraIndicatorConfig Camera;
	private readonly ScanController Controller;
	private readonly AppDatabase Database;
	private readonly LanguagePackDownloader Downloader;
	private readonly IKeyboardLayoutProvider KeyboardLayouts;
	private readonly NavigationManager Navigation;
	private readonly IDatabaseProvisioner Provisioner;
	private readonly ISettingsStore SettingsStore;
	private readonly ITextToSpeechService Speech;

	public Settings(ScanController controller, CameraIndicatorConfig camera, AppDatabase database, NavigationManager navigation, ISettingsStore settingsStore, LanguagePackDownloader downloader, IDatabaseProvisioner provisioner, IKeyboardLayoutProvider keyboardLayouts, ITextToSpeechService speech)
	{
		Speech = speech;
		Controller = controller;
		Camera = camera;
		Database = database;
		Navigation = navigation;
		SettingsStore = settingsStore;
		Downloader = downloader;
		Provisioner = provisioner;
		KeyboardLayouts = keyboardLayouts;
	}

	protected override void OnInitialized()
	{
		ScanSpeed = Controller.CycleDelaySeconds;
		LanguageOnEntry = AppLanguage.Name;

		// Full code before partial: an entry for the running culture itself wins over one that merely
		// shares its language, so a region-specific option is preselected rather than its neutral sibling.
		CultureInfo current = AppLanguage.Current;
		SelectedLanguage = (Languages.FirstOrDefault(culture => culture.Name == current.Name)
			?? Languages.FirstOrDefault(culture => culture.TwoLetterISOLanguageName == current.TwoLetterISOLanguageName))
			?.Name ?? string.Empty;
	}

	/// <summary>
	/// The voice list has to be asked of the platform engine, so it cannot be loaded in
	/// <see cref="OnInitialized"/>. The page renders once without it and again once it arrives, which
	/// is fine: the dropdown shows the system-default option in the meantime.
	/// </summary>
	protected override Task OnInitializedAsync() => LoadVoicesAsync();

	/// <summary>
	/// Reads the voices for whichever language the app is now running in, and the choice stored for
	/// it. A voice stored for a language whose pack has since been removed is dropped back to the
	/// system default here, rather than being shown as a selection the dropdown has no option for.
	/// </summary>
	private async Task LoadVoicesAsync()
	{
		Voices = await Speech.GetVoicesForCurrentLanguageAsync();
		string? stored = Speech.SelectedVoiceId;
		SelectedVoiceId = stored is not null && Voices.Any(voice => voice.Id == stored)
			? stored
			: string.Empty;
	}

	/// <summary>
	/// Written through as it changes, like the keyboard layout: there is no database to seed off the
	/// back of it, so there is nothing to defer until the person leaves. Speaking the sample straight
	/// away is the point of choosing — they hear what they picked without hunting for a Test button.
	/// </summary>
	private async Task OnVoiceChangedAsync()
	{
		Speech.SelectedVoiceId = SelectedVoiceId.Length == 0 ? null : SelectedVoiceId;
		await TestVoiceAsync();
	}

	private Task TestVoiceAsync() => Speech.SpeakAsync(Localization.Settings_Voice_SampleText);

	/// <summary>
	/// Leaving settings is where the language choice is persisted, and where the database for it is
	/// opened and migrated — not when the language changes, because each change would seed a
	/// dictionary the person may be about to change their mind about. Landing on the typing page
	/// needs word suggestions, so this is the last moment it can happen, and the person is already
	/// expecting the page to change.
	/// </summary>
	private async Task GoBackAsync()
	{
		if (IsDownloading)
			return;

		AppLanguage.Persist(SettingsStore);

		if (Provisioner.DatabaseExists())
		{
			Database.OpenForCurrentLanguage();
			ReturnToTyping();
			return;
		}

		if (await DownloadAndSeedAsync())
			ReturnToTyping();
	}

	/// <summary>
	/// Downloads the language pack for the current language into memory and seeds a new database
	/// from it, behind a modal with a progress bar. Returns false — leaving the person on this
	/// page — if the download is cancelled or fails.
	/// </summary>
	private async Task<bool> DownloadAndSeedAsync()
	{
		IsDownloading = true;
		DownloadProgress = null;
		DownloadError = null;
		using var cts = new CancellationTokenSource();
		DownloadCts = cts;
		try
		{
			// Progress<T> posts to the captured sync context; InvokeAsync keeps the render on the
			// UI thread per the app's threading rule.
			var progress = new Progress<double?>(fraction =>
			{
				DownloadProgress = fraction;
				_ = InvokeAsync(StateHasChanged);
			});
			byte[] zip = await Downloader.DownloadAsync(AppLanguage.Name, progress, cts.Token);

			// Seeding inserts tens of thousands of rows; keep it off the UI thread so the modal
			// stays responsive.
			await Task.Run(() => Database.OpenForCurrentLanguage(new InMemoryZipSeedWordSource(zip)));

			IsDownloading = false;
			return true;
		}
		catch (OperationCanceledException)
		{
			IsDownloading = false;
			return false;
		}
		catch
		{
			// The seed transaction rolls back on failure, but delete the file too so an empty
			// database is not mistaken for an installed language pack next time.
			Database.CloseCurrent();
			if (Provisioner.DatabaseExists())
				File.Delete(Provisioner.GetDatabasePath());
			DownloadError = Localization.Settings_LanguagePackDownloadFailed;
			return false;
		}
		finally
		{
			DownloadCts = null;
		}
	}

	private void CancelDownload() => DownloadCts?.Cancel();

	private void CloseDownloadModal()
	{
		IsDownloading = false;
		DownloadError = null;
	}

	private void GoToCamera() => Navigation.NavigateTo("/camera");

	/// <summary>
	/// Switches language as soon as the dropdown changes, so the person sees this page in the
	/// language they just picked and can tell they picked the right one. The choice is only written
	/// to settings when they leave — see <see cref="GoBack"/>.
	/// <para>
	/// The voice list is reloaded because it is language-specific: the voices below the dropdown are
	/// the ones that can speak the language above it.
	/// </para>
	/// </summary>
	private async Task OnLanguageChangedAsync()
	{
		CultureInfo? culture = Languages.FirstOrDefault(language => language.Name == SelectedLanguage);
		if (culture is not null)
			AppLanguage.SetCurrent(culture);

		await LoadVoicesAsync();
	}

	/// <summary>
	/// The controller outlives this page, and its scan levels are still holding the row and key
	/// counts of the keyboard for the language we may have just left — restart it so it picks up the
	/// new keyboard, and the suggestions from the new language's dictionary.
	/// <para>
	/// Clearing comes before the restart, which reloads suggestions: a sentence half-composed in the
	/// language just left cannot be finished in the new one, and it would be predicted against a
	/// dictionary that has never seen its words.
	/// </para>
	/// </summary>
	private void ReturnToTyping()
	{
		if (AppLanguage.Name != LanguageOnEntry)
			Controller.Sentence.Clear();
		Controller.Restart();
		Navigation.NavigateTo("/type");
	}

	private static string LayoutLabel(KeyboardLayoutStyle style) => style switch
	{
		KeyboardLayoutStyle.Speed => Localization.Settings_KeyboardLayout_Speed,
		_ => Localization.Settings_KeyboardLayout_Alphabetical
	};

	private void OnScanSpeedChanged(ChangeEventArgs e)
	{
		if (double.TryParse(e.Value?.ToString(), CultureInfo.InvariantCulture, out double seconds))
		{
			ScanSpeed = seconds;
			Controller.CycleDelaySeconds = seconds;
		}
	}
}