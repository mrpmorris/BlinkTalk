using BlinkTalk.Application.Text;

namespace BlinkTalk.Application.Input.Strategies;

/// <summary>
/// Scans the keys within the active row. Indicating a key types it into the sentence and
/// returns to row scanning; indicating the accent key opens the accent level instead. Auto-exits
/// after cycling through the keys about once without a selection (FocusChangeCount > keys + 2),
/// matching the original. Set up by the row selector via <see cref="SetActiveRow"/>.
/// </summary>
public sealed class KeyboardColumnSelectorInputStrategy : IInputStrategy
{
    private int ActiveRow;
    // Whether SetActiveRow has run. Initialize is called again when a child level pops back to
    // here, and the scan has to resume — but the very first Initialize happens before the row is
    // known, and starting a cycle then would scan a row of nothing.
    private bool Configured;
    private IScanController Controller = null!;
    private FocusCycler? Cycler;
    private int FocusedColumn;
    private int KeyCount;
    private SentenceBuilder Sentence = null!;

    public void ChildStrategyActivated(IInputStrategy childStrategy) => Cycler?.Stop();

    public void Initialize(IScanController controller)
    {
        Controller = controller;
        Sentence = controller.Sentence;
        if (Configured)
            SetActiveRow(ActiveRow);
    }

    public void ReceiveIndication()
    {
        Cycler?.Stop();
        KeyCode key = Controller.Keyboard.Rows[ActiveRow][FocusedColumn];
        if (key == KeyCode.Accent)
        {
            Controller.Push<AccentSelectorInputStrategy>().SetActiveRow(ActiveRow);
            return;
        }
        Sentence.Input(key);
        Controller.Pop();
    }

    public void SetActiveRow(int rowIndex)
    {
        ActiveRow = rowIndex;
        Configured = true;
        KeyCount = Controller.Keyboard.Rows[rowIndex].Count;
        Cycler?.Stop();
        Cycler = Controller.NewCycler(FocusIndexChanged, firstCycleMultiplier: Consts.FirstCycleDelayMultiplier);
        Cycler.Start(KeyCount);
    }

    public void Terminated() => Cycler?.Stop();

    private void FocusIndexChanged(int focusIndex)
    {
        FocusedColumn = focusIndex;
        Controller.SetHighlight(HighlightTarget.ForKey(ActiveRow, focusIndex));
        if (Cycler!.FocusChangeCount > KeyCount + 2)
            Controller.Pop();
    }
}
