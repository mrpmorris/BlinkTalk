using System.Globalization;
using BlinkTalk.Application.Abstractions;
using BlinkTalk.Resources;

namespace BlinkTalk.Services;

/// <summary>
/// The language the app is running in, as the plain English name used to name per-language
/// files: the bundled word list ("English.zip") and the writable database ("BlinkTalk-English.db").
/// Both must agree — a French UI seeded from the French word list needs its own database, or it
/// would inherit whichever language happened to be installed first.
/// </summary>
public static class AppLanguage
{
	/// <summary>
	/// The language the app runs in when nothing supported has been chosen yet. A specific culture
	/// rather than the neutral "en", so the TTS voice and number formatting have a region to work from.
	/// </summary>
	public const string DefaultCultureCode = "en-GB";

	/// <summary>Used when the UI language has no word list of its own.</summary>
	public const string Fallback = "English";

	/// <summary>
	/// The culture the app is running in. <see cref="Localization.Culture"/> is null unless something
	/// assigns it — that's the designer default, and it means "use the current UI culture" — so fall
	/// back the same way the resource lookups do. Read this rather than
	/// <see cref="CultureInfo.CurrentUICulture"/>: the latter is per-thread, and scan callbacks arrive
	/// on thread-pool threads that may predate <see cref="SetCurrent"/>.
	/// </summary>
	public static CultureInfo Current => Localization.Culture ?? CultureInfo.CurrentUICulture;

	/// <summary>The word-list language for the current UI culture.</summary>
	public static string Name => GetName(Current) ?? Fallback;

	/// <summary>
	/// Writes the current culture's code to <paramref name="settings"/> so the next launch starts in
	/// the same language. Called when the person leaves the settings page rather than when the
	/// dropdown changes, so a language they scroll past on the way to another is not remembered.
	/// </summary>
	public static void Persist(ISettingsStore settings)
	{
		ArgumentNullException.ThrowIfNull(settings);
		settings.SetString(SettingsKeys.LanguageCultureCode, Current.Name);
	}

	/// <summary>
	/// Makes the persisted choice the language the app runs in, falling back to
	/// <see cref="DefaultCultureCode"/> when nothing is stored, the stored code is not a culture this
	/// device knows, or the app has since dropped support for it (there is no word list to seed a
	/// dictionary from, and the UI would be half-translated).
	/// </summary>
	public static void RestorePersisted(ISettingsStore settings)
	{
		ArgumentNullException.ThrowIfNull(settings);
		string cultureCode = settings.GetString(SettingsKeys.LanguageCultureCode, string.Empty);
		SetCurrent(FindSupported(cultureCode) ?? new CultureInfo(DefaultCultureCode));
	}

	/// <summary>
	/// Makes <paramref name="cultureInfo"/> the language the app runs in. The DefaultThread* pair
	/// has to be set as well as the current thread's: FocusCycler's delay continuations (and
	/// therefore SpeakAsync) run on thread-pool threads, which would otherwise keep the system
	/// culture and resolve the wrong voice. <see cref="Localization.Culture"/> covers the threads
	/// that already exist, since DefaultThreadCurrentUICulture only reaches threads created after
	/// it is assigned — and it is what <see cref="Name"/> reads, so the word list and the database
	/// switch with the UI rather than with whichever thread happens to ask.
	/// </summary>
	public static void SetCurrent(CultureInfo cultureInfo)
	{
		ArgumentNullException.ThrowIfNull(cultureInfo);
		CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
		CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
		CultureInfo.CurrentCulture = cultureInfo;
		CultureInfo.CurrentUICulture = cultureInfo;
		Localization.Culture = cultureInfo;
	}

	public static bool IsSupported(CultureInfo cultureInfo)
	{
		ArgumentNullException.ThrowIfNull(cultureInfo);
		return GetName(cultureInfo) is not null;
	}

	/// <summary>
	/// The culture named by <paramref name="cultureCode"/>, or null if it is empty, is not a culture
	/// this device recognises, or is not one the app supports.
	/// </summary>
	private static CultureInfo? FindSupported(string cultureCode)
	{
		if (string.IsNullOrWhiteSpace(cultureCode))
			return null;

		try
		{
			var cultureInfo = new CultureInfo(cultureCode);
			return IsSupported(cultureInfo) ? cultureInfo : null;
		}
		catch (CultureNotFoundException)
		{
			return null;
		}
	}

	/// <summary>
	/// The word-list language for a culture: the full code ("pt-BR") is looked up before the partial
	/// one ("pt"), so a language whose regions need word lists of their own can name them without
	/// disturbing the languages where one list serves every region.
	/// </summary>
	private static string? GetName(CultureInfo cultureInfo) =>
		GetNameForCode(cultureInfo.Name) ?? GetNameForCode(cultureInfo.TwoLetterISOLanguageName);

	/// <summary>
	/// The word-list language named by exactly this culture code, or null if there is no list for it.
	/// Keys are lower case because the code is lower-cased before matching — a full code arrives
	/// canonicalised as "pt-BR", so it would not match a switch arm written in that casing by accident.
	/// </summary>
	private static string? GetNameForCode(string cultureCode) =>
		cultureCode.ToLowerInvariant() switch {
			"en" => "English",
			//"fr" => "French",
			//"de" => "German",
			//"es" => "Spanish",
			"pt" => "Portuguese",
			//"ar" => "Arabic",
			_ => null
		};

}
