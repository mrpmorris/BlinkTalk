using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlinkTalk.Application;
using BlinkTalk.Application.Abstractions;
using Microsoft.Maui.Media;

namespace BlinkTalk.Services;

/// <summary>
/// Text-to-speech via MAUI's cross-platform TextToSpeech. Reproduces the original
/// TextToSpeech.Speak: low pitch and full volume, a trailing period, and flushing any in-progress
/// utterance by cancelling it. The voice follows <see cref="AppLanguage.Current"/> so the
/// spoken language matches the localised UI; the document's &lt;html lang&gt; has no bearing on it,
/// because this is the native platform engine rather than the WebView's speechSynthesis.
/// <see cref="CultureInfo.CurrentCulture"/> would not do: it flows with the ExecutionContext, so it
/// reverts to the startup language once the handler that switched language has finished, and every
/// utterance after that would be spoken by the previous language's voice.
///
/// Note: MAUI's SpeechOptions exposes Volume, Pitch and Locale but no cross-platform speaking
/// rate, so the original's slow rate (0.4) is not yet applied. Applying it requires a
/// per-platform shim (Android setSpeechRate, iOS/MacCatalyst AVSpeechUtterance.Rate, Windows
/// SSML prosody); this is left as a follow-up and flagged so the gap is explicit.
/// </summary>
public sealed class MauiTtsService : ITextToSpeechService
{
    private CancellationTokenSource? CurrentSpeech;
    private const float Pitch = 0.6f;
    private Locale? ResolvedLocale;
    private string? ResolvedForCulture;
    private const float Volume = 1.0f;

    public async Task SpeakAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        // Flush any current utterance, mirroring the original SpeechFlush.
        CurrentSpeech?.Cancel();
        var cts = new CancellationTokenSource();
        CurrentSpeech = cts;

        var options = new SpeechOptions
        {
            Volume = Volume,
            Pitch = Pitch,
            Locale = await ResolveLocaleAsync()
        };

        try
        {
            await TextToSpeech.Default.SpeakAsync(text, options, cts.Token);
        }
        catch (System.OperationCanceledException)
        {
            // Superseded by a newer utterance; expected.
        }
    }

    /// <summary>
    /// Picks the installed voice that best matches the app's language: the exact tag first
    /// (en-GB), then any voice for the same language (fr-CA for fr-FR), then a language-only
    /// voice. Cached per culture name, so switching language re-resolves rather than reusing a
    /// stale voice.
    /// </summary>
    private async Task<Locale?> ResolveLocaleAsync()
    {
        var culture = AppLanguage.Current;
        if (ResolvedForCulture == culture.Name)
            return ResolvedLocale;

        try
        {
            var locales = (await TextToSpeech.Default.GetLocalesAsync()).ToList();
            string language = culture.TwoLetterISOLanguageName;
            ResolvedLocale =
                locales.FirstOrDefault(l => ToTag(l).Equals(culture.Name, StringComparison.OrdinalIgnoreCase))
                ?? locales.FirstOrDefault(l => ToTag(l).StartsWith(language + "-", StringComparison.OrdinalIgnoreCase))
                ?? locales.FirstOrDefault(l => ToTag(l).Equals(language, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            ResolvedLocale = null; // Fall back to the system default voice.
        }

        ResolvedForCulture = culture.Name;
        return ResolvedLocale;
    }

    /// <summary>
    /// Normalises a platform Locale to a BCP-47 tag. Android reports Language "en" with Country
    /// "GB", whereas Windows reports Language "en-GB" and leaves Country empty, so the two need
    /// flattening before they can be compared against a culture name.
    /// </summary>
    private static string ToTag(Locale locale)
    {
        string language = (locale.Language ?? string.Empty).Replace('_', '-');
        string country = (locale.Country ?? string.Empty).Replace('_', '-');
        return country.Length == 0 || language.Contains("-")
            ? language
            : language + "-" + country;
    }
}
