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
/// <c>Text/Layouts/</c>. Only the keys that are the same in every language are added here — Space and
/// Backspace at the start of the fourth row, and, for a language that writes combining marks, the
/// decorator key at the start of the first. Rows are in scan order, which for a right-to-left script
/// means index 0 is the rightmost key on screen.
/// </para>
/// </summary>
public sealed class KeyboardLayout
{
	/// <summary>
	/// The row Space and Backspace go on. The fourth: far enough down that the letters a word needs
	/// are reached first, near enough that finishing a word or fixing a mistake stays cheap.
	/// </summary>
	private const int EditingRowIndex = 3;

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
	/// A row of letters, with the fixed keys in front of them: first in the row is first in the scan,
	/// so each is a single dwell away.
	/// </summary>
	private static IReadOnlyList<KeyboardKey> BuildRow(int rowIndex, string[] letters, bool withDecoratorKey)
	{
		var row = new List<KeyboardKey>(letters.Length + 2);
		if (rowIndex == 0 && withDecoratorKey)
			row.Add(KeyboardKey.Decorators);
		if (rowIndex == EditingRowIndex)
		{
			row.Add(KeyboardKey.Space);
			row.Add(KeyboardKey.Backspace);
		}
		foreach (string letter in letters)
			row.Add(KeyboardKey.Character(letter));
		return row;
	}

	/// <summary>
	/// A language's data. English is the fallback rather than a throw, so a language that reaches
	/// here before its letters have been transcribed still gives the person a usable keyboard.
	/// </summary>
	private static LanguageKeyboard For(Language language)
	{
		switch (language)
		{
			case Language.Portuguese: return PortugueseKeyboard.Keyboard;
			case Language.Arabic: return ArabicKeyboard.Keyboard;
			default: return EnglishKeyboard.Keyboard;
		}
	}
}
