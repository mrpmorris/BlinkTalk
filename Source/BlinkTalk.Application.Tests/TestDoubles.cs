using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlinkTalk.Application.Abstractions;
using BlinkTalk.Application.Prediction;

namespace BlinkTalk.Application.Tests;

/// <summary>
/// A controllable replacement for the scan dwell. The cycler "parks" in <see cref="Delay"/>;
/// each <see cref="StepAsync"/> releases the current park so exactly one scan tick advances.
/// This makes the otherwise time-based scanner fully deterministic in tests.
/// </summary>
public sealed class StepDelay
{
    // Parked is released by the cycler once it has fired a tick and parked in Delay.
    // Go is released by the test to allow exactly one more tick. The semaphores make the
    // handshake robust regardless of whether continuations resume sync or async.
    private readonly SemaphoreSlim Parked = new SemaphoreSlim(0);
    private readonly SemaphoreSlim Go = new SemaphoreSlim(0);

    public async Task Delay(TimeSpan _, CancellationToken ct)
    {
        Parked.Release();
        await Go.WaitAsync(ct).ConfigureAwait(false);
    }

    public async Task StepAsync()
    {
        // Drain any stale 'parked' signals left by cyclers that started/stopped between steps
        // (e.g. a Pop that re-initialises the parent's cycler).
        while (Parked.Wait(0)) { }
        Go.Release();             // allow exactly one tick
        await Parked.WaitAsync(); // the cycler releases this only after the tick's fire completes
    }
}

public sealed class FakeWordService : IWordService
{
    private int NextId;
    public IReadOnlyList<string> Suggestions { get; set; } = Array.Empty<string>();

    public void IncreaseWordUsage(string word, out int wordId) => wordId = ++NextId;
    public void DecreaseWordUsage(int wordId) { }
    public List<string> GetWordSuggestions(string? currentWord, int numberOfWords) => new List<string>(Suggestions);
}

public sealed class FakePhraseService : IPhraseService
{
    public IReadOnlyList<string> Suggestions { get; set; } = Array.Empty<string>();

    public void IncrementPhraseUsage(IEnumerable<int> wordIds) { }
    public List<string> GetWordSuggestions(IEnumerable<int> wordIds, string? currentWord, int numberOfWords) =>
        new List<string>(Suggestions);
}

public sealed class FakeTextToSpeech : ITextToSpeechService
{
    public List<string> Spoken { get; } = new List<string>();
    public Task SpeakAsync(string text)
    {
        Spoken.Add(text);
        return Task.CompletedTask;
    }
}

public sealed class FakeSettingsStore : ISettingsStore
{
    private readonly Dictionary<string, double> Doubles = new Dictionary<string, double>();
    private readonly Dictionary<string, bool> Bools = new Dictionary<string, bool>();
    private readonly Dictionary<string, string> Strings = new Dictionary<string, string>();

    public double GetDouble(string key, double defaultValue) =>
        Doubles.TryGetValue(key, out var v) ? v : defaultValue;
    public void SetDouble(string key, double value) => Doubles[key] = value;

    public bool GetBool(string key, bool defaultValue) =>
        Bools.TryGetValue(key, out var v) ? v : defaultValue;
    public void SetBool(string key, bool value) => Bools[key] = value;

    public string GetString(string key, string defaultValue) =>
        Strings.TryGetValue(key, out var v) ? v : defaultValue;
    public void SetString(string key, string value) => Strings[key] = value;
}

/// <summary>A controllable indicator: <see cref="Fire"/> raises <see cref="Indicated"/>, standing
/// in for the pointer/keyboard/camera sources the controller subscribes to in the app. The dwell
/// events stand in for the camera's held-gesture edges.</summary>
public sealed class FakeIndicator : IIndicator
{
    public event Action? Indicated;
    public event Action? DwellStarted;
    public event Action? DwellEnded;

    public void Fire() => Indicated?.Invoke();
    public void FireDwellStarted() => DwellStarted?.Invoke();
    public void FireDwellEnded() => DwellEnded?.Invoke();
}

/// <summary>
/// A delay the test completes by hand, recording every requested duration. Unlike
/// <see cref="StepDelay"/> it lets a test inspect the duration asked for (to verify a paused
/// dwell resumes with only the remaining time) and signals when the cycler has entered a delay.
/// </summary>
public sealed class GatedDelay
{
    private readonly SemaphoreSlim Entered = new SemaphoreSlim(0);
    private TaskCompletionSource<bool>? Current;

    /// <summary>Every duration passed to <see cref="Delay"/>, in order.</summary>
    public List<TimeSpan> Requested { get; } = new List<TimeSpan>();

    public Task Delay(TimeSpan duration, CancellationToken ct)
    {
        Requested.Add(duration);
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Current = tcs;
        var registration = ct.Register(() => tcs.TrySetCanceled(ct));
        Entered.Release();
        return AwaitThenUnregister(tcs.Task, registration);
    }

    private static async Task AwaitThenUnregister(Task task, CancellationTokenRegistration registration)
    {
        try { await task.ConfigureAwait(false); }
        finally { registration.Dispose(); }
    }

    /// <summary>Wait until the cycler has entered (parked in) its next delay.</summary>
    public Task WaitEnteredAsync() => Entered.WaitAsync();

    /// <summary>Complete the current delay so the cycler advances.</summary>
    public void Complete() => Current?.TrySetResult(true);
}

public sealed class FixedClock : IClock
{
    public DateTime UtcNow { get; set; } = new DateTime(2026, 6, 3, 0, 0, 0, DateTimeKind.Utc);
}
