using System.Collections.Generic;

namespace BlinkTalk.Application.Text;

/// <summary>
/// The diacritics a language's accent key offers. Only marks that the language actually writes are
/// listed, because every extra mark is another item the person has to wait out while scanning.
/// </summary>
public sealed class AccentScheme
{
    private const char CombiningAcute = '́';
    private const char CombiningGrave = '̀';
    private const char CombiningCircumflex = '̂';
    private const char CombiningTilde = '̃';
    private const char CombiningDiaeresis = '̈';
    private const char CombiningCedilla = '̧';

    private const char ArabicMaddaAbove = 'ٓ';
    private const char ArabicHamzaAbove = 'ٔ';
    private const char ArabicHamzaBelow = 'ٕ';
    private const char ArabicFathatan = 'ً';
    private const char ArabicDammatan = 'ٌ';
    private const char ArabicKasratan = 'ٍ';
    private const char ArabicFatha = 'َ';
    private const char ArabicDamma = 'ُ';
    private const char ArabicKasra = 'ِ';
    private const char ArabicShadda = 'ّ';
    private const char ArabicSukun = 'ْ';

    public IReadOnlyList<AccentMark> Marks { get; }

    public AccentScheme(IReadOnlyList<AccentMark> marks)
    {
        Marks = marks;
    }

    /// <summary>
    /// The scheme for a language, by the name <c>AppLanguage.Name</c> uses, or null for a language
    /// that writes no diacritics — English, whose keyboard then carries no accent key at all.
    /// </summary>
    public static AccentScheme? ForLanguage(string languageName)
    {
        switch (languageName)
        {
            case "French": return French();
            case "German": return German();
            case "Spanish": return Spanish();
            case "Portuguese": return Portuguese();
            case "Arabic": return Arabic();
            default: return null;
        }
    }

    private static AccentScheme Arabic()
    {
        // The harakat are combining marks: they sit on any letter, and the letter keeps its identity.
        // Hamza and madda are different — they change the letter into one of its own precomposed
        // forms, which is how the word lists spell them.
        return new AccentScheme(new[]
        {
            AccentMark.Combining(ArabicFatha, IsArabicLetter),
            AccentMark.Combining(ArabicKasra, IsArabicLetter),
            AccentMark.Combining(ArabicDamma, IsArabicLetter),
            AccentMark.Combining(ArabicSukun, IsArabicLetter),
            AccentMark.Combining(ArabicShadda, IsArabicLetter),
            AccentMark.Combining(ArabicFathatan, IsArabicLetter),
            AccentMark.Combining(ArabicKasratan, IsArabicLetter),
            AccentMark.Combining(ArabicDammatan, IsArabicLetter),
            AccentMark.Precomposed(ArabicHamzaAbove, ('ا', "أ"), ('و', "ؤ"), ('ي', "ئ")),
            AccentMark.Precomposed(ArabicHamzaBelow, ('ا', "إ")),
            AccentMark.Precomposed(ArabicMaddaAbove, ('ا', "آ"))
        });
    }

    private static AccentScheme French()
    {
        return new AccentScheme(new[]
        {
            AccentMark.Precomposed(CombiningAcute, ('E', "É")),
            AccentMark.Precomposed(CombiningGrave, ('A', "À"), ('E', "È"), ('U', "Ù")),
            AccentMark.Precomposed(CombiningCircumflex,
                ('A', "Â"), ('E', "Ê"), ('I', "Î"), ('O', "Ô"), ('U', "Û")),
            AccentMark.Precomposed(CombiningDiaeresis, ('E', "Ë"), ('I', "Ï"), ('U', "Ü"), ('Y', "Ÿ")),
            AccentMark.Precomposed(CombiningCedilla, ('C', "Ç"))
        });
    }

    private static AccentScheme German()
    {
        return new AccentScheme(new[]
        {
            AccentMark.Precomposed(CombiningDiaeresis, ('A', "Ä"), ('O', "Ö"), ('U', "Ü")),
            // Capital ß. The words are typed in upper case, and folding spells either form as SS,
            // so this matches "straße" in the dictionary just as well as SS would.
            AccentMark.AlternativeLetter("ẞ", 'S', "ẞ")
        });
    }

    /// <summary>
    /// The Arabic letters, hamza (U+0621) through yeh (U+064A) — every key on the Arabic keyboard.
    /// The harakat sit above that range, so a mark can never land on another mark.
    /// </summary>
    private static bool IsArabicLetter(char character) => character >= 'ء' && character <= 'ي';

    private static AccentScheme Portuguese()
    {
        return new AccentScheme(new[]
        {
            AccentMark.Precomposed(CombiningAcute,
                ('A', "Á"), ('E', "É"), ('I', "Í"), ('O', "Ó"), ('U', "Ú")),
            AccentMark.Precomposed(CombiningTilde, ('A', "Ã"), ('O', "Õ")),
            AccentMark.Precomposed(CombiningCircumflex, ('A', "Â"), ('E', "Ê"), ('O', "Ô")),
            AccentMark.Precomposed(CombiningCedilla, ('C', "Ç")),
            AccentMark.Precomposed(CombiningGrave, ('A', "À"))
        });
    }

    private static AccentScheme Spanish()
    {
        return new AccentScheme(new[]
        {
            AccentMark.Precomposed(CombiningAcute,
                ('A', "Á"), ('E', "É"), ('I', "Í"), ('O', "Ó"), ('U', "Ú")),
            AccentMark.Precomposed(CombiningTilde, ('N', "Ñ")),
            AccentMark.Precomposed(CombiningDiaeresis, ('U', "Ü"))
        });
    }
}
