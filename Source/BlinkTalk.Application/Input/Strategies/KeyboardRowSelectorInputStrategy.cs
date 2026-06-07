using BlinkTalk.Application.Text;

namespace BlinkTalk.Application.Input.Strategies;

/// <summary>
/// Scans the rows of the keyboard. Indicating a row drills into the column selector for that
/// row. Auto-exits back to the section selector after cycling through the rows about once
/// without a selection (FocusChangeCount > rows + 1), matching the original. Uses the longer
/// first dwell the original applied to row scanning.
/// </summary>
public sealed class KeyboardRowSelectorInputStrategy : IInputStrategy
{
    private IScanController Controller = null!;
    private int FocusedRow;
    private int RowCount;
    private FocusCycler? Cycler;

    public void Initialize(IScanController controller)
    {
        Controller = controller;
        RowCount = controller.Keyboard.Rows.Count;
        Cycler?.Stop();
        Cycler = controller.NewCycler(FocusIndexChanged, firstCycleMultiplier: Consts.FirstCycleDelayMultiplier);
        Cycler.Start(RowCount);
    }

    public void ReceiveIndication()
    {
        Cycler?.Stop();
        var columnSelector = Controller.Push<KeyboardColumnSelectorInputStrategy>();
        columnSelector.SetActiveRow(FocusedRow);
    }

    public void ChildStrategyActivated(IInputStrategy childStrategy) => Cycler?.Stop();

    public void Terminated() => Cycler?.Stop();

    private void FocusIndexChanged(int focusIndex)
    {
        FocusedRow = focusIndex;
        Controller.SetHighlight(HighlightTarget.ForKeyboardRow(focusIndex));
        if (Cycler!.FocusChangeCount > RowCount + 1)
            Controller.Pop();
    }
}
