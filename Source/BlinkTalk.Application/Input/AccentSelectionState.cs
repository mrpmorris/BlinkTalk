using System.Collections.Generic;
using BlinkTalk.Application.Text;

namespace BlinkTalk.Application.Input;

/// <summary>
/// What the accent scan level is offering, so the UI can show a strip of diacritics beneath the row
/// being worked in. Null on the controller whenever no accent is being picked.
/// </summary>
public sealed class AccentSelectionState
{
    /// <summary>The keyboard row whose letters the mark will be applied to.</summary>
    public int ActiveRowIndex { get; }

    /// <summary>The mark already picked, once the scan has moved on to choosing a letter.</summary>
    public int? ChosenMarkIndex { get; }

    /// <summary>The marks on offer: the language's marks, minus any no letter in this row accepts.</summary>
    public IReadOnlyList<AccentMark> Marks { get; }

    public AccentSelectionState(int activeRowIndex, IReadOnlyList<AccentMark> marks, int? chosenMarkIndex = null)
    {
        ActiveRowIndex = activeRowIndex;
        Marks = marks;
        ChosenMarkIndex = chosenMarkIndex;
    }
}
