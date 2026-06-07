using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlinkTalk.Application.Abstractions;
using BlinkTalk.Application.Input;
using Xunit;

namespace BlinkTalk.Application.Tests;

/// <summary>
/// The camera indicator pauses the scan while a gesture is held (via ScanController), so the
/// highlight can't move out from under a selection. These cover FocusCycler's pause/resume:
/// the highlight freezes, and the paused span is excluded so the dwell resumes with only the
/// time that was remaining (the "1s cycle paused 0.5–0.7s next advances at 1.2s" behaviour).
/// </summary>
public class FocusCyclerPauseTests
{
    private static FocusCycler BuildPausable(double delaySeconds, List<int> fired, GatedDelay gate, IClock clock)
    {
        return new FocusCycler(
            new InlineUiDispatcher(),
            i => fired.Add(i),
            () => TimeSpan.FromSeconds(delaySeconds),
            firstCycleMultiplier: 1,
            mayFocus: _ => true,
            delay: gate.Delay,
            clock: clock);
    }

    private static async Task WaitUntil(Func<bool> condition, int timeoutMs = 2000)
    {
        int start = Environment.TickCount;
        while (!condition())
        {
            if (Environment.TickCount - start > timeoutMs)
                throw new TimeoutException("Condition was not met within the timeout.");
            await Task.Delay(5);
        }
    }

    [Fact]
    public async Task PauseBeforeDwellHoldsTheHighlightUntilResume()
    {
        var fired = new List<int>();
        var gate = new GatedDelay();
        var clock = new FixedClock();
        var cycler = BuildPausable(1.0, fired, gate, clock);

        cycler.Pause();
        cycler.Start(2);              // fires 0, then holds before entering any dwell

        await Task.Delay(50);
        Assert.Equal(new[] { 0 }, fired);
        Assert.Empty(gate.Requested); // never started a real dwell while paused

        cycler.Resume();
        await WaitUntil(() => gate.Requested.Count == 1);
        Assert.Equal(1.0, gate.Requested[0].TotalSeconds, 3); // full dwell once resumed
        Assert.Equal(new[] { 0 }, fired);                     // still on item 0 (dwell not done)

        gate.Complete();
        await WaitUntil(() => fired.Count == 2);
        Assert.Equal(new[] { 0, 1 }, fired);

        cycler.Stop();
    }

    [Fact]
    public async Task ExcludesPausedTimeAndResumesWithTheRemainder()
    {
        var fired = new List<int>();
        var gate = new GatedDelay();
        var clock = new FixedClock();
        var cycler = BuildPausable(1.0, fired, gate, clock);

        cycler.Start(2);                         // fires 0, enters the 1s dwell
        await WaitUntil(() => gate.Requested.Count == 1);
        Assert.Equal(1.0, gate.Requested[0].TotalSeconds, 3);
        Assert.Equal(new[] { 0 }, fired);

        clock.UtcNow = clock.UtcNow.AddSeconds(0.5); // 0.5s of the dwell has elapsed
        cycler.Pause();                              // interrupts the dwell and freezes

        await Task.Delay(50);
        Assert.Equal(new[] { 0 }, fired);  // highlight frozen
        Assert.Single(gate.Requested);     // no new dwell while paused

        cycler.Resume();
        await WaitUntil(() => gate.Requested.Count == 2);
        // Resumed with only the 0.5s that was remaining — paused time excluded.
        Assert.Equal(0.5, gate.Requested[1].TotalSeconds, 3);
        Assert.Equal(new[] { 0 }, fired);

        gate.Complete();
        await WaitUntil(() => fired.Count == 2);
        Assert.Equal(new[] { 0, 1 }, fired);

        cycler.Stop();
    }

    [Fact]
    public async Task StopWhilePausedExitsCleanlyWithoutAdvancing()
    {
        var fired = new List<int>();
        var gate = new GatedDelay();
        var clock = new FixedClock();
        var cycler = BuildPausable(1.0, fired, gate, clock);

        cycler.Start(2);
        await WaitUntil(() => gate.Requested.Count == 1);
        cycler.Pause();
        await Task.Delay(30);

        cycler.Stop();          // must not throw, must not advance
        gate.Complete();        // completing the abandoned dwell does nothing now

        await Task.Delay(30);
        Assert.Equal(new[] { 0 }, fired);
    }
}
