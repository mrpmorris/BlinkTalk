using System.Collections.Generic;

namespace BlinkTalk.Application.Text;

/// <summary>
/// The on-screen keyboard as rows of keys. The original layout lived as GUID references
/// inside the Unity scene/prefab and was not load-bearing, so this is a clean QWERTY-style
/// grid covering exactly the keys the app supports (letters, digits, basic punctuation,
/// plus Space and Backspace as keys — exactly as in the original SentenceBuilder).
/// Row scanning then column scanning walk this structure.
/// <para>
/// The layout is per language: the script differs (Arabic), and languages that write diacritics
/// get an <see cref="KeyCode.Accent"/> key at the start of each letter row. Rows are in scan order,
/// which for a right-to-left script means index 0 is the rightmost key on screen.
/// </para>
/// </summary>
public sealed class KeyboardLayout
{
    /// <summary>The diacritics the accent key offers, or null when the layout has no accent key.</summary>
    public AccentScheme? AccentScheme { get; }

    /// <summary>Whether the script reads right to left, which the UI mirrors itself for.</summary>
    public bool IsRightToLeft { get; }

    public IReadOnlyList<IReadOnlyList<KeyCode>> Rows { get; }

    public KeyboardLayout(
        IReadOnlyList<IReadOnlyList<KeyCode>> rows,
        AccentScheme? accentScheme = null,
        bool isRightToLeft = false)
    {
        Rows = rows;
        AccentScheme = accentScheme;
        IsRightToLeft = isRightToLeft;
    }

    public static KeyboardLayout CreateDefault() => CreateForLanguage("English");

    /// <summary>
    /// The keyboard for a language, by the name <c>AppLanguage.Name</c> uses. Unknown names get the
    /// Latin layout without accents, which is the same thing English gets.
    /// </summary>
    public static KeyboardLayout CreateForLanguage(string languageName)
    {
        AccentScheme? accents = AccentScheme.ForLanguage(languageName);
        return languageName == "Arabic"
            ? new KeyboardLayout(CreateArabicRows(accents != null), accents, isRightToLeft: true)
            : new KeyboardLayout(CreateLatinRows(accents != null), accents);
    }

    /// <summary>
    /// The standard Arabic keyboard, each row read right to left as it is written — so scanning
    /// starts at the right-hand end of the row, where the eye already is.
    /// </summary>
    private static List<IReadOnlyList<KeyCode>> CreateArabicRows(bool withAccentKey)
    {
        return new List<IReadOnlyList<KeyCode>>
        {
            WithAccentKey(withAccentKey,
                KeyCode.ArabicDad, KeyCode.ArabicSad, KeyCode.ArabicTheh, KeyCode.ArabicQaf,
                KeyCode.ArabicFeh, KeyCode.ArabicGhain, KeyCode.ArabicAin, KeyCode.ArabicHeh,
                KeyCode.ArabicKhah, KeyCode.ArabicHah, KeyCode.ArabicJeem, KeyCode.ArabicDal),
            WithAccentKey(withAccentKey,
                KeyCode.ArabicSheen, KeyCode.ArabicSeen, KeyCode.ArabicYeh, KeyCode.ArabicBeh,
                KeyCode.ArabicLam, KeyCode.ArabicAlef, KeyCode.ArabicTeh, KeyCode.ArabicNoon,
                KeyCode.ArabicMeem, KeyCode.ArabicKaf, KeyCode.ArabicTah),
            WithAccentKey(withAccentKey,
                KeyCode.ArabicThal, KeyCode.ArabicHamza, KeyCode.ArabicReh, KeyCode.ArabicAlefMaksura,
                KeyCode.ArabicTehMarbuta, KeyCode.ArabicWaw, KeyCode.ArabicZain, KeyCode.ArabicZah),
            EditingRow(),
            NumberRow()
        };
    }

    private static List<IReadOnlyList<KeyCode>> CreateLatinRows(bool withAccentKey)
    {
        return new List<IReadOnlyList<KeyCode>>
        {
            WithAccentKey(withAccentKey,
                KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T,
                KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P),
            WithAccentKey(withAccentKey,
                KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G,
                KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L),
            WithAccentKey(withAccentKey,
                KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V,
                KeyCode.B, KeyCode.N, KeyCode.M),
            EditingRow(),
            NumberRow()
        };
    }

    /// <summary>
    /// Space and Backspace. There is no punctuation on the keyboard: every key costs a dwell on
    /// every sweep of the row, and speech does not need it.
    /// </summary>
    private static IReadOnlyList<KeyCode> EditingRow()
    {
        return new[] { KeyCode.Space, KeyCode.Backspace };
    }

    /// <summary>Western digits in every language, as the word lists and clock do.</summary>
    private static IReadOnlyList<KeyCode> NumberRow()
    {
        return new[]
        {
            KeyCode.Number1, KeyCode.Number2, KeyCode.Number3, KeyCode.Number4, KeyCode.Number5,
            KeyCode.Number6, KeyCode.Number7, KeyCode.Number8, KeyCode.Number9, KeyCode.Number0
        };
    }

    /// <summary>
    /// A letter row, starting with the accent key when the language writes diacritics — first in the
    /// row is first in the scan, so it takes one dwell to reach. One per row rather than one for the
    /// keyboard: the accent scan then continues along the row the person is already in, so the letter
    /// they want is a short wait away.
    /// </summary>
    private static IReadOnlyList<KeyCode> WithAccentKey(bool withAccentKey, params KeyCode[] letters)
    {
        if (!withAccentKey)
            return letters;

        var row = new List<KeyCode>(letters.Length + 1) { KeyCode.Accent };
        row.AddRange(letters);
        return row;
    }
}
