namespace BlinkTalk.Application.Text.Layouts;

/// <summary>
/// The Portuguese keyboard. Every accented letter is a key of its own rather than something composed
/// from a base letter and a mark: one dwell types it, and it is spelled exactly as the word list
/// spells it. That is why Portuguese declares no decorators.
/// </summary>
internal static class PortugueseKeyboard
{
	public static readonly LanguageKeyboard Keyboard = new LanguageKeyboard
	{
		Alphabetical = [
			[ "A", "Ã", "Á", "Â", "À", "B", "C", "Ç", "D", "E" ],
			[ "É", "Ê", "F", "G", "H", "I", "Í", "J", "K", "L" ],
			[ "M", "N", "O", "Ó", "Õ", "Ô", "P", "Q", "R", "S" ],
			[ "T", "U", "Ú", "V", "W", "X", "Y", "Z", "'" ],
			[ "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" ]
		],
		Speed = [
			[ "A", "E", "R", "M", "G", "Q", "J", "Ó", "K", "Ô" ],
			[ "O", "S", "D", "U", "F", "Ç", "Í", "Ê", "Y", "À" ],
			[ "I", "N", "T", "L", "Ã", "Á", "É", "Õ", "Â" ],
			[ "C", "P", "V", "B", "H", "Z", "X", "Ú", "W", "'" ],
			[ "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" ]
		]
	};
}
