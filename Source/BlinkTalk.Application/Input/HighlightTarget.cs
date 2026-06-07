namespace BlinkTalk.Application.Input;

public enum HighlightKind
{
    None,
    Section,
    KeyboardRow,
    Key,
    WordSuggestion
}

/// <summary>
/// A UI-agnostic description of what is currently highlighted by the scanner. The Razor UI
/// maps this to a CSS class on the corresponding element. Replaces the original's direct
/// reference to a RectTransform on screen.
/// </summary>
public readonly struct HighlightTarget
{
    public HighlightKind Kind { get; }
    public Section Section { get; }
    public int RowIndex { get; }
    public int ColumnIndex { get; }
    public int WordIndex { get; }

    private HighlightTarget(HighlightKind kind, Section section, int rowIndex, int columnIndex, int wordIndex)
    {
        Kind = kind;
        Section = section;
        RowIndex = rowIndex;
        ColumnIndex = columnIndex;
        WordIndex = wordIndex;
    }

    public static readonly HighlightTarget None =
        new HighlightTarget(HighlightKind.None, default, -1, -1, -1);

    public static HighlightTarget ForSection(Section section) =>
        new HighlightTarget(HighlightKind.Section, section, -1, -1, -1);

    public static HighlightTarget ForKeyboardRow(int rowIndex) =>
        new HighlightTarget(HighlightKind.KeyboardRow, default, rowIndex, -1, -1);

    public static HighlightTarget ForKey(int rowIndex, int columnIndex) =>
        new HighlightTarget(HighlightKind.Key, default, rowIndex, columnIndex, -1);

    public static HighlightTarget ForWord(int wordIndex) =>
        new HighlightTarget(HighlightKind.WordSuggestion, default, -1, -1, wordIndex);
}
