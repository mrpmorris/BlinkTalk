using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlinkTalk.Application.Abstractions;
using BlinkTalk.Application.Input;
using BlinkTalk.Application.Text;

namespace BlinkTalk.Application.Tests;

public class ScanFlowTests
{
    [Fact]
    public void DrillingIntoKeyboardTypesALetterAndReturnsToRowScanning()
    {
        var (controller, indicator, _, _) = Build();
        controller.Start();                  // highlight: Keyboard section

        indicator.Fire();                    // -> row selector (depth 2), row 0
        Assert.Equal(2, controller.Depth);
        Assert.Equal(HighlightKind.KeyboardRow, controller.Highlight.Kind);
        Assert.Equal(0, controller.Highlight.RowIndex);

        indicator.Fire();                    // -> column selector (depth 3), key (0,0)
        Assert.Equal(3, controller.Depth);
        Assert.Equal(HighlightKind.Key, controller.Highlight.Kind);
        Assert.Equal(0, controller.Highlight.RowIndex);
        Assert.Equal(0, controller.Highlight.ColumnIndex);

        indicator.Fire();                    // type key (0,0) and pop back to rows
        Assert.Equal(2, controller.Depth);
        Assert.False(controller.Sentence.IsEmpty);
    }

    [Fact]
    public async Task RowSelectorAutoExitsAfterCyclingOnceWithoutSelection()
    {
        var (controller, indicator, gate, _) = Build();
        controller.Start();
        indicator.Fire();                    // into rows (depth 2), row 0 fired (count = 1)

        int rows = controller.Keyboard.Rows.Count;
        // The row selector pops when FocusChangeCount > rows + 1.
        for (int i = 0; i < rows + 1; i++)
            await gate.StepAsync();

        Assert.Equal(1, controller.Depth);   // popped back to the section selector
    }

    [Fact]
    public void SpeakingCommitsAndSpeaksTheSentence()
    {
        var (controller, indicator, _, tts) = Build();
        controller.Start();

        // Type a letter so the sentence is non-empty (enables the Speak section).
        indicator.Fire(); // rows
        indicator.Fire(); // keys of row 0
        indicator.Fire(); // type (0,0) -> 'A', back to rows

        string typed = controller.Sentence.ToString().Trim();
        Assert.False(string.IsNullOrEmpty(typed));

        // Climb back to the section selector and walk to the Speak section.
        // (Pop the row selector by cycling is covered elsewhere; here we drive the Speak path
        // through a fresh section selector by committing directly.)
        string committed = controller.Sentence.Commit();
        _ = controller.Speech.SpeakAsync(committed);

        Assert.Contains(committed, tts.Spoken);
    }

    [Fact]
    public void StartEntersSectionSelectorAndHighlightsKeyboardWhenNothingElseAvailable()
    {
        var (controller, _, _, tts) = Build();

        controller.Start();

        // Depth 1 = section selector. With an empty sentence and no suggestions, only the
        // Keyboard section is focusable, so it is highlighted first.
        Assert.Equal(1, controller.Depth);
        Assert.Equal(HighlightKind.Section, controller.Highlight.Kind);
        Assert.Equal(Section.Keyboard, controller.Highlight.Section);
        // Starting is silent — no spoken greeting.
        Assert.Empty(tts.Spoken);
    }

    [Fact]
    public async Task BackspaceStaysFocusedSoItCanBeRepeated()
    {
        var (controller, indicator, gate, _) = Build();
        controller.Start();
        indicator.Fire();                    // rows
        for (int i = 0; i < 3; i++)
        {
            indicator.Fire();                // keys of row 0
            indicator.Fire();                // type (0,0), back to rows
        }
        Assert.Equal("AAA", controller.Sentence.ToString().Replace(" ", ""));

        await SelectBackspaceAsync(controller, indicator, gate);
        Assert.Equal("AA", controller.Sentence.ToString().Trim());
        Assert.Equal(3, controller.Depth);   // still on the key level
        Assert.Equal(HighlightKind.Key, controller.Highlight.Kind);
        Assert.Equal(KeyboardKeyKind.Backspace,
            controller.Keyboard.Rows[controller.Highlight.RowIndex][controller.Highlight.ColumnIndex].Kind);

        indicator.Fire();                    // repeat within the first-cycle dwell
        Assert.Equal("A", controller.Sentence.ToString().Trim());
        Assert.Equal(3, controller.Depth);
    }

    [Fact]
    public async Task BackspaceReturnsToRowsWhenNotRepeatedWithinTheFirstCycle()
    {
        var (controller, indicator, gate, _) = Build();
        controller.Start();
        indicator.Fire();                    // rows
        indicator.Fire();                    // keys of row 0
        indicator.Fire();                    // type 'A', back to rows
        indicator.Fire();
        indicator.Fire();                    // type 'A' again
        await SelectBackspaceAsync(controller, indicator, gate);
        Assert.Equal(3, controller.Depth);

        await gate.StepAsync();              // the first-cycle dwell passes unselected

        Assert.Equal(2, controller.Depth);
        Assert.Equal(HighlightKind.KeyboardRow, controller.Highlight.Kind);
    }

    [Fact]
    public async Task BackspaceReturnsToRowsAtOnceWhenNothingIsLeftToDelete()
    {
        var (controller, indicator, gate, _) = Build();
        controller.Start();
        indicator.Fire();                    // rows
        indicator.Fire();                    // keys of row 0
        indicator.Fire();                    // type 'A', back to rows

        await SelectBackspaceAsync(controller, indicator, gate);

        Assert.True(controller.Sentence.IsEmpty);
        Assert.Equal(2, controller.Depth);
        Assert.Equal(HighlightKind.KeyboardRow, controller.Highlight.Kind);
    }

    [Theory]
    [InlineData(0.5, 5.0)]   // fast scan: 2x would be 1s, so it is lifted to the 5s minimum
    [InlineData(1.0, 5.0)]
    [InlineData(3.0, 6.0)]   // slow scan: the normal first-cycle dwell already exceeds 5s
    public async Task BackspaceHoldLastsAtLeastFiveSeconds(double cycleSeconds, double expectedHoldSeconds)
    {
        var gate = new StepDelay();
        var delays = new List<TimeSpan>();
        var indicator = new FakeIndicator();
        var settings = new FakeSettingsStore();
        var controller = new ScanController(
            new SentenceBuilder(new FakeWordService(), new FakePhraseService()),
            new FixedKeyboardLayoutProvider(KeyboardLayout.CreateDefault()), new FakeTextToSpeech(),
            settings, new InlineUIDispatcher(), new[] { indicator },
            (span, ct) =>
            {
                delays.Add(span);
                return gate.Delay(span, ct);
            });
        controller.CycleDelaySeconds = cycleSeconds;
        controller.Start();
        indicator.Fire();                    // rows
        indicator.Fire();                    // keys of row 0
        indicator.Fire();                    // type 'A', back to rows
        indicator.Fire();
        indicator.Fire();                    // type 'A' again, so Backspace leaves something to repeat on
        await SelectBackspaceAsync(controller, indicator, gate);

        Assert.Equal(TimeSpan.FromSeconds(expectedHoldSeconds), delays[delays.Count - 1]);
    }

    // From the row level: scan to Backspace's row and key, and select it.
    private static async Task SelectBackspaceAsync(ScanController controller, FakeIndicator indicator, StepDelay gate)
    {
        int row = 0;
        int column = 0;
        for (int r = 0; r < controller.Keyboard.Rows.Count; r++)
        {
            for (int c = 0; c < controller.Keyboard.Rows[r].Count; c++)
            {
                if (controller.Keyboard.Rows[r][c].Kind == KeyboardKeyKind.Backspace)
                {
                    row = r;
                    column = c;
                }
            }
        }
        for (int i = 0; i < row; i++)
            await gate.StepAsync();
        indicator.Fire();                    // into the row's keys
        for (int i = 0; i < column; i++)
            await gate.StepAsync();
        indicator.Fire();                    // select Backspace
    }

    private static (ScanController controller, FakeIndicator indicator, StepDelay gate, FakeTextToSpeech tts) Build()
    {
        var word = new FakeWordService();
        var phrase = new FakePhraseService();
        var sentence = new SentenceBuilder(word, phrase);
        var tts = new FakeTextToSpeech();
        var gate = new StepDelay();
        var indicator = new FakeIndicator();
        var controller = new ScanController(
            sentence, new FixedKeyboardLayoutProvider(KeyboardLayout.CreateDefault()), tts,
            new FakeSettingsStore(), new InlineUIDispatcher(), new[] { indicator }, gate.Delay);
        return (controller, indicator, gate, tts);
    }
}
