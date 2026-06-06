using System;

namespace BlinkTalk.Application.Abstractions
{
    /// <summary>
    /// An input source for the single switch (pointer, keyboard, camera gesture). Raises
    /// <see cref="Indicated"/> on the UI thread when the person indicates; <c>ScanController</c>
    /// subscribes to every registered indicator and routes the signal to the active strategy.
    /// </summary>
    public interface IIndicator
    {
        event Action? Indicated;
    }
}
