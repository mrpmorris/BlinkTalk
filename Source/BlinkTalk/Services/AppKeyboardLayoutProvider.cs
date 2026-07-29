using System.Collections.Concurrent;
using BlinkTalk.Application.Abstractions;
using BlinkTalk.Application.Text;

namespace BlinkTalk.Services;

/// <summary>
/// The keyboard for whichever language the app is currently running in. Resolved on each access
/// rather than once at startup, because the language can change while the app runs — the person
/// picks one on the settings page and goes straight back to typing. Layouts are immutable, so each
/// one is built once and kept.
/// </summary>
public sealed class AppKeyboardLayoutProvider : IKeyboardLayoutProvider
{
	private readonly ConcurrentDictionary<string, KeyboardLayout> LayoutsByLanguage = new();

	public KeyboardLayout Current =>
		LayoutsByLanguage.GetOrAdd(AppLanguage.Name, KeyboardLayout.CreateForLanguage);
}
