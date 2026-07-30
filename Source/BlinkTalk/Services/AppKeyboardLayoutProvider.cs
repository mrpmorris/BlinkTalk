using System.Collections.Concurrent;
using BlinkTalk.Application;
using BlinkTalk.Application.Abstractions;
using BlinkTalk.Application.Text;

namespace BlinkTalk.Services;

/// <summary>
/// The keyboard for whichever language the app is currently running in, in whichever arrangement the
/// person has chosen. Resolved on each access rather than once at startup, because both can change
/// while the app runs — they are settings, picked on the settings page on the way back to typing.
/// Layouts are immutable, so each one is built once and kept.
/// </summary>
public sealed class AppKeyboardLayoutProvider : IKeyboardLayoutProvider
{
	// Keyed on both axes: keying on language alone would serve the previous arrangement's keyboard
	// for the rest of the session after the person changed it.
	private readonly ConcurrentDictionary<(Language Language, KeyboardLayoutStyle Style), KeyboardLayout> Layouts = new();
	private readonly ISettingsStore Settings;

	public AppKeyboardLayoutProvider(ISettingsStore settings)
	{
		Settings = settings;
	}

	public KeyboardLayout Current =>
		Layouts.GetOrAdd((AppLanguage.Name, Style), key => KeyboardLayout.Create(key.Language, key.Style));

	/// <summary>
	/// Persisted as the member name rather than a number, because the store has no integer support
	/// and a name survives a member being added to the enum.
	/// </summary>
	public KeyboardLayoutStyle Style
	{
		get
		{
			string name = Settings.GetString(SettingsKeys.KeyboardLayoutStyle, string.Empty);
			return Enum.TryParse(name, out KeyboardLayoutStyle style) ? style : KeyboardLayoutStyle.Alphabetical;
		}
		set => Settings.SetString(SettingsKeys.KeyboardLayoutStyle, value.ToString());
	}
}
