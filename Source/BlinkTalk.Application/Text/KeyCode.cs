namespace BlinkTalk.Application.Text;

/// <summary>
/// The subset of keys the app understands. Replaces UnityEngine.KeyCode from the
/// original project — only the keys present in SentenceBuilder's char map plus the
/// two editing keys (Space, Backspace) are represented.
/// </summary>
public enum KeyCode
{
    A, B, C, D, E, F, G, H, I, J, K, L, M,
    N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
    Number0, Number1, Number2, Number3, Number4,
    Number5, Number6, Number7, Number8, Number9,
    Comma, Period, Exclaim, Question,
    Space, Backspace
}
