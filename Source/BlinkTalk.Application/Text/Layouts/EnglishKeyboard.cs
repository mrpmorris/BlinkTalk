namespace BlinkTalk.Application.Text.Layouts;

/// <summary>
/// The English keyboard. Letters are upper case, as the whole UI is, and the apostrophe earns a key
/// because English writes it inside words.
/// </summary>
internal static class EnglishKeyboard
{
	public static readonly LanguageKeyboard Keyboard = new LanguageKeyboard
	{
		Alphabetical = [
			[ "A", "B", "C", "D", "E", "F", "G" ],
			[ "H", "I", "J", "K", "L", "M", "N" ],
			[ "O", "P", "Q", "R", "S", "T", "U" ],
			[ "V", "W", "X", "Y", "Z", "'" ],
			[ "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" ]
		],
		Speed = [
			[ "E", "T", "O", "L", "F", "B", "Q" ],
			[ "A", "I", "S", "C", "P", "K", "Z" ],
			[ "N", "R", "H", "M", "Y", "J" ],
			[ "D", "U", "G", "W", "V", "X", "'" ],
			[ "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" ]
		]
	};
}
