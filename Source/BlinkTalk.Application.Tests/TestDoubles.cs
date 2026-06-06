using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlinkTalk.Application.Abstractions;
using BlinkTalk.Application.Prediction;

namespace BlinkTalk.Application.Tests
{
    /// <summary>
    /// A controllable replacement for the scan dwell. The cycler "parks" in <see cref="Delay"/>;
    /// each <see cref="StepAsync"/> releases the current park so exactly one scan tick advances.
    /// This makes the otherwise time-based scanner fully deterministic in tests.
    /// </summary>
    public sealed class StepDelay
    {
        // _parked is released by the cycler once it has fired a tick and parked in Delay.
        // _go is released by the test to allow exactly one more tick. The semaphores make the
        // handshake robust regardless of whether continuations resume sync or async.
        private readonly SemaphoreSlim _parked = new SemaphoreSlim(0);
        private readonly SemaphoreSlim _go = new SemaphoreSlim(0);

        public async Task Delay(TimeSpan _, CancellationToken ct)
        {
            _parked.Release();
            await _go.WaitAsync(ct).ConfigureAwait(false);
        }

        public async Task StepAsync()
        {
            // Drain any stale 'parked' signals left by cyclers that started/stopped between steps
            // (e.g. a Pop that re-initialises the parent's cycler).
            while (_parked.Wait(0)) { }
            _go.Release();             // allow exactly one tick
            await _parked.WaitAsync(); // the cycler releases this only after the tick's fire completes
        }
    }

    public sealed class FakeWordService : IWordService
    {
        private int _nextId;
        public IReadOnlyList<string> Suggestions { get; set; } = Array.Empty<string>();

        public void IncreaseWordUsage(string word, out int wordId) => wordId = ++_nextId;
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
        private readonly Dictionary<string, double> _doubles = new Dictionary<string, double>();
        private readonly Dictionary<string, bool> _bools = new Dictionary<string, bool>();
        private readonly Dictionary<string, string> _strings = new Dictionary<string, string>();

        public double GetDouble(string key, double defaultValue) =>
            _doubles.TryGetValue(key, out var v) ? v : defaultValue;
        public void SetDouble(string key, double value) => _doubles[key] = value;

        public bool GetBool(string key, bool defaultValue) =>
            _bools.TryGetValue(key, out var v) ? v : defaultValue;
        public void SetBool(string key, bool value) => _bools[key] = value;

        public string GetString(string key, string defaultValue) =>
            _strings.TryGetValue(key, out var v) ? v : defaultValue;
        public void SetString(string key, string value) => _strings[key] = value;
    }

    /// <summary>A controllable indicator: <see cref="Fire"/> raises <see cref="Indicated"/>, standing
    /// in for the pointer/keyboard/camera sources the controller subscribes to in the app.</summary>
    public sealed class FakeIndicator : IIndicator
    {
        public event Action? Indicated;
        public void Fire() => Indicated?.Invoke();
    }

    public sealed class FixedClock : IClock
    {
        public DateTime UtcNow { get; set; } = new DateTime(2026, 6, 3, 0, 0, 0, DateTimeKind.Utc);
    }
}
