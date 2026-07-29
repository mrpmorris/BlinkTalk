using System.Collections.Generic;
using System.Threading.Tasks;
using BlinkTalk.Application.Abstractions;
using BlinkTalk.Application.Input;
using BlinkTalk.Application.Text;

namespace BlinkTalk.Application.Tests;

/// <summary>
/// The accent scan level, end to end: the accent key at the start of a letter row opens a strip of
/// diacritics, and picking one continues scanning the same row for the letter to put it on.
/// </summary>
public class AccentScanFlowTests
{
    [Fact]
    public void AccentKeyIsTheFirstKeyOfEachLetterRowForALanguageWithAccents()
    {
        var french = KeyboardLayout.CreateForLanguage("French");

        // First in the row is first in the scan, so reaching it costs a single dwell.
        for (int row = 0; row < 3; row++)
            Assert.Equal(KeyCode.Accent, french.Rows[row][0]);
        // Not on the punctuation or number rows: there is nothing there to accent.
        Assert.DoesNotContain(KeyCode.Accent, french.Rows[3]);
        Assert.DoesNotContain(KeyCode.Accent, french.Rows[4]);
    }

    [Fact]
    public void EnglishHasNoAccentKeyAtAll()
    {
        var english = KeyboardLayout.CreateForLanguage("English");

        Assert.Null(english.AccentScheme);
        Assert.All(english.Rows, row => Assert.DoesNotContain(KeyCode.Accent, row));
    }

    [Fact]
    public async Task PickingAnAccentThenALetterTypesThemTogetherAndReturnsToRowScanning()
    {
        var (controller, indicator, gate) = Build("French");
        await DrillToAccentKeyAsync(controller, indicator, gate, row: 0);

        // Depth 4: the accent strip is being scanned, starting on the acute.
        Assert.Equal(4, controller.Depth);
        Assert.Equal(HighlightKind.AccentMark, controller.Highlight.Kind);
        Assert.Equal(0, controller.Highlight.MarkIndex);
        Assert.NotNull(controller.AccentState);
        Assert.Equal(0, controller.AccentState!.ActiveRowIndex);
        Assert.Null(controller.AccentState.ChosenMarkIndex);

        indicator.Fire(); // pick the acute; scanning moves to the letters of row 0

        Assert.Equal(0, controller.AccentState!.ChosenMarkIndex);
        Assert.Equal(HighlightKind.Key, controller.Highlight.Kind);
        // Straight to E: the accent key itself, Q and W take no acute, and skipped keys consume no
        // dwell. Row 0 is ` Q W E …, so E is column 3.
        Assert.Equal(3, controller.Highlight.ColumnIndex);

        indicator.Fire();

        Assert.Equal("É", controller.Sentence.CurrentWord);
        Assert.Equal(2, controller.Depth);   // back to row scanning, as after any other key
        Assert.Null(controller.AccentState); // and the strip is gone
    }

    [Fact]
    public async Task OnlyMarksThatSomeLetterInTheRowAcceptsAreOffered()
    {
        // French writes the cedilla on C alone, and C is in the bottom row.
        var topRowMarks = await MarksOfferedForRowAsync(row: 0);
        var bottomRowMarks = await MarksOfferedForRowAsync(row: 2);

        Assert.Equal(4, topRowMarks.Count); // acute, grave, circumflex and diaeresis, but no cedilla
        Assert.All(topRowMarks, mark => Assert.False(mark.CanCompose('C')));
        Assert.Equal("Ç", Assert.Single(bottomRowMarks).Compose('C'));
    }

    [Fact]
    public async Task PickingALetterTheMarkCannotTakeIsImpossibleBecauseThoseKeysAreSkipped()
    {
        var (controller, indicator, gate) = Build("French");
        await DrillToAccentKeyAsync(controller, indicator, gate, row: 1);
        indicator.Fire();                                   // pick the first mark on offer
        Assert.Equal(HighlightKind.Key, controller.Highlight.Kind);

        // Row 1 is A S D F G H J K L, of which only A takes any French accent — so every focus in
        // this phase lands on a letter the chosen mark composes, however long the scan runs.
        var marks = controller.AccentState!.Marks;
        AccentMark chosen = marks[controller.AccentState.ChosenMarkIndex!.Value];
        for (int step = 0; step < 4 && controller.Depth == 4; step++)
        {
            KeyCode focused = controller.Keyboard.Rows[1][controller.Highlight.ColumnIndex];
            Assert.True(KeyCharacters.TryGetLetter(focused, out char letter));
            Assert.True(chosen.CanCompose(letter));
            await gate.StepAsync();
        }
    }

    [Fact]
    public async Task GivingUpWhileChoosingAnAccentReturnsToScanningTheSameRowsKeys()
    {
        var (controller, indicator, gate) = Build("French");
        await DrillToAccentKeyAsync(controller, indicator, gate, row: 0);
        int marks = controller.AccentState!.Marks.Count;

        // The accent level pops when FocusChangeCount > marks + 1.
        for (int i = 0; i < marks + 1; i++)
            await gate.StepAsync();

        Assert.Equal(3, controller.Depth);   // key scanning, not thrown back to the rows
        Assert.Null(controller.AccentState);
        // And that key scan is live: the next tick moves the highlight along the row.
        await gate.StepAsync();
        Assert.Equal(HighlightKind.Key, controller.Highlight.Kind);
    }

    [Fact]
    public async Task GivingUpWhileChoosingALetterReturnsToRowScanning()
    {
        var (controller, indicator, gate) = Build("French");
        await DrillToAccentKeyAsync(controller, indicator, gate, row: 0);
        indicator.Fire(); // pick the acute

        // Row 0 offers the acute on E alone, so one more focus change than that pops the level.
        for (int i = 0; i < 3; i++)
            await gate.StepAsync();

        Assert.Equal(2, controller.Depth);
        Assert.Null(controller.AccentState);
        Assert.Equal("", controller.Sentence.CurrentWord);
    }

    [Fact]
    public async Task ArabicHarakatAreTypedOntoTheLetterTheyFollow()
    {
        var (controller, indicator, gate) = Build("Arabic");
        await DrillToAccentKeyAsync(controller, indicator, gate, row: 1);

        indicator.Fire(); // fatha, the first mark
        // Every Arabic letter takes a fatha, so the scan lands on the row's first letter — column 1,
        // since column 0 is the accent key, which is not a letter and so is skipped.
        Assert.Equal(1, controller.Highlight.ColumnIndex);
        indicator.Fire();

        Assert.Equal("شَ", controller.Sentence.CurrentWord);
    }

    /// <summary>The marks the accent key offers from a given row of the French keyboard.</summary>
    private static async Task<IReadOnlyList<AccentMark>> MarksOfferedForRowAsync(int row)
    {
        var (controller, indicator, gate) = Build("French");
        await DrillToAccentKeyAsync(controller, indicator, gate, row);
        return controller.AccentState!.Marks;
    }

    /// <summary>
    /// Scans down to <paramref name="row"/>, along to its accent key, and selects it.
    /// </summary>
    private static async Task DrillToAccentKeyAsync(
        ScanController controller, FakeIndicator indicator, StepDelay gate, int row)
    {
        controller.Start();
        indicator.Fire();                        // into row scanning, on row 0
        for (int i = 0; i < row; i++)
            await gate.StepAsync();
        indicator.Fire();                        // into key scanning, on the accent key at column 0
        indicator.Fire();                        // select it
    }

    private static (ScanController controller, FakeIndicator indicator, StepDelay gate) Build(string language)
    {
        var sentence = new SentenceBuilder(new FakeWordService(), new FakePhraseService());
        var gate = new StepDelay();
        var indicator = new FakeIndicator();
        var controller = new ScanController(
            sentence, new FixedKeyboardLayoutProvider(KeyboardLayout.CreateForLanguage(language)),
            new FakeTextToSpeech(), new FakeSettingsStore(), new InlineUIDispatcher(),
            new[] { indicator }, gate.Delay);
        return (controller, indicator, gate);
    }
}
