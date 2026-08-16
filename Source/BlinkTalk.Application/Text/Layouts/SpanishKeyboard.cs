namespace BlinkTalk.Application.Text.Layouts;

/// <summary>
/// The Spanish keyboard. Each accented vowel, the eñe and the diaeresis U are keys of their own rather
/// than marks composed onto a base letter, the same choice Portuguese makes: one dwell types each, and
/// the spelling matches the word list. That is why Spanish declares no decorators. The digraphs CH and
/// LL earn no keys — they are typed as the two letters they are spelled with, which is how the word
/// list spells them too.
/// </summary>
internal static class SpanishKeyboard
{
	public static readonly LanguageKeyboard Keyboard = new LanguageKeyboard
	{
		Alphabetical = [
			[ "A", "Á", "B", "C", "D", "E", "É", "F", "G" ],
			[ "H", "I", "Í", "J", "K", "L", "M", "N", "Ñ" ],
			[ "O", "Ó", "P", "Q", "R", "S", "T", "U", "Ú" ],
			[ "Ü", "V", "W", "X", "Y", "Z", "'" ],
			[ "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" ]
		],
		Speed = [
			[ "E", "A", "N", "T", "G", "F", "Z", "X", "Ü" ],
			[ "O", "S", "I", "U", "V", "Á", "Y", "Ú" ],
			[ "R", "L", "D", "P", "Ó", "Í", "É", "K" ],
			[ "C", "M", "B", "Q", "H", "J", "Ñ", "W", "'" ],
			[ "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" ]
		]
	};
}
