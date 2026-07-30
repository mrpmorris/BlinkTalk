namespace BlinkTalk.Application.Text.Layouts;

/// <summary>
/// The English keyboard. Letters are upper case, as the whole UI is, and the apostrophe earns a key
/// because English writes it inside words.
/// </summary>
internal static class EnglishKeyboard
{
	public static readonly LanguageKeyboard Keyboard = new LanguageKeyboard
	{
		Alphabetical = new[]
		{
			new[] { "A", "B", "C", "D", "E", "F", "G" },
			new[] { "H", "I", "J", "K", "L", "M", "N" },
			new[] { "O", "P", "Q", "R", "S", "T", "U" },
			new[] { "V", "W", "X", "Y", "Z", "'" },
			new[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" }
		},
		Speed = new[]
		{
			new[] { "E", "T", "O", "L", "F", "B", "Q" },
			new[] { "A", "I", "S", "C", "P", "K", "Z" },
			new[] { "N", "R", "H", "M", "Y", "J" },
			new[] { "D", "U", "G", "W", "V", "X", "'" },
			new[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" }
		}
	};
}
