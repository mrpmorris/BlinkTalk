using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlinkTalk.Application.Abstractions;
using BlinkTalk.Application.Persistence;
using BlinkTalk.Application.Prediction;
using BlinkTalk.Application.Text;

namespace BlinkTalk.Application.Tests;

/// <summary>
/// A controllable replacement for the scan dwell. The cycler "parks" in <see cref="Delay"/>;
/// each <see cref="StepAsync"/> releases the current park so exactly one scan tick advances.
/// This makes the otherwise time-based scanner fully deterministic in tests.
/// </summary>
public sealed class StepDelay
{
    // Parked is released by the cycler once it has fired a tick and parked in Delay. Current is the
    // park a step releases: the most recent one, which is always the running cycler's, since a
    // cycler is stopped before its replacement is started and so the replacement parks last.
    //
    // Releasing that one park specifically — rather than posting a shared signal for whichever
    // delay happens to be waiting — is what makes stepping deterministic. A stopped cycler's park
    // lingers in the wait queue for a moment (a cancelled SemaphoreSlim waiter is removed
    // asynchronously), and would otherwise take the signal meant for the live cycler and then exit
    // without parking again, hanging the test.
    private TaskCompletionSource<bool>? Current;
    private readonly SemaphoreSlim Parked = new SemaphoreSlim(0);
    private readonly object Sync = new object();

    public Task Delay(TimeSpan _, CancellationToken ct)
    {
        var park = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (Sync)
            Current = park;
        CancellationTokenRegistration registration = ct.Register(() => park.TrySetCanceled(ct));
        Parked.Release();
        return AwaitThenUnregister(park.Task, registration);
    }

    public async Task StepAsync()
    {
        // Drain any stale 'parked' signals left by cyclers that started/stopped between steps
        // (e.g. a Pop that re-initialises the parent's cycler).
        while (Parked.Wait(0)) { }

        TaskCompletionSource<bool>? park;
        lock (Sync)
            park = Current;
        if (park is null)
            throw new InvalidOperationException("Nothing is scanning: no cycler has parked in Delay.");

        park.TrySetResult(true);  // allow exactly one tick
        await Parked.WaitAsync(); // the cycler releases this only after the tick's fire completes
    }

    private static async Task AwaitThenUnregister(Task task, CancellationTokenRegistration registration)
    {
        try { await task.ConfigureAwait(false); }
        finally { registration.Dispose(); }
    }
}

public sealed class FakeWordService : IWordService
{
    public IReadOnlyList<string> Suggestions { get; set; } = Array.Empty<string>();
    private int NextId;

    public void DecreaseWordUsage(int wordId) { }
    public List<string> GetWordSuggestions(string? currentWord, int numberOfWords) => new List<string>(Suggestions);
    public void IncreaseWordUsage(string word, out int wordId) => wordId = ++NextId;
}

public sealed class FakePhraseService : IPhraseService
{
    public IReadOnlyList<string> Suggestions { get; set; } = Array.Empty<string>();

    public List<string> GetWordSuggestions(IEnumerable<int> wordIds, string? currentWord, int numberOfWords) =>
        new List<string>(Suggestions);
    public void IncrementPhraseUsage(IEnumerable<int> wordIds) { }
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
    private readonly Dictionary<string, bool> Bools = new Dictionary<string, bool>();
    private readonly Dictionary<string, double> Doubles = new Dictionary<string, double>();
    private readonly Dictionary<string, string> Strings = new Dictionary<string, string>();

    public bool GetBool(string key, bool defaultValue) =>
        Bools.TryGetValue(key, out var v) ? v : defaultValue;
    public double GetDouble(string key, double defaultValue) =>
        Doubles.TryGetValue(key, out var v) ? v : defaultValue;
    public string GetString(string key, string defaultValue) =>
        Strings.TryGetValue(key, out var v) ? v : defaultValue;
    public void SetBool(string key, bool value) => Bools[key] = value;
    public void SetDouble(string key, double value) => Doubles[key] = value;
    public void SetString(string key, string value) => Strings[key] = value;
}

/// <summary>A controllable indicator: <see cref="Fire"/> raises <see cref="Indicated"/>, standing
/// in for the pointer/keyboard/camera sources the controller subscribes to in the app. The dwell
/// events stand in for the camera's held-gesture edges.</summary>
public sealed class FakeIndicator : IIndicator
{
    public event Action? DwellEnded;
    public event Action? DwellStarted;
    public event Action? Indicated;

    public void Fire() => Indicated?.Invoke();
    public void FireDwellEnded() => DwellEnded?.Invoke();
    public void FireDwellStarted() => DwellStarted?.Invoke();
}

/// <summary>
/// A delay the test completes by hand, recording every requested duration. Unlike
/// <see cref="StepDelay"/> it lets a test inspect the duration asked for (to verify a paused
/// dwell resumes with only the remaining time) and signals when the cycler has entered a delay.
/// </summary>
public sealed class GatedDelay
{
    /// <summary>Every duration passed to <see cref="Delay"/>, in order.</summary>
    public List<TimeSpan> Requested { get; } = new List<TimeSpan>();
    private TaskCompletionSource<bool>? Current;
    private readonly SemaphoreSlim Entered = new SemaphoreSlim(0);

    /// <summary>Complete the current delay so the cycler advances.</summary>
    public void Complete() => Current?.TrySetResult(true);

    public Task Delay(TimeSpan duration, CancellationToken ct)
    {
        Requested.Add(duration);
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Current = tcs;
        var registration = ct.Register(() => tcs.TrySetCanceled(ct));
        Entered.Release();
        return AwaitThenUnregister(tcs.Task, registration);
    }

    /// <summary>Wait until the cycler has entered (parked in) its next delay.</summary>
    public Task WaitEnteredAsync() => Entered.WaitAsync();

    private static async Task AwaitThenUnregister(Task task, CancellationTokenRegistration registration)
    {
        try { await task.ConfigureAwait(false); }
        finally { registration.Dispose(); }
    }
}

public sealed class FixedClock : IClock
{
    public DateTime UtcNow { get; set; } = new DateTime(2026, 6, 3, 0, 0, 0, DateTimeKind.Utc);
}

/// <summary>One keyboard, for tests that do not change language mid-run.</summary>
public sealed class FixedKeyboardLayoutProvider : IKeyboardLayoutProvider
{
    public KeyboardLayout Current { get; }

    public FixedKeyboardLayoutProvider(KeyboardLayout current) => Current = current;
}

/// <summary>A word list given inline, for seeding a database without building a zip.</summary>
public sealed class FakeSeedWordSource : ISeedWordSource
{
    private readonly (string Word, int LanguageUsageCount)[] Words;

    public FakeSeedWordSource(params (string Word, int LanguageUsageCount)[] words) => Words = words;

    public IEnumerable<(string Word, int LanguageUsageCount)> GetWords() => Words;
}
