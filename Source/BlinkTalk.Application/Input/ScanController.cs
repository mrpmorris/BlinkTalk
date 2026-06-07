using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlinkTalk.Application.Abstractions;
using BlinkTalk.Application.Input.Strategies;
using BlinkTalk.Application.Text;

namespace BlinkTalk.Application.Input;

/// <summary>
/// The logical port of the original TypingController. Owns the SentenceBuilder and the stack
/// of input strategies, drives highlight state, and routes the single "indicate" signal to
/// the active strategy. Knows nothing about the UI framework — it raises <see cref="StateChanged"/>
/// and the Razor layer re-renders.
/// </summary>
public sealed class ScanController : IScanController, IDisposable
{
    public HighlightTarget Highlight { get; private set; } = HighlightTarget.None;
    public KeyboardLayout Keyboard { get; }
    public SentenceBuilder Sentence { get; }
    public ITextToSpeechService Speech { get; }

    public event Action? StateChanged;

    // The single cycler currently running (exactly one is active at a time — each strategy
    // stops its cycler before a child starts one). Paused while a camera gesture is held.
    private FocusCycler? ActiveCycler;
    private readonly IClock Clock;
    private readonly Func<TimeSpan, CancellationToken, Task>? Delay;
    private readonly IUiDispatcher Dispatcher;
    // Number of in-progress held gestures; while > 0 the active cycle is paused.
    private int DwellDepth;
    private readonly IEnumerable<IIndicator> Indicators;
    private readonly ISettingsStore Settings;
    private bool Started;
    private readonly Stack<IInputStrategy> Strategies = new Stack<IInputStrategy>();

    public ScanController(
        SentenceBuilder sentence,
        KeyboardLayout keyboard,
        ITextToSpeechService speech,
        ISettingsStore settings,
        IUiDispatcher dispatcher,
        IEnumerable<IIndicator> indicators,
        Func<TimeSpan, CancellationToken, Task>? delay = null,
        IClock? clock = null)
    {
        Sentence = sentence;
        Keyboard = keyboard;
        Speech = speech;
        Settings = settings;
        Dispatcher = dispatcher;
        Indicators = indicators;
        Delay = delay;
        Clock = clock ?? new SystemClock();
        Sentence.ViewModelChanged += (s, e) => RaiseStateChanged();
        foreach (var indicator in Indicators)
        {
            indicator.Indicated += OnIndicated;
            indicator.DwellStarted += OnDwellStarted;
            indicator.DwellEnded += OnDwellEnded;
        }
    }

    /// <summary>Scan speed in seconds, persisted in settings. Affects the next dwell.</summary>
    public double CycleDelaySeconds
    {
        get => Settings.GetDouble(SettingsKeys.CycleDelaySeconds, Consts.DefaultCycleDelaySeconds);
        set => Settings.SetDouble(SettingsKeys.CycleDelaySeconds, value);
    }

    public int Depth => Strategies.Count;

    public void Dispose()
    {
        foreach (var indicator in Indicators)
        {
            indicator.Indicated -= OnIndicated;
            indicator.DwellStarted -= OnDwellStarted;
            indicator.DwellEnded -= OnDwellEnded;
        }
    }

    public FocusCycler NewCycler(Action<int> focusChanged, double firstCycleMultiplier = 1,
        Func<int, bool>? mayFocus = null, Action? onExhausted = null)
    {
        FocusCycler cycler = null!;
        cycler = new FocusCycler(
            Dispatcher,
            focusChanged,
            () => TimeSpan.FromSeconds(CycleDelaySeconds),
            firstCycleMultiplier,
            mayFocus,
            Delay,
            onExhausted,
            Clock,
            onRunningChanged: running =>
            {
                if (running)
                {
                    ActiveCycler = cycler;
                    // A gesture already in progress when this cycle starts → start it paused.
                    if (Volatile.Read(ref DwellDepth) > 0)
                        cycler.Pause();
                }
                else if (ReferenceEquals(ActiveCycler, cycler))
                {
                    ActiveCycler = null;
                }
            });
        return cycler;
    }

    public void Pop()
    {
        if (Strategies.Count == 0)
            return;
        IInputStrategy terminated = Strategies.Pop();
        terminated.Terminated();
        if (Strategies.Count > 0)
            Strategies.Peek().Initialize(this);
        RaiseStateChanged();
    }

    public TStrategy Push<TStrategy>() where TStrategy : IInputStrategy, new()
    {
        var strategy = new TStrategy();
        if (Strategies.Count > 0)
            Strategies.Peek().ChildStrategyActivated(strategy);
        // Push before Initialize: a strategy may pop itself during Initialize (e.g. the word
        // selector when no suggestions remain), and Pop must target this strategy. In the
        // original this was implicit because Initialize deferred its work by a frame.
        Strategies.Push(strategy);
        strategy.Initialize(this);
        RaiseStateChanged();
        return strategy;
    }

    public void SetHighlight(HighlightTarget target)
    {
        Highlight = target;
        RaiseStateChanged();
    }

    /// <summary>Begin scanning: load suggestions and enter the top-level section selector.</summary>
    public void Start()
    {
        if (Started)
            return;
        Started = true;
        Sentence.Initialize();
        Push<SectionSelectorInputStrategy>();
        RaiseStateChanged();
    }

    private void OnDwellEnded()
    {
        if (Interlocked.Decrement(ref DwellDepth) <= 0)
        {
            Interlocked.Exchange(ref DwellDepth, 0); // clamp against any unbalanced end
            ActiveCycler?.Resume();
        }
    }

    // A held gesture started/ended (camera only). While one or more are in progress the active
    // cycle is paused, so the highlight stays put and a selection lands on the element the user
    // was on when the gesture began. DwellEnded always balances DwellStarted — including after
    // a successful indication — so the counter returns to 0 and scanning resumes.
    private void OnDwellStarted()
    {
        if (Interlocked.Increment(ref DwellDepth) == 1)
            ActiveCycler?.Pause();
    }

    /// <summary>
    /// The single switch input: raised by an <see cref="IIndicator"/> (the helper presses the
    /// button, or the camera detects the gesture) when the person blinks. Routes to the active
    /// strategy. Indicators raise this on the UI thread, preserving single-threaded mutation.
    /// </summary>
    private void OnIndicated()
    {
        if (Strategies.Count > 0)
            Strategies.Peek().ReceiveIndication();
    }

    private void RaiseStateChanged() => StateChanged?.Invoke();
}
