namespace BlinkTalk.Application.Text.Layouts;

/// <summary>
/// One language's keyboard data: its letters in both arrangements, plus any combining marks the
/// language writes. Four rows of letters then a row of digits, exactly as transcribed — the keys
/// that are the same in every language (Space, Backspace, and the decorator key) are inserted by
/// <see cref="KeyboardLayout.Create"/> rather than repeated in every language's file.
/// <para>
/// Both arrangements must contain the same characters; only the order differs. A test enforces it,
/// because a dropped or duplicated letter is the way transcribing one of these goes wrong.
/// </para>
/// </summary>
internal sealed class LanguageKeyboard
{
	public required string[][] Alphabetical { get; init; }

	/// <summary>
	/// The marks the decorator key offers, in the order they are offered — most used first, so the
	/// one most often wanted costs the fewest dwells. Empty for a language that writes none, which
	/// is what leaves the decorator key off its keyboard entirely.
	/// </summary>
	public string[] Decorators { get; init; } = [];

	/// <summary>Whether the script reads right to left, which the UI mirrors itself for.</summary>
	public bool IsRightToLeft { get; init; }

	public required string[][] Speed { get; init; }
}
