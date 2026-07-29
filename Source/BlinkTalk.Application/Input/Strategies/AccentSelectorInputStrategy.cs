using System;
using System.Collections.Generic;
using BlinkTalk.Application.Text;

namespace BlinkTalk.Application.Input.Strategies;

/// <summary>
/// The accent level, entered by selecting the accent key at the end of a letter row. It scans in
/// two phases: first the diacritics on offer, then — once one is picked — the letters of the same
/// row, skipping any the mark cannot go on. Indicating in the second phase types the letter and its
/// mark together and returns to row scanning, exactly as typing an ordinary key does.
/// <para>
/// Giving up during the first phase returns to scanning the row's keys, so declining an accent does
/// not cost the person the row they had already chosen. Giving up during the second phase returns to
/// row scanning like any other abandoned key.
/// </para>
/// </summary>
public sealed class AccentSelectorInputStrategy : IInputStrategy
{
    private int ActiveRow;
    private AccentMark? ChosenMark;
    private IScanController Controller = null!;
    private FocusCycler? Cycler;
    private int FocusableLetterCount;
    private int FocusedIndex;
    private IReadOnlyList<AccentMark> Marks = Array.Empty<AccentMark>();
    private IReadOnlyList<KeyCode> RowKeys = Array.Empty<KeyCode>();

    public void ChildStrategyActivated(IInputStrategy childStrategy) { }

    public void Initialize(IScanController controller)
    {
        Controller = controller;
    }

    public void ReceiveIndication()
    {
        Cycler?.Stop();
        if (ChosenMark == null)
            StartChoosingLetter();
        else
            TypeAccentedLetter();
    }

    public void SetActiveRow(int rowIndex)
    {
        ActiveRow = rowIndex;
        RowKeys = Controller.Keyboard.Rows[rowIndex];
        Marks = MarksUsableInRow(Controller.Keyboard.AccentScheme);
        if (Marks.Count == 0)
        {
            // No mark applies to anything in this row: nothing to offer, so hand straight back.
            Controller.Pop();
            return;
        }

        Controller.SetAccentState(new AccentSelectionState(ActiveRow, Marks));
        Cycler = Controller.NewCycler(MarkFocusChanged, firstCycleMultiplier: Consts.FirstCycleDelayMultiplier);
        Cycler.Start(Marks.Count);
    }

    public void Terminated()
    {
        Cycler?.Stop();
        Controller.SetAccentState(null);
    }

    private void LetterFocusChanged(int focusIndex)
    {
        FocusedIndex = focusIndex;
        Controller.SetHighlight(HighlightTarget.ForKey(ActiveRow, focusIndex));
        if (Cycler!.FocusChangeCount > FocusableLetterCount + 1)
            Controller.Pop(2);
    }

    private void MarkFocusChanged(int focusIndex)
    {
        FocusedIndex = focusIndex;
        Controller.SetHighlight(HighlightTarget.ForAccentMark(focusIndex));
        if (Cycler!.FocusChangeCount > Marks.Count + 1)
            Controller.Pop();
    }

    /// <summary>
    /// Whether the mark being applied can go on the key at <paramref name="index"/>. Indices that
    /// fail this are skipped without consuming a dwell, so picking the cedilla in the middle row
    /// lands on C immediately rather than after waiting out A, S, D and F.
    /// </summary>
    private bool MayFocusLetter(int index)
    {
        return KeyCharacters.TryGetLetter(RowKeys[index], out char letter)
            && ChosenMark!.CanCompose(letter);
    }

    /// <summary>
    /// The language's marks, minus any that no letter in this row accepts — offering the cedilla in
    /// a row with no C would strand the person in a second phase with nothing to select.
    /// </summary>
    private IReadOnlyList<AccentMark> MarksUsableInRow(AccentScheme? scheme)
    {
        if (scheme == null)
            return Array.Empty<AccentMark>();

        var usable = new List<AccentMark>(scheme.Marks.Count);
        foreach (AccentMark mark in scheme.Marks)
        {
            foreach (KeyCode key in RowKeys)
            {
                if (KeyCharacters.TryGetLetter(key, out char letter) && mark.CanCompose(letter))
                {
                    usable.Add(mark);
                    break;
                }
            }
        }
        return usable;
    }

    private void StartChoosingLetter()
    {
        ChosenMark = Marks[FocusedIndex];
        Controller.SetAccentState(new AccentSelectionState(ActiveRow, Marks, FocusedIndex));

        FocusableLetterCount = 0;
        for (int i = 0; i < RowKeys.Count; i++)
        {
            if (MayFocusLetter(i))
                FocusableLetterCount++;
        }

        Cycler = Controller.NewCycler(
            LetterFocusChanged,
            firstCycleMultiplier: Consts.FirstCycleDelayMultiplier,
            mayFocus: MayFocusLetter,
            onExhausted: () => Controller.Pop(2));
        Cycler.Start(RowKeys.Count);
    }

    private void TypeAccentedLetter()
    {
        KeyCharacters.TryGetLetter(RowKeys[FocusedIndex], out char letter);
        string? composed = ChosenMark!.Compose(letter);
        if (composed != null)
            Controller.Sentence.InputText(composed);
        Controller.Pop(2);
    }
}
