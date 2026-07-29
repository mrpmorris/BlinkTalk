namespace BlinkTalk.Application.Text;

/// <summary>Human-readable labels for keys, for rendering the on-screen keyboard.</summary>
public static class KeyDisplay
{
    public static string Label(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.Space: return "space";
            case KeyCode.Backspace: return "⌫";
            case KeyCode.Accent: return "`";
            // Everything else is labelled with the character it types, so a new key needs no entry
            // here — only in KeyCharacters.
            default: return KeyCharacters.Text(key) ?? key.ToString();
        }
    }
}
