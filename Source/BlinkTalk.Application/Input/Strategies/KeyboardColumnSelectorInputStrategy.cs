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
    private IScanController _controller = null!;
    private SentenceBuilder _sentence = null!;
    private int _activeRow;
    private int _keyCount;
    private int _focusedColumn;
    private FocusCycler? _cycler;

    public void Initialize(IScanController controller)
    {
        _controller = controller;
        _sentence = controller.Sentence;
    }

    public void SetActiveRow(int rowIndex)
    {
        _activeRow = rowIndex;
        _keyCount = _controller.Keyboard.Rows[rowIndex].Count;
        _cycler?.Stop();
        _cycler = _controller.NewCycler(FocusIndexChanged, firstCycleMultiplier: Consts.FirstCycleDelayMultiplier);
        _cycler.Start(_keyCount);
    }

    public void ReceiveIndication()
    {
        _cycler?.Stop();
        KeyCode key = _controller.Keyboard.Rows[_activeRow][_focusedColumn];
        _sentence.Input(key);
        _controller.Pop();
    }

    public void ChildStrategyActivated(IInputStrategy childStrategy) { }

    public void Terminated() => _cycler?.Stop();

    private void FocusIndexChanged(int focusIndex)
    {
        _focusedColumn = focusIndex;
        _controller.SetHighlight(HighlightTarget.ForKey(_activeRow, focusIndex));
        if (_cycler!.FocusChangeCount > _keyCount + 2)
            _controller.Pop();
    }
}
