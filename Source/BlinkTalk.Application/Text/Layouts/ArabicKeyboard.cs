namespace BlinkTalk.Application.Text.Layouts;

/// <summary>
/// The Arabic keyboard. The precomposed forms — hamza and madda on alef, waw and yeh — are keys of
/// their own, because that is how the word list spells them. The harakat are not: they sit on any
/// letter, so they are decorators, reached through the popup and appended after the letter they
/// belong to.
/// <para>
/// The letters below are in scan order, which for a right-to-left script means the first element of
/// a row is its rightmost key on screen. An editor may render each row's literals right to left, so
/// read the source order, not the visual one.
/// </para>
/// </summary>
internal static class ArabicKeyboard
{
	public static readonly LanguageKeyboard Keyboard = new LanguageKeyboard
	{
		IsRightToLeft = true,
		Alphabetical = [
			[ "ا", "أ", "إ", "ئ", "ء", "ؤ", "آ", "ب", "ت" ],
			[ "ة", "ث", "ج", "ح", "خ", "د", "ذ", "ر", "ز" ],
			[ "س", "ش", "ص", "ض", "ط", "ظ", "ع", "غ", "ف" ],
			[ "ق", "ك", "ل", "م", "ن", "ه", "و", "ي", "ى" ],
			[ "٠", "١", "٢", "٣", "٤", "٥", "٦", "٧", "٨", "٩" ]
		],
		Speed = [
			[ "ا", "ل", "و", "ة", "ه", "ج", "ى", "ذ", "ء" ],
			[ "ي", "م", "ت", "د", "أ", "ش", "خ", "ث", "ظ" ],
			[ "ن", "ر", "ب", "س", "ك", "إ", "ض", "ئ", "ؤ" ],
			[ "ع", "ف", "ق", "ح", "ط", "ص", "ز", "غ", "آ" ],
			[ "٠", "١", "٢", "٣", "٤", "٥", "٦", "٧", "٨", "٩" ]
		],
		// Combining marks have no shape of their own, so a literal here would be an invisible edit
		// waiting to happen — the value is written by code point, named, most-used first. The comment
		// then shows the mark on a dotted circle, which is how Unicode prints one on its own; sitting
		// directly after the name it would attach to the N and be unreadable.
		Decorators =
		[
			"\u064B", // ARABIC FATHATAN ◌ً
			"\u0651", // ARABIC SHADDA ◌ّ
			"\u064F", // ARABIC DAMMA ◌ُ
			"\u064E", // ARABIC FATHA ◌َ
			"\u0650", // ARABIC KASRA ◌ِ
			"\u064D", // ARABIC KASRATAN ◌ٍ
			"\u0652", // ARABIC SUKUN ◌ْ
			"\u064C", // ARABIC DAMMATAN ◌ٌ
			"\u0670"  // ARABIC LETTER SUPERSCRIPT ALEF ◌ٰ
		]
	};
}
