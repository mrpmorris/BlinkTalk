using BlinkTalk.Application.Abstractions;
using BlinkTalk.Application.Input;
using BlinkTalk.Services;
using Microsoft.AspNetCore.Components;
using System.Globalization;

namespace BlinkTalk.Components.Pages;

public partial class Settings
{
	/// <summary>
	/// The languages offered in the dropdown: every neutral culture this device knows about that
	/// the app has a word list for. Neutral cultures only — the choice is a language, not a region
	/// — and one entry per language, because a language can have more than one neutral culture
	/// (script variants such as "sr-Cyrl" / "sr-Latn").
	/// </summary>
	private static readonly IReadOnlyList<CultureInfo> Languages =
		CultureInfo.GetCultures(CultureTypes.NeutralCultures)
			.Where(AppLanguage.IsSupported)
			.GroupBy(culture => culture.TwoLetterISOLanguageName)
			.Select(group => group.First())
			.OrderBy(culture => culture.NativeName, StringComparer.CurrentCultureIgnoreCase)
			.ToList();

	private double ScanSpeed { get; set; }

	private string SelectedLanguage { get; set; } = string.Empty;

	private readonly CameraIndicatorConfig Camera;
	private readonly ScanController Controller;
	private readonly AppDatabase Database;
	private readonly NavigationManager Navigation;
	private readonly ISettingsStore SettingsStore;

	public Settings(ScanController controller, CameraIndicatorConfig camera, AppDatabase database, NavigationManager navigation, ISettingsStore settingsStore)
	{
		Controller = controller;
		Camera = camera;
		Database = database;
		Navigation = navigation;
		SettingsStore = settingsStore;
	}

	protected override void OnInitialized()
	{
		ScanSpeed = Controller.CycleDelaySeconds;

		// Full code before partial: an entry for the running culture itself wins over one that merely
		// shares its language, so a region-specific option is preselected rather than its neutral sibling.
		CultureInfo current = AppLanguage.Current;
		SelectedLanguage = (Languages.FirstOrDefault(culture => culture.Name == current.Name)
			?? Languages.FirstOrDefault(culture => culture.TwoLetterISOLanguageName == current.TwoLetterISOLanguageName))
			?.Name ?? string.Empty;
	}

	/// <summary>
	/// Leaving settings is where the language choice is persisted, and where the database for it is
	/// opened and migrated — not when the language changes, because each change would seed a
	/// dictionary the person may be about to change their mind about. Landing on the typing page
	/// needs word suggestions, so this is the last moment it can happen, and the person is already
	/// expecting the page to change.
	/// </summary>
	private void GoBack()
	{
		AppLanguage.Persist(SettingsStore);
		Database.OpenForCurrentLanguage();
		Navigation.NavigateTo("/type");
	}

	private void GoToCamera() => Navigation.NavigateTo("/camera");

	/// <summary>
	/// Switches language as soon as the dropdown changes, so the person sees this page in the
	/// language they just picked and can tell they picked the right one. The choice is only written
	/// to settings when they leave — see <see cref="GoBack"/>.
	/// </summary>
	private void OnLanguageChanged()
	{
		CultureInfo? culture = Languages.FirstOrDefault(language => language.Name == SelectedLanguage);
		if (culture is not null)
			AppLanguage.SetCurrent(culture);
	}

	private void OnScanSpeedChanged(ChangeEventArgs e)
	{
		if (double.TryParse(e.Value?.ToString(), CultureInfo.InvariantCulture, out double seconds))
		{
			ScanSpeed = seconds;
			Controller.CycleDelaySeconds = seconds;
		}
	}
}