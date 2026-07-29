using System;
using System.Collections.Generic;

namespace BlinkTalk.Application.Text;

/// <summary>
/// One diacritic offered by the accent key: how to show it, which letters it goes on, and what the
/// two of them together type.
/// </summary>
public sealed class AccentMark
{
    /// <summary>
    /// A dotted circle is the Unicode convention for showing a diacritic on its own — a combining
    /// mark with no letter to sit on renders as nothing at all. Showing the mark rather than an
    /// example letter also avoids implying that picking it types that letter.
    /// </summary>
    private const string DottedCircle = "◌";

    /// <summary>What the accent-mark strip shows for this mark, e.g. "◌́" or "ẞ".</summary>
    public string DisplayGlyph { get; }

    private readonly Func<char, string?> Composer;

    private AccentMark(string displayGlyph, Func<char, string?> composer)
    {
        DisplayGlyph = displayGlyph;
        Composer = composer;
    }

    /// <summary>
    /// A mark whose letters have a precomposed form — É rather than E plus a combining acute. That
    /// is how the word lists are written, so it is what the European accents use.
    /// </summary>
    public static AccentMark Precomposed(char combiningCharacter, params (char Letter, string Composed)[] composed)
    {
        var map = new Dictionary<char, string>(composed.Length);
        foreach ((char letter, string result) in composed)
            map[letter] = result;
        return new AccentMark(
            DottedCircle + combiningCharacter,
            letter => map.TryGetValue(letter, out string? result) ? result : null);
    }

    /// <summary>
    /// A mark appended to its letter as a combining character, which is how the Arabic harakat
    /// work: there is no precomposed form, the letter simply carries the mark.
    /// </summary>
    public static AccentMark Combining(char combiningCharacter, Func<char, bool> appliesTo)
    {
        return new AccentMark(
            DottedCircle + combiningCharacter,
            letter => appliesTo(letter) ? letter.ToString() + combiningCharacter : null);
    }

    /// <summary>
    /// A mark shown as a glyph of its own, for the one case that is not a diacritic at all: the
    /// German ß, which the keyboard offers as an alternative form of S.
    /// </summary>
    public static AccentMark AlternativeLetter(string displayGlyph, char letter, string composed) =>
        new AccentMark(displayGlyph, typed => typed == letter ? composed : null);

    public bool CanCompose(char letter) => Composer(letter) != null;

    /// <summary>What to type for this mark on <paramref name="letter"/>, or null if it does not apply.</summary>
    public string? Compose(char letter) => Composer(letter);
}
