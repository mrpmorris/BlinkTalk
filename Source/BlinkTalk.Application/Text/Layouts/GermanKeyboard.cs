namespace BlinkTalk.Application.Text.Layouts;

/// <summary>
/// The German keyboard. The umlauted vowels and the eszett are keys of their own rather than marks
/// composed onto a base letter, the same choice Portuguese makes: one dwell types each, and the
/// spelling matches the word list. That is why German declares no decorators. Only the capital forms
/// appear, as the whole UI is upper case — capital ß rather than the SS a German typist would write,
/// because the word list spells the lower-case letter and the key has to round-trip to it.
/// </summary>
internal static class GermanKeyboard
{
	public static readonly LanguageKeyboard Keyboard = new LanguageKeyboard
	{
		Alphabetical = [
			[ "A", "Ä", "B", "C", "D", "E", "F", "G" ],
			[ "H", "I", "J", "K", "L", "M", "N", "O" ],
			[ "Ö", "P", "Q", "R", "S", "ß", "T", "U" ],
			[ "Ü", "V", "W", "X", "Y", "Z", "'" ],
			[ "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" ]
		],
		Speed = [
			[ "E", "N", "T", "U", "B", "Z", "Ö", "X" ],
			[ "I", "R", "A", "G", "F", "V", "J", "Q" ],
			[ "S", "D", "H", "C", "W", "Ü", "ß" ],
			[ "L", "M", "O", "K", "P", "Ä", "Y", "'" ],
			[ "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" ]
		]
	};
}
