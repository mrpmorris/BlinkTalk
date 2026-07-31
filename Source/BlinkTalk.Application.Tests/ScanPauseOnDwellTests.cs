using System;
using System.Threading.Tasks;
using BlinkTalk.Application.Abstractions;
using BlinkTalk.Application.Input;
using BlinkTalk.Application.Text;

namespace BlinkTalk.Application.Tests;

/// <summary>
/// The scan pauses for as long as a held gesture lasts, so a selection lands on the element that
/// was highlighted when the gesture began — the person can't see the screen mid-blink.
/// <see cref="FocusCyclerPauseTests"/> covers the pause mechanics; these cover the controller's
/// wiring of them: which cycler is paused, and the counter that decides when to resume.
/// </summary>
public class ScanPauseOnDwellTests
{
    [Fact]
    public async Task AnEndWithoutAStartLeavesTheNextGestureStillAbleToPause()
    {
        var (controller, indicator, gate) = Build();
        controller.Start();
        indicator.Fire();                                 // into the row selector, row 0
        await WaitUntil(() => controller.Highlight.Kind == HighlightKind.KeyboardRow);
        int dwells = gate.Requested.Count;

        // A gesture released without a matching start (the camera component balances the counter
        // on teardown). If that drove the counter negative the next real hold wouldn't pause.
        indicator.FireDwellEnded();
        indicator.FireDwellStarted();

        await Task.Delay(50);
        Assert.Equal(0, controller.Highlight.RowIndex);
        Assert.Equal(dwells, gate.Requested.Count);

        indicator.FireDwellEnded();
        await WaitUntil(() => gate.Requested.Count > dwells);

        controller.Dispose();
    }

    [Fact]
    public async Task AGestureStillHeldWhenTheNextLevelOpensStartsThatLevelPaused()
    {
        var (controller, indicator, gate) = Build();
        controller.Start();
        int dwells = gate.Requested.Count;                // the section selector is scanning

        // The real order of events: the hold begins, the hold reaches the dwell threshold and
        // indicates, and only then is the gesture released. The cycler the indication starts must
        // therefore begin paused, or the new level scans on while the eyes are still shut.
        indicator.FireDwellStarted();
        indicator.Fire();

        Assert.Equal(HighlightKind.KeyboardRow, controller.Highlight.Kind);
        Assert.Equal(0, controller.Highlight.RowIndex);
        await Task.Delay(50);
        Assert.Equal(dwells, gate.Requested.Count);       // never began timing row 0's dwell

        indicator.FireDwellEnded();
        await WaitUntil(() => gate.Requested.Count > dwells);
        Assert.Equal(0, controller.Highlight.RowIndex);   // resumed on the row it opened on

        controller.Dispose();
    }

    [Fact]
    public async Task AHeldGestureFreezesTheHighlightUntilItEnds()
    {
        var (controller, indicator, gate) = Build();
        controller.Start();
        indicator.Fire();                                 // into the row selector, row 0
        await WaitUntil(() => controller.Highlight.Kind == HighlightKind.KeyboardRow);
        int dwells = gate.Requested.Count;

        indicator.FireDwellStarted();

        await Task.Delay(50);
        Assert.Equal(0, controller.Highlight.RowIndex);    // frozen where the gesture began
        Assert.Equal(dwells, gate.Requested.Count);        // no dwell timing while held

        indicator.FireDwellEnded();
        await WaitUntil(() => gate.Requested.Count > dwells);
        Assert.Equal(0, controller.Highlight.RowIndex);    // resume alone doesn't advance

        gate.Complete();                                   // the remainder of the dwell elapses
        await WaitUntil(() => controller.Highlight.RowIndex == 1);

        controller.Dispose();
    }

    private static (ScanController controller, FakeIndicator indicator, GatedDelay gate) Build()
    {
        var sentence = new SentenceBuilder(new FakeWordService(), new FakePhraseService());
        var gate = new GatedDelay();
        var indicator = new FakeIndicator();
        var controller = new ScanController(
            sentence, new FixedKeyboardLayoutProvider(KeyboardLayout.CreateDefault()),
            new FakeTextToSpeech(), new FakeSettingsStore(), new InlineUIDispatcher(),
            new[] { indicator }, gate.Delay);
        return (controller, indicator, gate);
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
}
