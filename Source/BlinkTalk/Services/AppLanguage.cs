using System.Globalization;
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
	/// <summary>Used when the UI language has no word list of its own.</summary>
	public const string Fallback = "English";

	/// <summary>
	/// The word-list language for the current UI culture. <see cref="Localization.Culture"/> is
	/// null unless something assigns it — that's the designer default, and it means "use the
	/// current UI culture" — so fall back the same way the resource lookups do.
	/// </summary>
	public static string Name => GetName(Localization.Culture ?? CultureInfo.CurrentUICulture) ?? Fallback;

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

	private static string? GetName(CultureInfo cultureInfo) =>
		cultureInfo.TwoLetterISOLanguageName.ToLowerInvariant() switch {
			"en" => "English",
			"fr" => "French",
			"de" => "German",
			_ => null
		};

}
