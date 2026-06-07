using BlinkTalk.Application.Text;

namespace BlinkTalk.Application.Input.Strategies;

/// <summary>
/// Scans the keys within the active row. Indicating a key types it into the sentence and
/// returns to row scanning. Auto-exits after cycling through the keys about once without a
/// selection (FocusChangeCount > keys + 2), matching the original. Set up by the row selector
/// via <see cref="SetActiveRow"/>.
/// </summary>
public sealed class KeyboardColumnSelectorInputStrategy : IInputStrategy
{
    private IScanController Controller = null!;
    private SentenceBuilder Sentence = null!;
    private int ActiveRow;
    private int KeyCount;
    private int FocusedColumn;
    private FocusCycler? Cycler;

    public void Initialize(IScanController controller)
    {
        Controller = controller;
        Sentence = controller.Sentence;
    }

    public void SetActiveRow(int rowIndex)
    {
        ActiveRow = rowIndex;
        KeyCount = Controller.Keyboard.Rows[rowIndex].Count;
        Cycler?.Stop();
        Cycler = Controller.NewCycler(FocusIndexChanged, firstCycleMultiplier: Consts.FirstCycleDelayMultiplier);
        Cycler.Start(KeyCount);
    }

    public void ReceiveIndication()
    {
        Cycler?.Stop();
        KeyCode key = Controller.Keyboard.Rows[ActiveRow][FocusedColumn];
        Sentence.Input(key);
        Controller.Pop();
    }

    public void ChildStrategyActivated(IInputStrategy childStrategy) { }

    public void Terminated() => Cycler?.Stop();

    private void FocusIndexChanged(int focusIndex)
    {
        FocusedColumn = focusIndex;
        Controller.SetHighlight(HighlightTarget.ForKey(ActiveRow, focusIndex));
        if (Cycler!.FocusChangeCount > KeyCount + 2)
            Controller.Pop();
    }
}
