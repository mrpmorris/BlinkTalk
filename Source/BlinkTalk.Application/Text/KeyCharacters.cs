using System;
using System.Collections.Generic;

namespace BlinkTalk.Application.Text;

/// <summary>
/// What each key types. The single source of truth for both the sentence being built and the
/// on-screen labels, so a key can never be scannable but untypable.
/// <para>
/// Letters are upper case: the whole UI displays upper case, and matching against the dictionary
/// is case-folded anyway (see <see cref="TextFold"/>). Space, Backspace and
/// <see cref="KeyCode.Accent"/> type nothing and are deliberately absent — they are handled as
/// actions, not characters.
/// </para>
/// </summary>
public static class KeyCharacters
{
    private static readonly Dictionary<KeyCode, string> TextByKeyCode = BuildMap();

    /// <summary>The text a key types, or null for the keys that are actions rather than characters.</summary>
    public static string? Text(KeyCode key) => TextByKeyCode.TryGetValue(key, out string? text) ? text : null;

    /// <summary>
    /// The single character an accent can be applied to, for keys that type exactly one.
    /// </summary>
    public static bool TryGetLetter(KeyCode key, out char letter)
    {
        string? text = Text(key);
        letter = text != null && text.Length == 1 ? text[0] : '\0';
        return letter != '\0';
    }

    /// <summary>The text a key types. Throws for keys that type nothing, which is a layout bug.</summary>
    public static string TextOf(KeyCode key) =>
        Text(key) ?? throw new ArgumentOutOfRangeException(nameof(key), key, "Key types no character.");

    private static Dictionary<KeyCode, string> BuildMap()
    {
        var map = new Dictionary<KeyCode, string>
        {
            { KeyCode.A, "A" }, { KeyCode.B, "B" }, { KeyCode.C, "C" }, { KeyCode.D, "D" },
            { KeyCode.E, "E" }, { KeyCode.F, "F" }, { KeyCode.G, "G" }, { KeyCode.H, "H" },
            { KeyCode.I, "I" }, { KeyCode.J, "J" }, { KeyCode.K, "K" }, { KeyCode.L, "L" },
            { KeyCode.M, "M" }, { KeyCode.N, "N" }, { KeyCode.O, "O" }, { KeyCode.P, "P" },
            { KeyCode.Q, "Q" }, { KeyCode.R, "R" }, { KeyCode.S, "S" }, { KeyCode.T, "T" },
            { KeyCode.U, "U" }, { KeyCode.V, "V" }, { KeyCode.W, "W" }, { KeyCode.X, "X" },
            { KeyCode.Y, "Y" }, { KeyCode.Z, "Z" },
            { KeyCode.Number0, "0" }, { KeyCode.Number1, "1" }, { KeyCode.Number2, "2" },
            { KeyCode.Number3, "3" }, { KeyCode.Number4, "4" }, { KeyCode.Number5, "5" },
            { KeyCode.Number6, "6" }, { KeyCode.Number7, "7" }, { KeyCode.Number8, "8" },
            { KeyCode.Number9, "9" },

            { KeyCode.ArabicHamza, "ء" }, { KeyCode.ArabicAlef, "ا" }, { KeyCode.ArabicBeh, "ب" },
            { KeyCode.ArabicTehMarbuta, "ة" }, { KeyCode.ArabicTeh, "ت" }, { KeyCode.ArabicTheh, "ث" },
            { KeyCode.ArabicJeem, "ج" }, { KeyCode.ArabicHah, "ح" }, { KeyCode.ArabicKhah, "خ" },
            { KeyCode.ArabicDal, "د" }, { KeyCode.ArabicThal, "ذ" }, { KeyCode.ArabicReh, "ر" },
            { KeyCode.ArabicZain, "ز" }, { KeyCode.ArabicSeen, "س" }, { KeyCode.ArabicSheen, "ش" },
            { KeyCode.ArabicSad, "ص" }, { KeyCode.ArabicDad, "ض" }, { KeyCode.ArabicTah, "ط" },
            { KeyCode.ArabicZah, "ظ" }, { KeyCode.ArabicAin, "ع" }, { KeyCode.ArabicGhain, "غ" },
            { KeyCode.ArabicFeh, "ف" }, { KeyCode.ArabicQaf, "ق" }, { KeyCode.ArabicKaf, "ك" },
            { KeyCode.ArabicLam, "ل" }, { KeyCode.ArabicMeem, "م" }, { KeyCode.ArabicNoon, "ن" },
            { KeyCode.ArabicHeh, "ه" }, { KeyCode.ArabicWaw, "و" },
            { KeyCode.ArabicAlefMaksura, "ى" }, { KeyCode.ArabicYeh, "ي" }
        };
        return map;
    }
}
