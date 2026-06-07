namespace BlinkTalk.Application.Input;

/// <summary>The three top-level sections the section selector scans. Order matters: it
/// is the scan order and must match the original (WordSelector, Keyboard, Speak).</summary>
public enum Section
{
    WordSelector = 0,
    Keyboard = 1,
    Speak = 2
}
