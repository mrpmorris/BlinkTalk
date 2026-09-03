using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlinkTalk.Application.Abstractions;

/// <summary>
/// One voice installed on this device, as offered in the settings page's dropdown.
/// <para>
/// A plain record rather than the platform's own voice type, because this assembly targets
/// netstandard2.0 and cannot reference MAUI: the app-side implementation maps its platform locales
/// onto this.
/// </para>
/// </summary>
/// <param name="Id">
/// Identifies the voice well enough to find it again on a later launch. Opaque to callers — it is
/// stored and compared, never parsed.
/// </param>
/// <param name="DisplayName">The name to show the person choosing.</param>
public sealed record SpeechVoiceOption(string Id, string DisplayName);

/// <summary>
/// Speaks text aloud. Mirrors the original TextToSpeech.Speak: slow rate and low pitch,
/// interrupting any in-progress utterance. The voice is the one the person chose for the app's
/// current language, or the platform's best match for that language when they have not chosen.
/// </summary>
public interface ITextToSpeechService
{
    /// <summary>
    /// The <see cref="SpeechVoiceOption.Id"/> chosen for the app's current language, or null to let
    /// the platform pick. Persisted per language, so switching language does not carry a voice over
    /// to a language it cannot speak.
    /// </summary>
    string? SelectedVoiceId { get; set; }

    /// <summary>
    /// The voices installed for the app's current language. Empty when the platform reports none, or
    /// when it cannot be asked — the caller then has nothing to offer beyond the system default.
    /// </summary>
    Task<IReadOnlyList<SpeechVoiceOption>> GetVoicesForCurrentLanguageAsync();

    /// <summary>Speak the given text, cancelling/flushing any current speech first.</summary>
    Task SpeakAsync(string text);
}
