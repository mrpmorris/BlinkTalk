namespace BlinkTalk.Application.Text;

/// <summary>What a key does when it is selected.</summary>
public enum KeyboardKeyKind
{
	/// <summary>Types <see cref="KeyboardKey.Text"/> into the word being composed.</summary>
	Character,
	Space,
	Backspace,

	/// <summary>Types nothing: opens the letter-decorator popup.</summary>
	Decorators
}

/// <summary>
/// A key on the on-screen keyboard. A character key carries the text it types, so a key can never be
/// scannable but untypable — which is why there is no enum of valid keys and no character map beside
/// it: a language's layout is simply the characters it wants, and everything the scanner and the UI
/// need is on the key itself.
/// </summary>
public sealed class KeyboardKey
{
	/// <summary>The conventional stand-in for the letter a diacritic would sit on.</summary>
	public const string DottedCircle = "◌";

	public KeyboardKeyKind Kind { get; }

	/// <summary>What the UI shows on the key.</summary>
	public string Label { get; }

	/// <summary>The text this key types, or null for the keys that are actions rather than characters.</summary>
	public string? Text { get; }

	public static readonly KeyboardKey Backspace = new KeyboardKey(KeyboardKeyKind.Backspace, null, "⌫");
	public static readonly KeyboardKey Decorators = new KeyboardKey(KeyboardKeyKind.Decorators, null, DottedCircle);
	public static readonly KeyboardKey Space = new KeyboardKey(KeyboardKeyKind.Space, null, "space");

	private KeyboardKey(KeyboardKeyKind kind, string? text, string label)
	{
		Kind = kind;
		Text = text;
		Label = label;
	}

	/// <summary>A key that types <paramref name="text"/>, labelled with what it types.</summary>
	public static KeyboardKey Character(string text) =>
		new KeyboardKey(KeyboardKeyKind.Character, text, text);
}
