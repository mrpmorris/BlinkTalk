using System;
using System.Threading;
using System.Threading.Tasks;
using BlinkTalk.Application.Abstractions;

namespace BlinkTalk.Application.Input
{
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
        private CancellationTokenSource? _cts;

        public int FocusChangeCount { get; private set; }

        public FocusCycler(
            IUiDispatcher dispatcher,
            Action<int> focusChanged,
            Func<TimeSpan> cycleDelay,
            double firstCycleMultiplier = 1,
            Func<int, bool>? mayFocus = null,
            Func<TimeSpan, CancellationToken, Task>? delay = null,
            Action? onExhausted = null)
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
            _ = RunAsync(numberOfItems, cts.Token);
        }

        public void Stop()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
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

                    await _delay(Scale(_cycleDelay(), delayMultiplier), ct).ConfigureAwait(false);
                    delayMultiplier = 1;
                    focusIndex = (firedIndex + 1) % numberOfItems;
                }
            }
            catch (OperationCanceledException)
            {
                // Stop() was called; expected.
            }
        }

        private static TimeSpan Scale(TimeSpan value, double factor) =>
            TimeSpan.FromTicks((long)(value.Ticks * factor));
    }
}
