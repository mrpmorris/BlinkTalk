using System.Threading.Tasks;
using BlinkTalk.Application.Abstractions;
using BlinkTalk.Application.Input;
using BlinkTalk.Application.Text;

namespace BlinkTalk.Application.Tests;

/// <summary>
/// The letter-decorator level, end to end: the decorator key at the start of the first row opens a
/// popup of the language's combining marks, and picking one appends it to the word being composed and
/// returns to row scanning, exactly as typing a letter does.
/// </summary>
public class DecoratorScanFlowTests
{
    private const string Alef = "ا";
    private const string Fathatan = "\u064B";
    private const string Shadda = "\u0651";

    [Fact]
    public async Task TheDecoratorKeyOpensThePopupOnTheMostUsedMark()
    {
        var (controller, indicator, gate) = Build(Language.Arabic);
        await DrillToKeyAsync(controller, indicator, gate, row: 0, column: 0);

        indicator.Fire();

        Assert.Equal(4, controller.Depth);
        Assert.True(controller.IsChoosingDecorator);
        Assert.Equal(HighlightKind.Decorator, controller.Highlight.Kind);
        Assert.Equal(0, controller.Highlight.DecoratorIndex);
    }

    [Fact]
    public async Task PickingADecoratorTypesItOntoTheLetterAndReturnsToRowScanning()
    {
        var (controller, indicator, gate) = Build(Language.Arabic);
        // Column 1, because column 0 of the first row is the decorator key itself.
        await DrillToKeyAsync(controller, indicator, gate, row: 0, column: 1);
        indicator.Fire();
        Assert.Equal(Alef, controller.Sentence.CurrentWord);

        indicator.Fire(); // into the keys of row 0, on the decorator key
        indicator.Fire(); // open the popup
        indicator.Fire(); // pick the mark it opened on

        Assert.Equal(Alef + Fathatan, controller.Sentence.CurrentWord);
        Assert.Equal(2, controller.Depth);      // back to row scanning, as after any other key
        Assert.False(controller.IsChoosingDecorator); // and the popup is gone
    }

    [Fact]
    public async Task SeveralDecoratorsAreAppliedByOpeningThePopupAgain()
    {
        var (controller, indicator, gate) = Build(Language.Arabic);
        await DrillToKeyAsync(controller, indicator, gate, row: 0, column: 1);
        indicator.Fire();                       // type the alef
        indicator.Fire();                       // keys of row 0, on the decorator key
        indicator.Fire();                       // open the popup
        indicator.Fire();                       // the first mark

        indicator.Fire();                       // keys of row 0 again, on the decorator key
        indicator.Fire();                       // open the popup again
        await gate.StepAsync();                 // along to the second mark
        indicator.Fire();

        // Marks are appended, not composed, so the second lands after the first.
        Assert.Equal(Alef + Fathatan + Shadda, controller.Sentence.CurrentWord);
    }

    [Fact]
    public async Task GivingUpWhileChoosingADecoratorReturnsToScanningTheSameRowsKeys()
    {
        var (controller, indicator, gate) = Build(Language.Arabic);
        await DrillToKeyAsync(controller, indicator, gate, row: 0, column: 0);
        indicator.Fire();
        int decorators = controller.Keyboard.Decorators.Count;

        // The decorator level pops when FocusChangeCount > decorators + 1.
        for (int i = 0; i < decorators + 1; i++)
            await gate.StepAsync();

        Assert.Equal(3, controller.Depth);   // key scanning, not thrown back to the rows
        Assert.False(controller.IsChoosingDecorator);
        Assert.Empty(controller.Sentence.CurrentWord);
        // And that key scan is live: the next tick moves the highlight along the row.
        await gate.StepAsync();
        Assert.Equal(HighlightKind.Key, controller.Highlight.Kind);
    }

    [Fact]
    public async Task ALanguageWithNoDecoratorsHasALetterInThatFirstPositionInstead()
    {
        var (controller, indicator, gate) = Build(Language.English);
        await DrillToKeyAsync(controller, indicator, gate, row: 0, column: 0);

        indicator.Fire();

        Assert.Equal("A", controller.Sentence.CurrentWord);
        Assert.False(controller.IsChoosingDecorator);
        Assert.Equal(2, controller.Depth);
    }

    private static (ScanController controller, FakeIndicator indicator, StepDelay gate) Build(Language language)
    {
        var sentence = new SentenceBuilder(new FakeWordService(), new FakePhraseService());
        var gate = new StepDelay();
        var indicator = new FakeIndicator();
        var controller = new ScanController(
            sentence,
            new FixedKeyboardLayoutProvider(KeyboardLayout.Create(language, KeyboardLayoutStyle.Alphabetical)),
            new FakeTextToSpeech(), new FakeSettingsStore(), new InlineUIDispatcher(),
            new[] { indicator }, gate.Delay);
        return (controller, indicator, gate);
    }

    /// <summary>
    /// Scans down to <paramref name="row"/> and along to <paramref name="column"/>, leaving the key
    /// focused but not selected — the test fires the indication itself.
    /// </summary>
    private static async Task DrillToKeyAsync(
        ScanController controller, FakeIndicator indicator, StepDelay gate, int row, int column)
    {
        controller.Start();
        indicator.Fire();                        // into row scanning, on row 0
        for (int i = 0; i < row; i++)
            await gate.StepAsync();
        indicator.Fire();                        // into key scanning, on column 0
        for (int i = 0; i < column; i++)
            await gate.StepAsync();
    }
}
