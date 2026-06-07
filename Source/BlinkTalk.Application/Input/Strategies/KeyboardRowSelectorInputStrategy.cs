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
    private IScanController _controller = null!;
    private int _focusedRow;
    private int _rowCount;
    private FocusCycler? _cycler;

    public void Initialize(IScanController controller)
    {
        _controller = controller;
        _rowCount = controller.Keyboard.Rows.Count;
        _cycler?.Stop();
        _cycler = controller.NewCycler(FocusIndexChanged, firstCycleMultiplier: Consts.FirstCycleDelayMultiplier);
        _cycler.Start(_rowCount);
    }

    public void ReceiveIndication()
    {
        _cycler?.Stop();
        var columnSelector = _controller.Push<KeyboardColumnSelectorInputStrategy>();
        columnSelector.SetActiveRow(_focusedRow);
    }

    public void ChildStrategyActivated(IInputStrategy childStrategy) => _cycler?.Stop();

    public void Terminated() => _cycler?.Stop();

    private void FocusIndexChanged(int focusIndex)
    {
        _focusedRow = focusIndex;
        _controller.SetHighlight(HighlightTarget.ForKeyboardRow(focusIndex));
        if (_cycler!.FocusChangeCount > _rowCount + 1)
            _controller.Pop();
    }
}
