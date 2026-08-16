using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.InteropServices;

namespace LanguagePackBuilder;

/// <summary>
/// Validates words against the alphabet of a culture's language using Unicode CLDR
/// exemplar character sets, accessed via the ICU library that ships with Windows 10+.
/// No alphabet data is hardcoded; CLDR is the authority on which letters a locale uses.
/// </summary>
public static class IcuAlphabet
{
	// uset.h
	private const uint USET_CASE_INSENSITIVE = 2;
	// ulocdata.h ULocaleDataExemplarSetType
	private const int ULOCDATA_ES_STANDARD = 0;
	private const int ULOCDATA_ES_AUXILIARY = 1;

	// USet pointers are frozen and cached for the process lifetime, never closed.
	private static readonly ConcurrentDictionary<string, IntPtr> ExemplarSets = new();


	/// <summary>
	/// Returns true only if every character in <paramref name="word"/> is a letter of the
	/// given culture's alphabet (CLDR standard exemplar set). Matching is case-insensitive,
	/// so uppercased input matches the lowercase exemplar data (including Turkish İ/ı).
	/// Non-letters such as digits, spaces and punctuation always fail.
	/// </summary>
	/// <param name="includeAuxiliary">
	/// Also accept the CLDR auxiliary set (loanword characters, e.g. é for English).
	/// </param>
	public static bool IsValidWord(CultureInfo culture, string word, bool includeAuxiliary = false)
	{
		if (string.IsNullOrWhiteSpace(word))
			return false;

		IntPtr set = SetFor(culture, includeAuxiliary);

		for (int i = 0; i < word.Length;)
		{
			bool isPair = char.IsSurrogatePair(word, i);
			if (char.IsSurrogate(word[i]) && !isPair)
				return false;
			int codePoint = char.ConvertToUtf32(word, i);
			if (uset_contains(set, codePoint) == 0)
				return false;
			i += isPair ? 2 : 1;
		}
		return true;
	}

	/// <summary>
	/// True if <paramref name="character"/> is a letter of the culture's own alphabet — CLDR's standard
	/// exemplar set, which is the letters needed to write the language's own words and nothing else.
	/// The auxiliary set is deliberately not consulted: that is where the borrowings live, the <c>è</c>
	/// of <c>carrière</c> and the <c>ç</c> of <c>Curaçao</c>, and a letter that only a borrowing needs
	/// is not worth a key on a keyboard somebody drives one dwell at a time.
	/// </summary>
	public static bool IsStandardLetter(CultureInfo culture, char character) =>
		uset_contains(SetFor(culture, includeAuxiliary: false), character) != 0;

	/// <summary>
	/// True if <paramref name="character"/> is a combining mark rather than a letter of its own — the
	/// Arabic harakat, the Devanagari matras, the Thai vowel and tone marks. CLDR lists these in a
	/// locale's exemplar set because they are part of the writing system, so <see cref="IsValidWord"/>
	/// accepts words that contain them, but they are not keys: the app reaches them through the accent
	/// popup instead.
	/// <para>
	/// The Unicode General Category is the test here, not the Unicode Diacritic property. The latter
	/// sounds purpose-built but splits the wrong way: it reports false for U+0670 (Arabic superscript
	/// alef) and true for the Thai tone marks and the Devanagari virama.
	/// </para>
	/// </summary>
	public static bool IsCombiningMark(char character) =>
		CharUnicodeInfo.GetUnicodeCategory(character) is
			UnicodeCategory.NonSpacingMark or
			UnicodeCategory.SpacingCombiningMark or
			UnicodeCategory.EnclosingMark;

	/// <summary>The culture's exemplar set, opened once and then served from the cache.</summary>
	private static IntPtr SetFor(CultureInfo culture, bool includeAuxiliary) =>
		ExemplarSets.GetOrAdd(
			$"{culture.Name}|{includeAuxiliary}",
			_ => OpenExemplarSet(culture, includeAuxiliary));

	private static IntPtr OpenExemplarSet(CultureInfo culture, bool includeAuxiliary)
	{
		string localeId = culture.Name.Replace('-', '_');
		int status = 0;
		IntPtr localeData = ulocdata_open(localeId, ref status);
		if (status > 0 || localeData == IntPtr.Zero)
			throw new InvalidOperationException($"ICU could not open locale data for \"{culture.Name}\" (status {status}).");

		try
		{
			IntPtr set = ulocdata_getExemplarSet(localeData, IntPtr.Zero, USET_CASE_INSENSITIVE, ULOCDATA_ES_STANDARD, ref status);
			if (status > 0 || set == IntPtr.Zero)
				throw new InvalidOperationException($"ICU could not load the exemplar character set for \"{culture.Name}\" (status {status}).");

			if (includeAuxiliary)
			{
				IntPtr aux = ulocdata_getExemplarSet(localeData, IntPtr.Zero, USET_CASE_INSENSITIVE, ULOCDATA_ES_AUXILIARY, ref status);
				if (status > 0 || aux == IntPtr.Zero)
					throw new InvalidOperationException($"ICU could not load the auxiliary exemplar set for \"{culture.Name}\" (status {status}).");
				uset_addAll(set, aux);
				uset_close(aux);
			}

			// Frozen sets are immutable and safe to share across threads.
			uset_freeze(set);
			return set;
		}
		finally
		{
			ulocdata_close(localeData);
		}
	}

	/// <summary>
	/// Returns the ten digits of the culture's native CLDR numbering system, in value order,
	/// e.g. 0-9 for en-GB, ٠-٩ for ar-AE and ०-९ for hi-IN. Sourced from ICU because .NET's
	/// <see cref="NumberFormatInfo.NativeDigits"/> follows CLDR's *default* numbering system —
	/// the one a locale conventionally formats numbers with — which is latn for ar-AE, ar-DZ,
	/// hi-IN and th-TH even though each of those languages has digits of its own. The native
	/// system is therefore requested explicitly, falling back to the default if it is missing.
	/// Locales whose native system is algorithmic (Roman numerals and the like, which have no
	/// digit list) fall back to CLDR's "latn" system.
	/// </summary>
	public static string[] GetDigits(CultureInfo culture)
	{
		string localeId = culture.Name.Replace('-', '_');
		int status = 0;
		IntPtr numberingSystem = unumsys_open($"{localeId}@numbers=native", ref status);
		if (status > 0 || numberingSystem == IntPtr.Zero)
		{
			if (numberingSystem != IntPtr.Zero)
				unumsys_close(numberingSystem);
			status = 0;
			numberingSystem = unumsys_open(localeId, ref status);
			if (status > 0 || numberingSystem == IntPtr.Zero)
				throw new InvalidOperationException($"ICU could not open the numbering system for \"{culture.Name}\" (status {status}).");
		}

		if (unumsys_isAlgorithmic(numberingSystem) != 0)
		{
			unumsys_close(numberingSystem);
			status = 0;
			numberingSystem = unumsys_openByName("latn", ref status);
			if (status > 0 || numberingSystem == IntPtr.Zero)
				throw new InvalidOperationException($"ICU could not open the \"latn\" numbering system (status {status}).");
		}

		try
		{
			// For a positional system the description is simply its digits, lowest value first.
			char[] buffer = new char[64];
			int length = unumsys_getDescription(numberingSystem, buffer, buffer.Length, ref status);
			if (status > 0 || length <= 0 || length > buffer.Length)
				throw new InvalidOperationException($"ICU could not read the digits for \"{culture.Name}\" (status {status}).");

			string description = new string(buffer, 0, length);
			List<string> digits = new List<string>();
			for (int i = 0; i < description.Length;)
			{
				int size = char.IsSurrogatePair(description, i) ? 2 : 1;
				digits.Add(description.Substring(i, size));
				i += size;
			}
			return digits.ToArray();
		}
		finally
		{
			unumsys_close(numberingSystem);
		}
	}

	// Unsuffixed exports exist only in the combined icu.dll of Windows 10 1903+ / Windows 11.
	// Cross-platform would need Icu.Net instead.
	[DllImport("icu.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false)]
	private static extern IntPtr ulocdata_open(string localeId, ref int status);

	[DllImport("icu.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern void ulocdata_close(IntPtr localeData);

	[DllImport("icu.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern IntPtr ulocdata_getExemplarSet(IntPtr localeData, IntPtr fillIn, uint options, int extype, ref int status);

	[DllImport("icu.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern sbyte uset_contains(IntPtr set, int codePoint);

	[DllImport("icu.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern void uset_addAll(IntPtr set, IntPtr additionalSet);

	[DllImport("icu.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern void uset_close(IntPtr set);

	[DllImport("icu.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern void uset_freeze(IntPtr set);

	[DllImport("icu.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false)]
	private static extern IntPtr unumsys_open(string localeId, ref int status);

	[DllImport("icu.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false)]
	private static extern IntPtr unumsys_openByName(string name, ref int status);

	[DllImport("icu.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern void unumsys_close(IntPtr numberingSystem);

	[DllImport("icu.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern sbyte unumsys_isAlgorithmic(IntPtr numberingSystem);

	[DllImport("icu.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	private static extern int unumsys_getDescription(IntPtr numberingSystem, [Out] char[] result, int resultLength, ref int status);
}
