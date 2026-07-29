using System.Globalization;
using System.Text;

namespace BlinkTalk.Application.Text;

/// <summary>
/// The two folding rules the dictionary is matched with.
/// <para>
/// <see cref="Fold"/> produces the search key stored in Words.SearchWord: case, diacritics and
/// the Arabic letter variants all collapse, so typing CAFE finds "café" and كتب finds كَتَبَ.
/// SQLite's own folding cannot do this — the shipped e_sqlite3 build has no ICU, so both LIKE
/// and COLLATE NOCASE fold ASCII only.
/// </para>
/// <para>
/// <see cref="FoldCase"/> collapses case alone and is used where accents must stay meaningful —
/// notably deciding whether two dictionary rows are the same word ("the"/"THE" are; "ou"/"où"
/// are not).
/// </para>
/// </summary>
public static class TextFold
{
    // Purely decorative letter-stretching; it carries no meaning and must not affect matching.
    private const char ArabicTatweel = 'ـ';
    private const char ArabicTehMarbuta = 'ة';
    private const char ArabicHeh = 'ه';
    private const char ArabicAlefMaksura = 'ى';
    private const char ArabicYeh = 'ي';
    private const char LatinCapitalAe = 'Æ';
    private const char LatinCapitalOe = 'Œ';

    /// <summary>
    /// The search key: <see cref="FoldCase"/> plus diacritic removal and Arabic unification.
    /// Idempotent, so a folded string can safely be folded again.
    /// </summary>
    public static string Fold(string? text)
    {
        string upper = FoldCase(text);
        if (upper.Length == 0)
            return upper;

        string decomposed = upper.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (char character in decomposed)
        {
            // Decomposition has split every diacritic off its base letter, so one category test
            // removes the European accents and the Arabic harakat alike. It also covers the
            // hamza and madda of أ إ آ ؤ ئ, which is why those unify with plain alef/waw/yeh
            // without a rule of their own.
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                continue;
            if (character == ArabicTatweel)
                continue;

            switch (character)
            {
                case ArabicTehMarbuta:
                    builder.Append(ArabicHeh);
                    break;
                case ArabicAlefMaksura:
                    builder.Append(ArabicYeh);
                    break;
                // Ligatures have no canonical decomposition, so spell them out by hand.
                case LatinCapitalAe:
                    builder.Append("AE");
                    break;
                case LatinCapitalOe:
                    builder.Append("OE");
                    break;
                default:
                    builder.Append(character);
                    break;
            }
        }
        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Upper cases invariantly, keeping accents. Invariant rather than culture-sensitive so the
    /// same word folds identically whichever language the app is running in — a Turkish culture
    /// would otherwise map "i" to "İ" and never match a stored "I".
    /// </summary>
    public static string FoldCase(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        // Upper casing leaves ß as ß, so spell it out first: German writes the uppercase form as
        // SS, and that is how the word lists spell it.
        string expanded = text!.Replace("ß", "SS").Replace("ẞ", "SS");
        return expanded.ToUpperInvariant();
    }
}
