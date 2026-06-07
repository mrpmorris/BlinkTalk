using BlinkTalk.Application.Abstractions;

namespace BlinkTalk.Services;

/// <summary>
/// Typed access to the camera-indicator config. The trained gesture (signal + threshold) is
/// persisted via <see cref="ISettingsStore"/> so it survives restarts, but whether the camera
/// is actively used as the indicator is a <b>session-only</b> flag: it always starts false and
/// can only be turned on at runtime after setup. The "signal" is the MediaPipe FaceLandmarker
/// blendshape category the user's gesture most strongly drives (e.g. eyeLookUpLeft, eyeBlinkLeft),
/// and "threshold" is the score at which an indication fires. Registered as a singleton so the
/// session flag lives for the lifetime of the running app.
/// </summary>
public sealed class CameraIndicatorConfig
{
    private readonly ISettingsStore _settings;
    private bool _enabledThisSession; // never persisted — false on every app start

    public CameraIndicatorConfig(ISettingsStore settings)
    {
        _settings = settings;
    }

    /// <summary>True once the user has completed a training run that found a usable gesture.</summary>
    public bool IsTrained
    {
        get => _settings.GetBool(SettingsKeys.CameraTrained, false);
        set => _settings.SetBool(SettingsKeys.CameraTrained, value);
    }

    /// <summary>
    /// Whether the camera is being used as the indicator. Deliberately not persisted: it resets
    /// to false on every launch, and can only be set true at runtime once the user is trained.
    /// </summary>
    public bool IsEnabled
    {
        get => _enabledThisSession && IsTrained;
        set => _enabledThisSession = value;
    }

    public string Signal
    {
        get => _settings.GetString(SettingsKeys.CameraSignal, "");
        set => _settings.SetString(SettingsKeys.CameraSignal, value);
    }

    public double Threshold
    {
        get => _settings.GetDouble(SettingsKeys.CameraThreshold, 0.5);
        set => _settings.SetDouble(SettingsKeys.CameraThreshold, value);
    }

    /// <summary>
    /// How long (seconds) the trained gesture must be held before it counts as an indication.
    /// Reflex blinks are ~0.1–0.2s, so a higher hold time rejects them. Slider range 0.1–2.0.
    /// </summary>
    public double DwellSeconds
    {
        get => _settings.GetDouble(SettingsKeys.CameraDwellSeconds, 0.3);
        set => _settings.SetDouble(SettingsKeys.CameraDwellSeconds, value);
    }

    public void SaveTraining(string signal, double threshold)
    {
        Signal = signal;
        Threshold = threshold;
        IsTrained = true;
    }
}
