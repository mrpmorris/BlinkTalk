using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlinkTalk.Application.Abstractions;
using Microsoft.Maui.Media;

namespace BlinkTalk.Services;

/// <summary>
/// Text-to-speech via MAUI's cross-platform TextToSpeech. Reproduces the original
/// TextToSpeech.Speak: UK English voice, low pitch and full volume, a trailing period, and
/// flushing any in-progress utterance by cancelling it.
///
/// Note: MAUI's SpeechOptions exposes Volume, Pitch and Locale but no cross-platform speaking
/// rate, so the original's slow rate (0.4) is not yet applied. Applying it requires a
/// per-platform shim (Android setSpeechRate, iOS/MacCatalyst AVSpeechUtterance.Rate, Windows
/// SSML prosody); this is left as a follow-up and flagged so the gap is explicit.
/// </summary>
public sealed class MauiTtsService : ITextToSpeechService
{
    private CancellationTokenSource? CurrentSpeech;
    private bool LocaleResolved;
    private const float Pitch = 0.6f;
    private Locale? ResolvedLocale;
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
            await TextToSpeech.Default.SpeakAsync(text + ".", options, cts.Token);
        }
        catch (System.OperationCanceledException)
        {
            // Superseded by a newer utterance; expected.
        }
    }

    private async Task<Locale?> ResolveLocaleAsync()
    {
        if (LocaleResolved)
            return ResolvedLocale;

        try
        {
            var locales = (await TextToSpeech.Default.GetLocalesAsync()).ToList();
            ResolvedLocale = locales.FirstOrDefault(l => l.Language == "en" && l.Country == "GB")
                      ?? locales.FirstOrDefault(l => l.Language == "en");
        }
        catch
        {
            ResolvedLocale = null; // Fall back to the system default voice.
        }

        LocaleResolved = true;
        return ResolvedLocale;
    }
}
