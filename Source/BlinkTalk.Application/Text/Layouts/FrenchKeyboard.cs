namespace BlinkTalk.Application.Text.Layouts;

/// <summary>
/// The French keyboard. Every accented letter and both ligatures are keys of their own rather than
/// something composed from a base letter and a mark, the same choice Portuguese makes: one dwell types
/// each, and the spelling matches the word list. That is why French declares no decorators. It is the
/// widest keyboard the app has — French accents four of its vowels and writes Æ and Œ besides — so the
/// rows run to eleven letters where English runs to seven.
/// </summary>
internal static class FrenchKeyboard
{
	public static readonly LanguageKeyboard Keyboard = new LanguageKeyboard
	{
		Alphabetical = [
			[ "A", "À", "Â", "Æ", "B", "C", "Ç", "D", "E", "É", "È" ],
			[ "Ê", "Ë", "F", "G", "H", "I", "Î", "Ï", "J", "K", "L" ],
			[ "M", "N", "O", "Ô", "Œ", "P", "Q", "R", "S", "T", "U" ],
			[ "Û", "Ù", "Ü", "V", "W", "X", "Y", "Ÿ", "Z", "'" ],
			[ "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" ]
		],
		Speed = [
			[ "E", "S", "I", "O", "V", "B", "Y", "W", "Â", "Œ", "Æ" ],
			[ "A", "N", "T", "C", "F", "J", "K", "Ç", "Î", "Ï", "Ÿ" ],
			[ "R", "U", "L", "M", "Q", "X", "Ê", "Ô", "Û", "Ë" ],
			[ "D", "P", "É", "G", "H", "È", "Z", "À", "Ù", "Ü", "'" ],
			[ "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" ]
		]
	};
}
