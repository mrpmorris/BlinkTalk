using System.Threading.Tasks;

namespace BlinkTalk.Application.Abstractions
{
    /// <summary>
    /// Speaks text aloud. Mirrors the original TextToSpeech.Speak: UK English voice,
    /// slow rate and low pitch, interrupting any in-progress utterance.
    /// </summary>
    public interface ITextToSpeechService
    {
        /// <summary>Speak the given text, cancelling/flushing any current speech first.</summary>
        Task SpeakAsync(string text);
    }
}
