namespace BlinkTalk.Application.Text
{
    /// <summary>Human-readable labels for keys, for rendering the on-screen keyboard.</summary>
    public static class KeyDisplay
    {
        public static string Label(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.Space: return "space";
                case KeyCode.Backspace: return "⌫"; // ⌫
                case KeyCode.Comma: return ",";
                case KeyCode.Period: return ".";
                case KeyCode.Exclaim: return "!";
                case KeyCode.Question: return "?";
                case KeyCode.Number0: return "0";
                case KeyCode.Number1: return "1";
                case KeyCode.Number2: return "2";
                case KeyCode.Number3: return "3";
                case KeyCode.Number4: return "4";
                case KeyCode.Number5: return "5";
                case KeyCode.Number6: return "6";
                case KeyCode.Number7: return "7";
                case KeyCode.Number8: return "8";
                case KeyCode.Number9: return "9";
                default: return key.ToString(); // single letters A–Z
            }
        }
    }
}
