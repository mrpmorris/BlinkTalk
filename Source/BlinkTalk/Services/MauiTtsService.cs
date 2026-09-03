using System;
using System.Collections.Generic;
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
/// utterance by cancelling it. The voice is the one chosen on the settings page for the app's
/// language, falling back to the platform's best match for <see cref="AppLanguage.Current"/> so the
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

    // The (culture, chosen voice) pair the cached locale was resolved for. Both axes matter:
    // keying on the culture alone would keep speaking in the previous voice for the rest of the
    // session after the person picked a new one.
    private (string Culture, string? VoiceId)? ResolvedFor;
    private readonly ISettingsStore Settings;
    private const float Volume = 1.0f;

    public MauiTtsService(ISettingsStore settings)
    {
        Settings = settings;
    }

    /// <summary>
    /// Stored against the app's current language. An empty stored value means "no choice", which is
    /// null here and the platform's own pick in <see cref="ResolveLocaleAsync"/> — the same state as
    /// before anyone visited the settings page.
    /// </summary>
    public string? SelectedVoiceId
    {
        get
        {
            string id = Settings.GetString(SettingsKeys.SpeechVoiceKey(AppLanguage.Current.Name), string.Empty);
            return string.IsNullOrEmpty(id) ? null : id;
        }
        set => Settings.SetString(SettingsKeys.SpeechVoiceKey(AppLanguage.Current.Name), value ?? string.Empty);
    }

    /// <summary>
    /// The installed voices that can speak the app's language: those matching the culture exactly
    /// (en-GB) plus any other region of the same language (en-US), which is what the fallback chain in
    /// <see cref="ResolveLocaleAsync"/> would settle for anyway. Ordered by the name shown, so the
    /// dropdown does not reorder itself between launches.
    /// </summary>
    public async Task<IReadOnlyList<SpeechVoiceOption>> GetVoicesForCurrentLanguageAsync()
    {
        try
        {
            IEnumerable<Locale> locales = await TextToSpeech.Default.GetLocalesAsync();
            return SpeakCurrentLanguage(locales)
                .Select(locale => new SpeechVoiceOption(ToId(locale), ToDisplayName(locale)))
                .OrderBy(voice => voice.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
        catch
        {
            // Same reasoning as the catch in ResolveLocaleAsync: an engine that cannot be enumerated
            // leaves the system default as the only thing on offer.
            return Array.Empty<SpeechVoiceOption>();
        }
    }

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
    /// The voice chosen for the app's language, or — when none is chosen, or the chosen one is no
    /// longer installed — the installed voice that best matches: the exact tag first (en-GB), then any
    /// voice for the same language (fr-CA for fr-FR), then a language-only voice. Cached per culture
    /// and chosen voice, so either changing re-resolves rather than reusing a stale voice.
    /// </summary>
    private async Task<Locale?> ResolveLocaleAsync()
    {
        var culture = AppLanguage.Current;
        string? voiceId = SelectedVoiceId;
        if (ResolvedFor == (culture.Name, voiceId))
            return ResolvedLocale;

        try
        {
            var locales = (await TextToSpeech.Default.GetLocalesAsync()).ToList();
            string language = culture.TwoLetterISOLanguageName;

            // The chosen voice can vanish — a language pack uninstalled, a device restored from a
            // backup taken on another handset — so fall through to the best match rather than going
            // silent or throwing.
            ResolvedLocale =
                (voiceId is null ? null : locales.FirstOrDefault(l => ToId(l) == voiceId))
                ?? locales.FirstOrDefault(l => ToTag(l).Equals(culture.Name, StringComparison.OrdinalIgnoreCase))
                ?? locales.FirstOrDefault(l => ToTag(l).StartsWith(language + "-", StringComparison.OrdinalIgnoreCase))
                ?? locales.FirstOrDefault(l => ToTag(l).Equals(language, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            ResolvedLocale = null; // Fall back to the system default voice.
        }

        ResolvedFor = (culture.Name, voiceId);
        return ResolvedLocale;
    }

    /// <summary>The locales able to speak the app's current language — see <see cref="GetVoicesForCurrentLanguageAsync"/>.</summary>
    private static IEnumerable<Locale> SpeakCurrentLanguage(IEnumerable<Locale> locales)
    {
        string language = AppLanguage.Current.TwoLetterISOLanguageName;
        return locales.Where(locale =>
            ToTag(locale).Equals(language, StringComparison.OrdinalIgnoreCase)
            || ToTag(locale).StartsWith(language + "-", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// What the dropdown shows. The platform's voice name where there is one ("Microsoft Hazel",
    /// "Daniel"), and the language tag where there is not: Android reports an empty name for most
    /// engines, so the region is all there is to tell two entries apart by.
    /// </summary>
    private static string ToDisplayName(Locale locale) =>
        string.IsNullOrWhiteSpace(locale.Name) ? ToTag(locale) : locale.Name;

    /// <summary>
    /// Identifies a voice across launches. The tag is part of it because <see cref="Locale.Name"/> is
    /// empty on Android, where the region is the only thing distinguishing one voice from another;
    /// the name is part of it because Windows and iOS list several named voices per tag.
    /// </summary>
    private static string ToId(Locale locale) => ToTag(locale) + "|" + (locale.Name ?? string.Empty);

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
