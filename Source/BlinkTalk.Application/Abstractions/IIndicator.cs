using System;

namespace BlinkTalk.Application.Abstractions;

/// <summary>
/// An input source for the single switch (pointer, keyboard, camera gesture). Raises
/// <see cref="Indicated"/> on the UI thread when the person indicates; <c>ScanController</c>
/// subscribes to every registered indicator and routes the signal to the active strategy.
/// </summary>
public interface IIndicator
{
    event Action? Indicated;

    /// <summary>
    /// Raised when a held gesture begins (rising edge), before it is known whether it will
    /// last long enough to count as an indication. Only the camera indicator raises these;
    /// the pointer/keyboard sources never do. <c>ScanController</c> pauses the scan while a
    /// gesture is in progress so the highlight cannot move out from under it. Always balanced
    /// by a later <see cref="DwellEnded"/> — including after a successful <see cref="Indicated"/>.
    /// </summary>
    event Action? DwellStarted;

    /// <summary>Raised when a held gesture ends (falling edge). See <see cref="DwellStarted"/>.</summary>
    event Action? DwellEnded;
}
