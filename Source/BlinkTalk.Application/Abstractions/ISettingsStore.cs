namespace BlinkTalk.Application.Abstractions;

/// <summary>Persists user preferences (e.g. scan speed, camera training). Backed by MAUI Preferences in the app.</summary>
public interface ISettingsStore
{
    double GetDouble(string key, double defaultValue);
    void SetDouble(string key, double value);
    bool GetBool(string key, bool defaultValue);
    void SetBool(string key, bool value);
    string GetString(string key, string defaultValue);
    void SetString(string key, string value);
}

public static class SettingsKeys
{
    public const string CameraDwellSeconds = "camera.dwellSeconds"; // how long the gesture must be held to count
    public const string CameraSignal = "camera.signal";       // MediaPipe blendshape category name
    public const string CameraThreshold = "camera.threshold"; // fire when the signal's score crosses this
    // Camera indicator detection. Note: whether the camera is *enabled* is intentionally NOT
    // persisted — it is a session-only flag (off on every start), so there is no key for it.
    public const string CameraTrained = "camera.trained";
    public const string CycleDelaySeconds = "cycleDelaySeconds";
    public const string KeyboardLayoutStyle = "keyboard.layoutStyle"; // a KeyboardLayoutStyle member name
    public const string LanguageCultureCode = "language.cultureCode"; // e.g. "fr", "en-GB"

    /// <summary>
    /// The chosen speech voice, under a key suffixed with the culture name ("speech.voice.fr") —
    /// see <see cref="SpeechVoiceKey"/>. Per language rather than one global choice, because a voice
    /// installed for one language cannot speak another: carrying the choice over would leave the app
    /// mute or reading French with an English voice.
    /// </summary>
    public const string SpeechVoicePrefix = "speech.voice.";

    /// <summary>The <see cref="SpeechVoicePrefix"/> key for one language's chosen voice.</summary>
    public static string SpeechVoiceKey(string cultureName) => SpeechVoicePrefix + cultureName;
}
