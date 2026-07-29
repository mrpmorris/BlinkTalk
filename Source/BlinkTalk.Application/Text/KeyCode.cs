namespace BlinkTalk.Application.Text;

/// <summary>
/// The subset of keys the app understands. Replaces UnityEngine.KeyCode from the
/// original project — only the keys present in <see cref="KeyCharacters"/> plus the
/// two editing keys (Space, Backspace) and <see cref="Accent"/> are represented.
/// A given language's keyboard uses a subset: see <see cref="KeyboardLayout.CreateForLanguage"/>.
/// </summary>
public enum KeyCode
{
    A, B, C, D, E, F, G, H, I, J, K, L, M,
    N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
    Number0, Number1, Number2, Number3, Number4,
    Number5, Number6, Number7, Number8, Number9,
    Space, Backspace,

    /// <summary>
    /// Types no character of its own. Selecting it opens a deeper scan level that picks a diacritic
    /// and then the letter in the same row to put it on — see AccentSelectorInputStrategy.
    /// </summary>
    Accent,

    ArabicHamza, ArabicAlef, ArabicBeh, ArabicTehMarbuta, ArabicTeh, ArabicTheh,
    ArabicJeem, ArabicHah, ArabicKhah, ArabicDal, ArabicThal, ArabicReh, ArabicZain,
    ArabicSeen, ArabicSheen, ArabicSad, ArabicDad, ArabicTah, ArabicZah, ArabicAin,
    ArabicGhain, ArabicFeh, ArabicQaf, ArabicKaf, ArabicLam, ArabicMeem, ArabicNoon,
    ArabicHeh, ArabicWaw, ArabicAlefMaksura, ArabicYeh
}
