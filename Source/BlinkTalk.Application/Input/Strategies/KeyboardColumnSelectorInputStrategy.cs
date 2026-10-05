using System;
using BlinkTalk.Application.Text;

namespace BlinkTalk.Application.Input.Strategies;

/// <summary>
/// Scans the keys within the active row. Indicating a key types it into the sentence and
/// returns to row scanning; indicating the decorator key opens the decorator level instead. Auto-exits
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
    // True while Backspace is held focused awaiting a repeat selection.
    private bool Holding;
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
        KeyboardKey key = Controller.Keyboard.Rows[ActiveRow][FocusedColumn];
        if (key.Kind == KeyboardKeyKind.Decorators)
        {
            Controller.Push<DecoratorSelectorInputStrategy>();
            return;
        }
        Sentence.Input(key);
        // Nothing left to delete means there is nothing to repeat, so fall through to the pop.
        if (key.Kind == KeyboardKeyKind.Backspace && !Sentence.IsEmpty)
        {
            HoldOnKey(FocusedColumn);
            return;
        }
        Controller.Pop();
    }

    // Keeps Backspace focused for one first-cycle dwell so it can be repeated. Only that key is
    // focusable, so the cycler's second focus (FocusChangeCount 2) means the dwell passed unselected.
    private void HoldOnKey(int column)
    {
        Holding = true;
        double multiplier = Math.Max(
            Consts.FirstCycleDelayMultiplier,
            Consts.MinimumBackspaceHoldSeconds / Controller.CycleDelaySeconds);
        Controller.SetBackspaceHold(Controller.CycleDelaySeconds * multiplier);
        Cycler = Controller.NewCycler(
            FocusIndexChanged,
            firstCycleMultiplier: multiplier,
            mayFocus: index => index == column);
        Cycler.Start(KeyCount);
    }

    public void SetActiveRow(int rowIndex)
    {
        ActiveRow = rowIndex;
        Configured = true;
        KeyCount = Controller.Keyboard.Rows[rowIndex].Count;
        EndHold();
        Cycler?.Stop();
        Cycler = Controller.NewCycler(FocusIndexChanged, firstCycleMultiplier: Consts.FirstCycleDelayMultiplier);
        Cycler.Start(KeyCount);
    }

    public void Terminated()
    {
        EndHold();
        Cycler?.Stop();
    }

    private void EndHold()
    {
        if (!Holding)
            return;
        Holding = false;
        Controller.SetBackspaceHold(null);
    }

    private void FocusIndexChanged(int focusIndex)
    {
        FocusedColumn = focusIndex;
        Controller.SetHighlight(HighlightTarget.ForKey(ActiveRow, focusIndex));
        if (Cycler!.FocusChangeCount > (Holding ? 1 : KeyCount + 2))
            Controller.Pop();
    }
}
