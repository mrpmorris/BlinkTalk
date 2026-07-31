using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BlinkTalk.Application.Text;

namespace BlinkTalk.Application.Tests;

/// <summary>
/// Guards the three lists that have to agree for a language to be usable: the culture codes the
/// settings dropdown offers, the <see cref="Language"/> members those resolve to, and the languages
/// with letters in <c>Text/Layouts/</c>. Nothing crashes when they drift — the person picks their
/// language, gets a translated UI, and finds an English keyboard under it — so only a test notices.
/// </summary>
public class AppLanguageTests
{
    public static TheoryData<string> EveryOfferedCultureCode()
    {
        var data = new TheoryData<string>();
        foreach (string cultureCode in AppLanguage.OfferedCultureCodes)
            data.Add(cultureCode);
        return data;
    }

    public static TheoryData<Language> EveryLanguage()
    {
        var data = new TheoryData<Language>();
        foreach (Language language in Enum.GetValues<Language>())
            data.Add(language);
        return data;
    }

    [Theory]
    [MemberData(nameof(EveryOfferedCultureCode))]
    public void EveryOfferedCultureCodeIsOneThisMachineKnows(string cultureCode)
    {
        // The settings page skips a code the device does not recognise, so a typo would not throw
        // there — it would quietly shorten the dropdown.
        CultureInfo cultureInfo = CultureInfo.GetCultureInfo(cultureCode);

        Assert.Equal(cultureCode, cultureInfo.Name, ignoreCase: true);
    }

    [Theory]
    [MemberData(nameof(EveryOfferedCultureCode))]
    public void EveryOfferedCultureCodeNamesAWordListLanguage(string cultureCode)
    {
        Language? language = AppLanguage.GetName(CultureInfo.GetCultureInfo(cultureCode));

        Assert.True(language.HasValue,
            $"'{cultureCode}' is offered in the settings dropdown but AppLanguage.GetNameForCode has no " +
            "arm for it or for its two-letter prefix, so it would fall back to English.");
    }

    [Theory]
    [MemberData(nameof(EveryOfferedCultureCode))]
    public void EveryOfferedCultureCodeTypesItsOwnLetters(string cultureCode)
    {
        // Both halves in one assertion, so a code that resolves to nothing reports this message rather
        // than throwing on the null — the theory above is the one that names that cause.
        Language? language = AppLanguage.GetName(CultureInfo.GetCultureInfo(cultureCode));

        Assert.True(language.HasValue && KeyboardLayout.HasLetters(language.Value),
            $"'{cultureCode}' resolves to {language?.ToString() ?? "no language at all"}, which has no " +
            "file in Text/Layouts/ — the person would get a translated UI over an English keyboard, " +
            "missing that language's accents.");
    }

    /// <summary>
    /// The other direction: a language can be given letters and a pack and still never reach anyone,
    /// because nothing added its code to the dropdown.
    /// </summary>
    [Theory]
    [MemberData(nameof(EveryLanguage))]
    public void EveryLanguageIsReachableFromAnOfferedCultureCode(Language language)
    {
        // Codes that resolve to nothing are dropped rather than dereferenced: that is the theory
        // above's failure to report, and letting it throw here would fail every other language too.
        IEnumerable<Language> offered = AppLanguage.OfferedCultureCodes
            .Select(code => AppLanguage.GetName(CultureInfo.GetCultureInfo(code)))
            .Where(name => name.HasValue)
            .Select(name => name!.Value);

        Assert.Contains(language, offered);
    }

    [Theory]
    [MemberData(nameof(EveryOfferedCultureCode))]
    public void EveryOfferedCultureCodeSurvivesBeingPersistedAndRestored(string cultureCode)
    {
        // The round trip the app actually makes: the dropdown writes a code, the next launch reads it
        // back through the same guard that rejects a code support was dropped for.
        Assert.NotNull(AppLanguage.FindSupported(cultureCode));
    }

    [Fact]
    public void TheDefaultCultureIsSupportedAndTypesItsOwnLetters()
    {
        CultureInfo cultureInfo = CultureInfo.GetCultureInfo(AppLanguage.DefaultCultureCode);

        Assert.True(AppLanguage.IsSupported(cultureInfo));
        Assert.True(KeyboardLayout.HasLetters(AppLanguage.GetName(cultureInfo)!.Value));
    }

    [Fact]
    public void TheFallbackLanguageTypesItsOwnLetters()
    {
        // Whatever else is missing, the language every unsupported culture lands on must be complete.
        Assert.True(KeyboardLayout.HasLetters(AppLanguage.Fallback));
    }

    [Fact]
    public void ARegionCodeFallsBackToItsLanguageWhenItHasNoWordListOfItsOwn()
    {
        // Brazilian Portuguese is translated and offered, but types and predicts as Portuguese.
        Assert.Equal(Language.Portuguese, AppLanguage.GetName(CultureInfo.GetCultureInfo("pt-BR")));
        Assert.Equal(Language.Portuguese, AppLanguage.GetName(CultureInfo.GetCultureInfo("pt-PT")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-culture-code")]
    [InlineData("ja")]
    public void FindSupportedRejectsWhatTheAppCannotRunIn(string cultureCode)
    {
        // Empty is "nothing stored yet", the malformed one is a corrupted setting, and Japanese is a
        // real culture the app has no word list for — all three have to end at the default.
        Assert.Null(AppLanguage.FindSupported(cultureCode));
    }
}
