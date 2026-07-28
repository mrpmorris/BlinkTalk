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

	public Settings(ScanController controller, CameraIndicatorConfig camera, AppDatabase database, NavigationManager navigation)
	{
		Controller = controller;
		Camera = camera;
		Database = database;
		Navigation = navigation;
	}

	protected override void OnInitialized()
	{
		ScanSpeed = Controller.CycleDelaySeconds;
		SelectedLanguage = Languages
			.FirstOrDefault(culture => culture.TwoLetterISOLanguageName == CultureInfo.CurrentUICulture.TwoLetterISOLanguageName)
			?.Name ?? string.Empty;
	}

	/// <summary>
	/// Leaving settings is where the database for the chosen language is opened and migrated — not
	/// when the language changes, because each change would seed a dictionary the person may be
	/// about to change their mind about. Landing on the typing page needs word suggestions, so this
	/// is the last moment it can happen, and the person is already expecting the page to change.
	/// </summary>
	private void GoBack()
	{
		Database.OpenForCurrentLanguage();
		Navigation.NavigateTo("/type");
	}

	private void GoToCamera() => Navigation.NavigateTo("/camera");

	/// <summary>
	/// Switches language as soon as the dropdown changes, so the person sees this page in the
	/// language they just picked and can tell they picked the right one.
	/// </summary>
	private void OnLanguageChanged(ChangeEventArgs e)
	{
		string? name = e.Value?.ToString();
		CultureInfo? culture = Languages.FirstOrDefault(language => language.Name == name);
		if (culture is null)
			return;

		SelectedLanguage = culture.Name;
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