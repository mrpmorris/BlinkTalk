namespace BlinkTalk.Application.Text.Layouts;

/// <summary>
/// The Dutch keyboard. Every accented letter is a key of its own rather than something composed from a
/// base letter and a mark, the same choice Portuguese makes: one dwell types each, and the spelling
/// matches the word list. That is why Dutch declares no decorators.
/// <para>
/// The letters are the ones CLDR lists as Dutch: the trema that splits a vowel pair into two syllables
/// (België, Oekraïne, coördinatie) and the acute that marks stress (één, vóór, wél). The grave and the
/// circumflex are left off, though the corpus has them — carrière, scène, première, enquête. Every word
/// that takes one is a borrowing, and a key costs every person a wider grid to scan across for as long
/// as the pack lives. The word list is built to match, so nothing it seeds needs a key that is missing.
/// </para>
/// <para>
/// IJ earns no key of its own. It is a digraph rather than a letter, the same call Spanish makes for CH
/// and LL: it is typed as the I and the J it is spelled with, which is how the word list spells it too.
/// </para>
/// </summary>
internal static class DutchKeyboard
{
	public static readonly LanguageKeyboard Keyboard = new LanguageKeyboard
	{
		Alphabetical = [
			[ "A", "Á", "Ä", "B", "C", "D", "E", "É", "Ë" ],
			[ "F", "G", "H", "I", "Ï", "Í", "J", "K", "L" ],
			[ "M", "N", "O", "Ó", "Ö", "P", "Q", "R", "S" ],
			[ "T", "U", "Ü", "Ú", "V", "W", "X", "Y", "Z", "'" ],
			[ "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" ]
		],
		Speed = [
			[ "E", "N", "I", "L", "U", "W", "X", "Q", "Ö" ],
			[ "A", "T", "O", "M", "P", "Z", "É", "Ó", "Í" ],
			[ "R", "D", "S", "H", "B", "F", "Ë", "Ü", "Ä" ],
			[ "G", "V", "K", "J", "C", "Y", "Ï", "Á", "Ú", "'" ],
			[ "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" ]
		]
	};
}
