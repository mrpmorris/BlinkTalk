using BlinkTalk.Application.Text;

namespace BlinkTalk.Application.Tests;

/// <summary>
/// The folding rules behind accent-insensitive prediction. These are behavioural: the search
/// keys stored in the database are produced by <see cref="TextFold.Fold"/>, so changing what it
/// returns changes which words the person can find.
/// </summary>
public class TextFoldTests
{
    [Theory]
    [InlineData("café", "CAFE")]
    [InlineData("CAFÉ", "CAFE")]
    [InlineData("été", "ETE")]
    [InlineData("naïve", "NAIVE")]
    [InlineData("mañana", "MANANA")]
    [InlineData("où", "OU")]
    [InlineData("também", "TAMBEM")]
    [InlineData("größe", "GROSSE")]
    [InlineData("GRÖSSE", "GROSSE")]
    [InlineData("œuvre", "OEUVRE")]
    public void FoldRemovesCaseAndAccents(string input, string expected) =>
        Assert.Equal(expected, TextFold.Fold(input));

    [Fact]
    public void FoldCaseKeepsAccentsSoDistinctWordsStayDistinct()
    {
        // The migration merges dictionary rows that fold to the same case key, so "ou" and "où"
        // must not share one — they are different French words.
        Assert.Equal("OU", TextFold.FoldCase("ou"));
        Assert.Equal("OÙ", TextFold.FoldCase("où"));
        Assert.NotEqual(TextFold.FoldCase("ou"), TextFold.FoldCase("où"));
    }

    [Fact]
    public void FoldCaseSpellsOutEszettSoBothGermanSpellingsMatch()
    {
        Assert.Equal("STRASSE", TextFold.FoldCase("straße"));
        Assert.Equal("STRASSE", TextFold.FoldCase("STRASSE"));
        Assert.Equal("STRASSE", TextFold.FoldCase("STRAẞE"));
    }

    [Theory]
    [InlineData("كَتَبَ", "كتب")]     // harakat stripped
    [InlineData("مُدَرِّسٌ", "مدرس")]   // shadda and tanween too
    [InlineData("كــتــب", "كتب")]   // tatweel is decoration only
    [InlineData("أحمد", "احمد")]     // alef with hamza above
    [InlineData("إسلام", "اسلام")]   // alef with hamza below
    [InlineData("آخر", "اخر")]       // alef with madda
    [InlineData("مسؤول", "مسوول")]   // waw with hamza
    [InlineData("رئيس", "رييس")]     // yeh with hamza
    [InlineData("مدرسة", "مدرسه")]   // teh marbuta unifies with heh
    [InlineData("على", "علي")]       // alef maksura unifies with yeh
    [InlineData("ماء", "ماء")]       // standalone hamza is a letter, not a mark
    public void FoldUnifiesArabicVariants(string input, string expected) =>
        Assert.Equal(expected, TextFold.Fold(input));

    [Theory]
    [InlineData("café")]
    [InlineData("STRAẞE")]
    [InlineData("مُدَرِّسٌ")]
    [InlineData("Œuvre")]
    public void FoldIsIdempotent(string input) =>
        Assert.Equal(TextFold.Fold(input), TextFold.Fold(TextFold.Fold(input)));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void FoldingNothingGivesEmptyRatherThanThrowing(string? input)
    {
        Assert.Equal("", TextFold.Fold(input));
        Assert.Equal("", TextFold.FoldCase(input));
    }
}
