using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BlinkTalk.Application.Persistence;

namespace BlinkTalk.Application.Tests;

/// <summary>
/// Guards the filter that stands between a word list and the database. Its job is to drop what the
/// keyboard cannot spell, and — much more importantly — to drop nothing else: the packs the app ships
/// are already built to their own layouts, so a filter that discarded any of their words would be
/// removing vocabulary from somebody who has no other way to say it.
/// </summary>
public class TypeableSeedWordSourceTests
{
    public static TheoryData<Language> EveryLanguage()
    {
        var data = new TheoryData<Language>();
        foreach (Language language in Enum.GetValues<Language>())
            data.Add(language);
        return data;
    }

    /// <summary>
    /// The test the whole thing exists to pass. Every shipped pack must come through untouched — not
    /// merely mostly, exactly — because each word lost is a word its language cannot predict again.
    /// </summary>
    [Theory]
    [MemberData(nameof(EveryLanguage))]
    public void EveryShippedPackPassesThroughUnchanged(Language language)
    {
        var source = new PackSeed(language);
        var before = source.GetWords().ToList();
        var after = new TypeableSeedWordSource(source, language).GetWords().ToList();

        Assert.NotEmpty(before);
        Assert.Equal(before.Count, after.Count);
        // Equal counts would still allow a word to have been rewritten on the way through.
        Assert.Equal(before, after);
    }

    /// <summary>
    /// Arabic writes its vowels as marks that sit on a letter, and they are reached through the
    /// decorator popup rather than from the grid. They are typeable all the same, so a filter that
    /// looked only at the keys would throw away one Arabic word in eight.
    /// </summary>
    [Fact]
    public void ArabicKeepsTheWordsWhoseMarksAreDecoratorsRatherThanKeys()
    {
        // كَتَبَ — three letters carrying a fatha each; and كِتاب, whose second character is a kasra.
        var source = new Words(("كَتَبَ", 10), ("كِتاب", 20));

        var kept = new TypeableSeedWordSource(source, Language.Arabic).GetWords().ToList();

        Assert.Equal(2, kept.Count);
    }

    [Theory]
    [InlineData("HELLO")]
    [InlineData("DON'T")]
    [InlineData("42")]
    public void KeepsWhatTheKeyboardCanSpell(string word)
    {
        var kept = new TypeableSeedWordSource(new Words((word, 1), ("OK", 2)), Language.English).GetWords().ToList();

        Assert.Contains((word, 1), kept);
    }

    [Theory]
    [InlineData("HÉLLO")]     // an accent English has no key for
    [InlineData("HELLO!")]    // punctuation the corpus carries and the keyboard does not
    [InlineData("“HI”")] // curly quotes
    [InlineData("HELLO WORLD")]    // Space types nothing, so a phrase is not a word
    public void DropsWhatTheKeyboardCannotSpell(string word)
    {
        var kept = new TypeableSeedWordSource(new Words((word, 1), ("OK", 2)), Language.English).GetWords().ToList();

        Assert.Equal([("OK", 2)], kept);
    }

    [Fact]
    public void ArabicDropsTheApostropheItHasNoKeyFor()
    {
        var source = new Words(("كتاب", 5), ("كتاب'", 5));

        var kept = new TypeableSeedWordSource(source, Language.Arabic).GetWords().ToList();

        Assert.Equal([("كتاب", 5)], kept);
    }

    /// <summary>
    /// A word list written in decomposed form spells É as E followed by a combining acute, which no
    /// keyboard here has a key for. Composing it rescues the word rather than losing every accented
    /// entry in the list.
    /// </summary>
    [Fact]
    public void ComposesAWordThatOnlyFailsBecauseItIsDecomposed()
    {
        var kept = new TypeableSeedWordSource(new Words(("CAFÉ", 7)), Language.French).GetWords().ToList();

        Assert.Equal([("CAFÉ", 7)], kept);
    }

    /// <summary>
    /// The other half of that: a word that already types is yielded exactly as it was stored. Composing
    /// unconditionally would reorder the marks of some scripts, quietly changing spellings on their way
    /// into the dictionary.
    /// </summary>
    [Fact]
    public void DoesNotRewriteAWordThatAlreadyTypes()
    {
        // Shadda then fatha: the order composition would swap, and both are Arabic decorators.
        const string AsStored = "كَّتب";
        var kept = new TypeableSeedWordSource(new Words((AsStored, 3)), Language.Arabic).GetWords().ToList();

        Assert.Equal([(AsStored, 3)], kept);
        Assert.Same(AsStored, kept[0].Word);
    }

    [Fact]
    public void PassesTheUsageCountThroughUnchanged()
    {
        var kept = new TypeableSeedWordSource(new Words(("HELLO", 113217)), Language.English).GetWords().ToList();

        Assert.Equal(113217, kept[0].LanguageUsageCount);
    }

    /// <summary>
    /// A word list of which nothing survives is the wrong list, or a right one built wrongly — left in
    /// lower case, say, against a keyboard that is upper case throughout. Seeding it would leave a
    /// dictionary that predicts nothing and says why not, so it fails loudly instead.
    /// </summary>
    [Fact]
    public void ThrowsWhenNothingInTheListCanBeTyped()
    {
        var source = new Words(("hello", 1), ("world", 2));

        var error = Assert.Throws<InvalidOperationException>(
            () => new TypeableSeedWordSource(source, Language.English).GetWords().ToList());

        Assert.Contains("English", error.Message);
    }

    /// <summary>
    /// An empty source is not a broken one: it is the app opening a database without a pack to seed it
    /// from, which every launch after the first one does.
    /// </summary>
    [Fact]
    public void AcceptsASourceThatOffersNothing()
    {
        Assert.Empty(new TypeableSeedWordSource(new Words(), Language.English).GetWords());
    }

    private sealed class Words : ISeedWordSource
    {
        private readonly (string Word, int LanguageUsageCount)[] Items;

        public Words(params (string Word, int LanguageUsageCount)[] items) => Items = items;

        public IEnumerable<(string Word, int LanguageUsageCount)> GetWords() => Items;
    }

    private sealed class PackSeed : ISeedWordSource
    {
        private readonly Language Language;

        public PackSeed(Language language) => Language = language;

        public IEnumerable<(string Word, int LanguageUsageCount)> GetWords()
        {
            using var zip = File.OpenRead(Path.Combine(AppContext.BaseDirectory, $"{Language}.zip"));
            foreach (var word in WordListZipReader.Read(zip))
                yield return word;
        }
    }
}
