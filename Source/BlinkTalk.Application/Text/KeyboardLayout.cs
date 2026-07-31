using System;
using System.Collections.Generic;
using BlinkTalk.Application.Text.Layouts;

namespace BlinkTalk.Application.Text;

/// <summary>
/// The on-screen keyboard as rows of keys — four rows of the language's letters then a row of its
/// digits. Row scanning then column scanning walk this structure and the UI renders it, so it is the
/// single source for both.
/// <para>
/// The letters are data: one array per language per <see cref="KeyboardLayoutStyle"/>, in
/// <c>Text/Layouts/</c>. Only the keys that are the same in every language are added here — Space at
/// the start of the third row, Backspace at the start of the fourth, and, for a language that writes
/// combining marks, the decorator key at the start of the first. Rows are in scan order, which for a
/// right-to-left script means index 0 is the rightmost key on screen.
/// </para>
/// </summary>
public sealed class KeyboardLayout
{
	/// <summary>
	/// The row Space leads. The third rather than the fourth: it is pressed once per word, more often
	/// than any single letter, so it is worth a row of scanning less than Backspace.
	/// </summary>
	private const int SpaceRowIndex = 2;

	/// <summary>
	/// The row Backspace leads. Far enough down that the letters a word needs are reached first, near
	/// enough that fixing a mistake stays cheap.
	/// </summary>
	private const int BackspaceRowIndex = 3;

	/// <summary>
	/// Every language whose letters have been transcribed, and the only place that mapping is written
	/// down — <see cref="HasLetters"/> and <see cref="For"/> both read it, so neither can come to
	/// disagree with the other about which languages are ready to type in.
	/// </summary>
	private static readonly IReadOnlyDictionary<Language, LanguageKeyboard> Keyboards =
		new Dictionary<Language, LanguageKeyboard>
		{
			[Language.Arabic] = ArabicKeyboard.Keyboard,
			[Language.English] = EnglishKeyboard.Keyboard,
			[Language.French] = FrenchKeyboard.Keyboard,
			[Language.German] = GermanKeyboard.Keyboard,
			[Language.Portuguese] = PortugueseKeyboard.Keyboard,
			[Language.Spanish] = SpanishKeyboard.Keyboard
		};

	/// <summary>The marks the decorator key offers; empty when the layout has no decorator key.</summary>
	public IReadOnlyList<string> Decorators { get; }

	/// <summary>Whether the script reads right to left, which the UI mirrors itself for.</summary>
	public bool IsRightToLeft { get; }

	public IReadOnlyList<IReadOnlyList<KeyboardKey>> Rows { get; }

	public KeyboardLayout(
		IReadOnlyList<IReadOnlyList<KeyboardKey>> rows,
		IReadOnlyList<string>? decorators = null,
		bool isRightToLeft = false)
	{
		Rows = rows;
		Decorators = decorators ?? Array.Empty<string>();
		IsRightToLeft = isRightToLeft;
	}

	/// <summary>The keyboard for a language, in the arrangement the person has chosen.</summary>
	public static KeyboardLayout Create(Language language, KeyboardLayoutStyle style)
	{
		LanguageKeyboard source = For(language);
		string[][] letters = style == KeyboardLayoutStyle.Speed ? source.Speed : source.Alphabetical;
		bool withDecoratorKey = source.Decorators.Length > 0;

		var rows = new List<IReadOnlyList<KeyboardKey>>(letters.Length);
		for (int rowIndex = 0; rowIndex < letters.Length; rowIndex++)
			rows.Add(BuildRow(rowIndex, letters[rowIndex], withDecoratorKey));
		return new KeyboardLayout(rows, source.Decorators, source.IsRightToLeft);
	}

	public static KeyboardLayout CreateDefault() =>
		Create(Language.English, KeyboardLayoutStyle.Alphabetical);

	/// <summary>
	/// Whether <paramref name="language"/> types its own letters rather than borrowing English's.
	/// <see cref="Create"/> answers for any language either way, so this is how a caller tells the
	/// difference between a keyboard chosen for the person and the fallback standing in for one.
	/// </summary>
	public static bool HasLetters(Language language) => Keyboards.ContainsKey(language);

	/// <summary>
	/// A row of letters, with the fixed keys in front of them: first in the row is first in the scan,
	/// so each is a single dwell away.
	/// </summary>
	private static IReadOnlyList<KeyboardKey> BuildRow(int rowIndex, string[] letters, bool withDecoratorKey)
	{
		var row = new List<KeyboardKey>(letters.Length + 1);
		if (rowIndex == 0 && withDecoratorKey)
			row.Add(KeyboardKey.Decorators);
		if (rowIndex == SpaceRowIndex)
			row.Add(KeyboardKey.Space);
		if (rowIndex == BackspaceRowIndex)
			row.Add(KeyboardKey.Backspace);
		foreach (string letter in letters)
			row.Add(KeyboardKey.Character(letter));
		return row;
	}

	/// <summary>
	/// A language's data. English is the fallback rather than a throw, so a language that reaches
	/// here before its letters have been transcribed still gives the person a usable keyboard.
	/// </summary>
	private static LanguageKeyboard For(Language language) =>
		Keyboards.TryGetValue(language, out LanguageKeyboard? keyboard) ? keyboard : EnglishKeyboard.Keyboard;
}
