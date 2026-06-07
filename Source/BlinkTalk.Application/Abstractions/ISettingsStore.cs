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
    public const string CycleDelaySeconds = "cycleDelaySeconds";

    // Camera indicator detection. Note: whether the camera is *enabled* is intentionally NOT
    // persisted — it is a session-only flag (off on every start), so there is no key for it.
    public const string CameraTrained = "camera.trained";
    public const string CameraSignal = "camera.signal";       // MediaPipe blendshape category name
    public const string CameraThreshold = "camera.threshold"; // fire when the signal's score crosses this
    public const string CameraDwellSeconds = "camera.dwellSeconds"; // how long the gesture must be held to count
}
