using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlinkTalk.Application.Abstractions;
using BlinkTalk.Application.Input.Strategies;
using BlinkTalk.Application.Text;

namespace BlinkTalk.Application.Input
{
    /// <summary>
    /// The logical port of the original TypingController. Owns the SentenceBuilder and the stack
    /// of input strategies, drives highlight state, and routes the single "indicate" signal to
    /// the active strategy. Knows nothing about the UI framework — it raises <see cref="StateChanged"/>
    /// and the Razor layer re-renders.
    /// </summary>
    public sealed class ScanController : IScanController, IDisposable
    {
        private readonly IUiDispatcher _dispatcher;
        private readonly ISettingsStore _settings;
        private readonly Func<TimeSpan, CancellationToken, Task>? _delay;
        private readonly IEnumerable<IIndicator> _indicators;
        private readonly Stack<IInputStrategy> _strategies = new Stack<IInputStrategy>();
        private HighlightTarget _highlight = HighlightTarget.None;
        private bool _started;

        public SentenceBuilder Sentence { get; }
        public KeyboardLayout Keyboard { get; }
        public ITextToSpeechService Speech { get; }

        public HighlightTarget Highlight => _highlight;
        public int Depth => _strategies.Count;

        public event Action? StateChanged;

        public ScanController(
            SentenceBuilder sentence,
            KeyboardLayout keyboard,
            ITextToSpeechService speech,
            ISettingsStore settings,
            IUiDispatcher dispatcher,
            IEnumerable<IIndicator> indicators,
            Func<TimeSpan, CancellationToken, Task>? delay = null)
        {
            Sentence = sentence;
            Keyboard = keyboard;
            Speech = speech;
            _settings = settings;
            _dispatcher = dispatcher;
            _indicators = indicators;
            _delay = delay;
            Sentence.ViewModelChanged += (s, e) => RaiseStateChanged();
            foreach (var indicator in _indicators)
                indicator.Indicated += OnIndicated;
        }

        /// <summary>Scan speed in seconds, persisted in settings. Affects the next dwell.</summary>
        public double CycleDelaySeconds
        {
            get => _settings.GetDouble(SettingsKeys.CycleDelaySeconds, Consts.DefaultCycleDelaySeconds);
            set => _settings.SetDouble(SettingsKeys.CycleDelaySeconds, value);
        }

        /// <summary>Begin scanning: load suggestions and enter the top-level section selector.</summary>
        public void Start()
        {
            if (_started)
                return;
            _started = true;
            Sentence.Initialize();
            Push<SectionSelectorInputStrategy>();
            RaiseStateChanged();
        }

        /// <summary>
        /// The single switch input: raised by an <see cref="IIndicator"/> (the helper presses the
        /// button, or the camera detects the gesture) when the person blinks. Routes to the active
        /// strategy. Indicators raise this on the UI thread, preserving single-threaded mutation.
        /// </summary>
        private void OnIndicated()
        {
            if (_strategies.Count > 0)
                _strategies.Peek().ReceiveIndication();
        }

        public FocusCycler NewCycler(Action<int> focusChanged, double firstCycleMultiplier = 1,
            Func<int, bool>? mayFocus = null, Action? onExhausted = null)
        {
            return new FocusCycler(
                _dispatcher,
                focusChanged,
                () => TimeSpan.FromSeconds(CycleDelaySeconds),
                firstCycleMultiplier,
                mayFocus,
                _delay,
                onExhausted);
        }

        public TStrategy Push<TStrategy>() where TStrategy : IInputStrategy, new()
        {
            var strategy = new TStrategy();
            if (_strategies.Count > 0)
                _strategies.Peek().ChildStrategyActivated(strategy);
            // Push before Initialize: a strategy may pop itself during Initialize (e.g. the word
            // selector when no suggestions remain), and Pop must target this strategy. In the
            // original this was implicit because Initialize deferred its work by a frame.
            _strategies.Push(strategy);
            strategy.Initialize(this);
            RaiseStateChanged();
            return strategy;
        }

        public void Pop()
        {
            if (_strategies.Count == 0)
                return;
            IInputStrategy terminated = _strategies.Pop();
            terminated.Terminated();
            if (_strategies.Count > 0)
                _strategies.Peek().Initialize(this);
            RaiseStateChanged();
        }

        public void SetHighlight(HighlightTarget target)
        {
            _highlight = target;
            RaiseStateChanged();
        }

        private void RaiseStateChanged() => StateChanged?.Invoke();

        public void Dispose()
        {
            foreach (var indicator in _indicators)
                indicator.Indicated -= OnIndicated;
        }
    }
}
