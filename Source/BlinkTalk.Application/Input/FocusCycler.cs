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
    public int FocusChangeCount { get; private set; }

    private readonly IClock Clock;
    private CancellationTokenSource? Cts;
    private readonly Func<TimeSpan> CycleDelay;
    private readonly Func<TimeSpan, CancellationToken, Task> Delay;
    private readonly IUiDispatcher Dispatcher;
    private CancellationTokenSource? DwellInterrupt;
    private readonly double FirstCycleMultiplier;
    private readonly Action<int> FocusChanged;
    private readonly Func<int, bool> MayFocus;
    private readonly Action? OnExhausted;
    private readonly Action<bool>? OnRunningChanged;

    // Pause state. Paused is set/cleared on the UI thread (Pause/Resume run on it), and read
    // by the dwell loop on the same thread; ResumeGate is the gate the loop awaits while paused;
    // DwellInterrupt cancels the in-flight delay so its elapsed portion can be captured.
    private bool Paused;
    private TaskCompletionSource<bool>? ResumeGate;

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

        Dispatcher = dispatcher;
        FocusChanged = focusChanged;
        CycleDelay = cycleDelay;
        FirstCycleMultiplier = firstCycleMultiplier;
        MayFocus = mayFocus ?? (_ => true);
        Delay = delay ?? ((ts, ct) => Task.Delay(ts, ct));
        OnExhausted = onExhausted;
        Clock = clock ?? new SystemClock();
        OnRunningChanged = onRunningChanged;
    }

    /// <summary>
    /// Suspend the scan: freezes the current highlight and stops the dwell timer counting.
    /// Idempotent. Safe to call when not running (the next dwell will start paused).
    /// </summary>
    public void Pause()
    {
        if (Paused)
            return;
        Paused = true;
        ResumeGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        // Interrupt the in-flight dwell so the loop captures how much of it has elapsed.
        DwellInterrupt?.Cancel();
    }

    /// <summary>Resume the scan after <see cref="Pause"/>, continuing the remaining dwell.</summary>
    public void Resume()
    {
        if (!Paused)
            return;
        Paused = false;
        ResumeGate?.TrySetResult(true);
    }

    public void Start(int numberOfItems)
    {
        if (Cts != null)
            throw new InvalidOperationException("FocusCycler already started");
        if (numberOfItems <= 0)
            throw new ArgumentOutOfRangeException(nameof(numberOfItems));

        FocusChangeCount = 0;
        var cts = new CancellationTokenSource();
        Cts = cts;
        OnRunningChanged?.Invoke(true);
        _ = RunAsync(numberOfItems, cts.Token);
    }

    public void Stop()
    {
        if (Cts != null)
        {
            // Cancel first, so a paused dwell loop woken below observes cancellation and exits
            // (rather than resuming the scan).
            Cts.Cancel();
            Paused = false;
            ResumeGate?.TrySetResult(true);
            DwellInterrupt?.Cancel();

            Cts.Dispose();
            Cts = null;
            OnRunningChanged?.Invoke(false);
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
            while (Paused)
            {
                ct.ThrowIfCancellationRequested();
                await WaitForResumeAsync(ct).ConfigureAwait(false);
            }
            ct.ThrowIfCancellationRequested();

            bool completed;
            using (var interrupt = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                DwellInterrupt = interrupt;
                // If a pause slipped in between the check above and here, cancel immediately.
                if (Paused)
                    interrupt.Cancel();

                DateTime start = Clock.UtcNow;
                try
                {
                    await Delay(remaining, interrupt.Token).ConfigureAwait(false);
                    completed = true;
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    // Paused mid-dwell: subtract the elapsed portion, loop back to wait for resume.
                    completed = false;
                    remaining -= Clock.UtcNow - start;
                    if (remaining < TimeSpan.Zero)
                        remaining = TimeSpan.Zero;
                }
                finally
                {
                    DwellInterrupt = null;
                }
            }

            if (completed)
                return;
        }
    }

    private async Task RunAsync(int numberOfItems, CancellationToken ct)
    {
        int focusIndex = 0;
        double delayMultiplier = FirstCycleMultiplier;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                bool exhausted = false;
                int firedIndex = -1;

                await Dispatcher.InvokeAsync(() =>
                {
                    if (ct.IsCancellationRequested)
                        return;

                    int skipped = 0;
                    while (!MayFocus(focusIndex))
                    {
                        focusIndex = (focusIndex + 1) % numberOfItems;
                        if (++skipped >= numberOfItems)
                        {
                            exhausted = true;
                            OnExhausted?.Invoke();
                            return;
                        }
                    }

                    FocusChangeCount++;
                    firedIndex = focusIndex;
                    FocusChanged(focusIndex);
                }).ConfigureAwait(false);

                if (exhausted || ct.IsCancellationRequested)
                    return;

                await DwellAsync(Scale(CycleDelay(), delayMultiplier), ct).ConfigureAwait(false);
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
                OnRunningChanged?.Invoke(false);
        }
    }

    private static TimeSpan Scale(TimeSpan value, double factor) =>
        TimeSpan.FromTicks((long)(value.Ticks * factor));

    // Await the resume gate, but also wake (and throw) if the cycler is stopped. netstandard2.0
    // has no Task.WaitAsync(CancellationToken), so make cancellation complete the same gate.
    private async Task WaitForResumeAsync(CancellationToken ct)
    {
        TaskCompletionSource<bool>? resume = ResumeGate;
        if (resume == null)
            return;

        using (ct.Register(static s => ((TaskCompletionSource<bool>)s!).TrySetResult(true), resume))
            await resume.Task.ConfigureAwait(false);

        ct.ThrowIfCancellationRequested();
    }
}
