using System.Collections.Generic;

namespace BlinkTalk.Application.Text
{
    /// <summary>
    /// The on-screen keyboard as rows of keys. The original layout lived as GUID references
    /// inside the Unity scene/prefab and was not load-bearing, so this is a clean QWERTY-style
    /// grid covering exactly the keys the app supports (letters, digits, basic punctuation,
    /// plus Space and Backspace as keys — exactly as in the original SentenceBuilder).
    /// Row scanning then column scanning walk this structure.
    /// </summary>
    public sealed class KeyboardLayout
    {
        public IReadOnlyList<IReadOnlyList<KeyCode>> Rows { get; }

        public KeyboardLayout(IReadOnlyList<IReadOnlyList<KeyCode>> rows)
        {
            Rows = rows;
        }

        public static KeyboardLayout CreateDefault()
        {
            var rows = new List<IReadOnlyList<KeyCode>>
            {
                new[]
                {
                    KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T,
                    KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P
                },
                new[]
                {
                    KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G,
                    KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L
                },
                new[]
                {
                    KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V,
                    KeyCode.B, KeyCode.N, KeyCode.M
                },
                new[]
                {
                    KeyCode.Space, KeyCode.Backspace,
                    KeyCode.Comma, KeyCode.Period, KeyCode.Exclaim, KeyCode.Question
                },
                new[]
                {
                    KeyCode.Number1, KeyCode.Number2, KeyCode.Number3, KeyCode.Number4, KeyCode.Number5,
                    KeyCode.Number6, KeyCode.Number7, KeyCode.Number8, KeyCode.Number9, KeyCode.Number0
                },
            };
            return new KeyboardLayout(rows);
        }
    }
}
