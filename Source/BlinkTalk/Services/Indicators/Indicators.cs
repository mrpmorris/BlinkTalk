using System;
using BlinkTalk.Application.Abstractions;

namespace BlinkTalk.Services.Indicators;

/// <summary>
/// Base for the input sources that drive the single switch. A UI surface (a Razor component or
/// the camera JS bridge) calls <see cref="Trigger"/>; ScanController subscribes to
/// <see cref="IIndicator.Indicated"/>. Each source is a distinct type so the component that owns
/// it can inject exactly that one.
/// </summary>
public abstract class IndicatorBase : IIndicator
{
    public event Action? Indicated;
    public event Action? DwellStarted;
    public event Action? DwellEnded;

    public void Trigger() => Indicated?.Invoke();

    // Only the camera bridge calls these — pointer/keyboard indications are instantaneous and
    // have no dwell. They must stay balanced (every started is later ended).
    public void TriggerDwellStarted() => DwellStarted?.Invoke();
    public void TriggerDwellEnded() => DwellEnded?.Invoke();
}

/// <summary>Tapping/clicking anywhere on the typing surface.</summary>
public sealed class PointerIndicator : IndicatorBase { }

/// <summary>Pressing the switch key (Space/Enter) on the typing surface.</summary>
public sealed class KeyboardIndicator : IndicatorBase { }

/// <summary>The camera detecting the person's trained face gesture.</summary>
public sealed class CameraGestureIndicator : IndicatorBase { }
