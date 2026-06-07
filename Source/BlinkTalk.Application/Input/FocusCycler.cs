using System;
using System.Threading;
using System.Threading.Tasks;
using BlinkTalk.Application.Abstractions;

namespace BlinkTalk.Application.Input;

/// <summary>
/// Drives the scan: advances a focus index, calling <c>focusChanged</c> for each focusable
/// index after a dwell of <c>cycleDelay</c> (the first dwell scaled by
/// <c>firstCycleMultiplier</c>). Indices for which <c>mayFocus</c> returns false are skipped
/// without consuming a dwell — exactly as the original Unity coroutine behaved.
///
/// The Unity <c>WaitForSeconds</c> coroutine is replaced by an async loop. Every callback is
/// marshalled through the <see cref="IUiDispatcher"/> so it runs on the UI thread, matching
/// the single-threaded guarantee Unity provided. The delay is injectable so tests can step
/// the cycle deterministically. If a full sweep finds nothing focusable the loop exits via
/// <c>onExhausted</c> rather than spinning.
///
/// The cycle can be <see cref="Pause"/>d and <see cref="Resume"/>d (the camera indicator does
/// this while a held gesture is in progress, via <c>ScanController</c>). Pausing freezes the
/// current highlight and suspends the dwell timer; the paused span is excluded, so on resume
/// the dwell finishes with the time that was still remaining when it paused.
/// </summary>
public sealed class FocusCycler
{
    private readonly IUiDispatcher _dispatcher;
    private readonly Func<int, bool> _mayFocus;
    private readonly Action<int> _focusChanged;
    private readonly Func<TimeSpan> _cycleDelay;
    private readonly double _firstCycleMultiplier;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;
    private readonly Action? _onExhausted;
    private readonly IClock _clock;
    private readonly Action<bool>? _onRunningChanged;
    private CancellationTokenSource? _cts;

    // Pause state. _paused is set/cleared on the UI thread (Pause/Resume run on it), and read
    // by the dwell loop on the same thread; _resume is the gate the loop awaits while paused;
    // _dwellInterrupt cancels the in-flight delay so its elapsed portion can be captured.
    private bool _paused;
    private TaskCompletionSource<bool>? _resume;
    private CancellationTokenSource? _dwellInterrupt;

    public int FocusChangeCount { get; private set; }

    public FocusCycler(
        IUiDispatcher dispatcher,
        Action<int> focusChanged,
        Func<TimeSpan> cycleDelay,
        double firstCycleMultiplier = 1,
        Func<int, bool>? mayFocus = null,
        Func<TimeSpan, CancellationToken, Task>? delay = null,
        Action? onExhausted = null,
        IClock? clock = null,
        Action<bool>? onRunningChanged = null)
    {
        if (firstCycleMultiplier <= 0)
            throw new ArgumentOutOfRangeException(nameof(firstCycleMultiplier));

        _dispatcher = dispatcher;
        _focusChanged = focusChanged;
        _cycleDelay = cycleDelay;
        _firstCycleMultiplier = firstCycleMultiplier;
        _mayFocus = mayFocus ?? (_ => true);
        _delay = delay ?? ((ts, ct) => Task.Delay(ts, ct));
        _onExhausted = onExhausted;
        _clock = clock ?? new SystemClock();
        _onRunningChanged = onRunningChanged;
    }

    public void Start(int numberOfItems)
    {
        if (_cts != null)
            throw new InvalidOperationException("FocusCycler already started");
        if (numberOfItems <= 0)
            throw new ArgumentOutOfRangeException(nameof(numberOfItems));

        FocusChangeCount = 0;
        var cts = new CancellationTokenSource();
        _cts = cts;
        _onRunningChanged?.Invoke(true);
        _ = RunAsync(numberOfItems, cts.Token);
    }

    public void Stop()
    {
        if (_cts != null)
        {
            // Cancel first, so a paused dwell loop woken below observes cancellation and exits
            // (rather than resuming the scan).
            _cts.Cancel();
            _paused = false;
            _resume?.TrySetResult(true);
            _dwellInterrupt?.Cancel();

            _cts.Dispose();
            _cts = null;
            _onRunningChanged?.Invoke(false);
        }
    }

    /// <summary>
    /// Suspend the scan: freezes the current highlight and stops the dwell timer counting.
    /// Idempotent. Safe to call when not running (the next dwell will start paused).
    /// </summary>
    public void Pause()
    {
        if (_paused)
            return;
        _paused = true;
        _resume = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        // Interrupt the in-flight dwell so the loop captures how much of it has elapsed.
        _dwellInterrupt?.Cancel();
    }

    /// <summary>Resume the scan after <see cref="Pause"/>, continuing the remaining dwell.</summary>
    public void Resume()
    {
        if (!_paused)
            return;
        _paused = false;
        _resume?.TrySetResult(true);
    }

    private async Task RunAsync(int numberOfItems, CancellationToken ct)
    {
        int focusIndex = 0;
        double delayMultiplier = _firstCycleMultiplier;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                bool exhausted = false;
                int firedIndex = -1;

                await _dispatcher.InvokeAsync(() =>
                {
                    if (ct.IsCancellationRequested)
                        return;

                    int skipped = 0;
                    while (!_mayFocus(focusIndex))
                    {
                        focusIndex = (focusIndex + 1) % numberOfItems;
                        if (++skipped >= numberOfItems)
                        {
                            exhausted = true;
                            _onExhausted?.Invoke();
                            return;
                        }
                    }

                    FocusChangeCount++;
                    firedIndex = focusIndex;
                    _focusChanged(focusIndex);
                }).ConfigureAwait(false);

                if (exhausted || ct.IsCancellationRequested)
                    return;

                await DwellAsync(Scale(_cycleDelay(), delayMultiplier), ct).ConfigureAwait(false);
                delayMultiplier = 1;
                focusIndex = (firedIndex + 1) % numberOfItems;
            }
        }
        catch (OperationCanceledException)
        {
            // Stop() was called; expected.
        }
        finally
        {
            if (!ct.IsCancellationRequested)
                _onRunningChanged?.Invoke(false);
        }
    }

    /// <summary>
    /// Wait out one dwell, honouring pause. While paused the timer is suspended (the paused
    /// span does not count toward the dwell); when resumed the dwell finishes with the time
    /// that was still remaining. Throws <see cref="OperationCanceledException"/> if stopped.
    /// </summary>
    private async Task DwellAsync(TimeSpan duration, CancellationToken ct)
    {
        // Await the injected delay for the full duration, but with pause time excluded: each
        // time a pause interrupts the wait we subtract the elapsed portion and, once resumed,
        // wait out only what remained. The delay is always awaited at least once (matching the
        // original single-await-per-dwell behaviour, including a zero-length dwell).
        TimeSpan remaining = duration;
        while (true)
        {
            // Hold here while paused — this does not consume any of the remaining dwell.
            while (_paused)
            {
                ct.ThrowIfCancellationRequested();
                await WaitForResumeAsync(ct).ConfigureAwait(false);
            }
            ct.ThrowIfCancellationRequested();

            bool completed;
            using (var interrupt = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                _dwellInterrupt = interrupt;
                // If a pause slipped in between the check above and here, cancel immediately.
                if (_paused)
                    interrupt.Cancel();

                DateTime start = _clock.UtcNow;
                try
                {
                    await _delay(remaining, interrupt.Token).ConfigureAwait(false);
                    completed = true;
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    // Paused mid-dwell: subtract the elapsed portion, loop back to wait for resume.
                    completed = false;
                    remaining -= _clock.UtcNow - start;
                    if (remaining < TimeSpan.Zero)
                        remaining = TimeSpan.Zero;
                }
                finally
                {
                    _dwellInterrupt = null;
                }
            }

            if (completed)
                return;
        }
    }

    // Await the resume gate, but also wake (and throw) if the cycler is stopped. netstandard2.0
    // has no Task.WaitAsync(CancellationToken), so make cancellation complete the same gate.
    private async Task WaitForResumeAsync(CancellationToken ct)
    {
        TaskCompletionSource<bool>? resume = _resume;
        if (resume == null)
            return;

        using (ct.Register(static s => ((TaskCompletionSource<bool>)s!).TrySetResult(true), resume))
            await resume.Task.ConfigureAwait(false);

        ct.ThrowIfCancellationRequested();
    }

    private static TimeSpan Scale(TimeSpan value, double factor) =>
        TimeSpan.FromTicks((long)(value.Ticks * factor));
}
