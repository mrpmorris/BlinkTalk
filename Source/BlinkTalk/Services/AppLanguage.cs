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
    public static string Name
    {
        get
        {
            CultureInfo culture = CultureInfo.CurrentCulture;

            // TwoLetterISOLanguageName collapses the regional variants: en-GB and en-US both give
            // "en", so a new region never needs a new case here.
            return culture.TwoLetterISOLanguageName.ToLowerInvariant() switch
            {
                "en" => "English",
                "fr" => "French",
                "de" => "German",
                _ => Fallback
            };
        }
    }
}
