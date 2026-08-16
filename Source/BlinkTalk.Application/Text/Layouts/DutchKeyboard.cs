namespace BlinkTalk.Application.Text.Layouts;

/// <summary>
/// The Dutch keyboard. Every accented letter is a key of its own rather than something composed from a
/// base letter and a mark, the same choice Portuguese makes: one dwell types each, and the spelling
/// matches the word list. That is why Dutch declares no decorators.
/// <para>
/// Dutch writes three kinds of accent and all three earn keys: the trema that splits a vowel pair into
/// two syllables (België, Oekraïne, coördinatie), the acute that marks stress (één, vóór, wél), and the
/// grave, which CLDR files under the auxiliary rather than the standard exemplar set because every word
/// that takes it is a borrowing — carrière, scène, première, crème, bèta. Borrowed or not, those are
/// ordinary vocabulary a person wants to type, so È is a key here; Ê joins it for enquête and crêpe.
/// The letters that appear only in foreign names the corpus happens to carry — Ñ, Ø, Å, Æ, Ã, Ç — do
/// not, because every extra key is another column for the scan to cross.
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
		Alphabetical = new[]
		{
			new[] { "A", "Á", "Ä", "B", "C", "D", "E", "É", "Ë", "È" },
			new[] { "Ê", "F", "G", "H", "I", "Ï", "Í", "J", "K", "L" },
			new[] { "M", "N", "O", "Ó", "Ö", "P", "Q", "R", "S", "T" },
			new[] { "U", "Ü", "Ú", "V", "W", "X", "Y", "Z", "'" },
			new[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" }
		},
		Speed = new[]
		{
			new[] { "E", "N", "I", "L", "U", "W", "X", "Q", "Á", "Ú" },
			new[] { "A", "T", "O", "M", "P", "Z", "É", "È", "Ö", "Ê" },
			new[] { "R", "D", "S", "H", "B", "F", "Ë", "Ó", "Í" },
			new[] { "G", "V", "K", "J", "C", "Y", "Ï", "Ü", "Ä", "'" },
			new[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" }
		}
	};
}
