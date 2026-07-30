using BlinkTalk.Application.Text;

namespace BlinkTalk.Application.Abstractions;

/// <summary>
/// Supplies the keyboard for the language in use. Behind an interface because the language lives in
/// the host project (it decides resource lookups and the database filename too) and because it can
/// change while the app is running: the person picks a language on the settings page and comes
/// straight back to typing.
/// </summary>
public interface IKeyboardLayoutProvider
{
    KeyboardLayout Current { get; }

    /// <summary>
    /// Which arrangement of the language's letters <see cref="Current"/> uses. Persisted, so this is
    /// also where the settings page reads and writes the choice.
    /// </summary>
    KeyboardLayoutStyle Style { get; set; }
}
