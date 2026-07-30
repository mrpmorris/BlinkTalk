using BlinkTalk.Application.Text;

namespace BlinkTalk.Application.Tests;

/// <summary>
/// The safety net over the layout data in <c>Text/Layouts/</c>. Those files are transcribed by hand,
/// so the failure to catch is a dropped, duplicated or misplaced character — nothing about the app
/// would crash, the person would just find a letter missing from their keyboard.
/// </summary>
public class KeyboardLayoutTests
{
    public static TheoryData<Language, KeyboardLayoutStyle> EveryLayout()
    {
        var data = new TheoryData<Language, KeyboardLayoutStyle>();
        foreach (Language language in Enum.GetValues<Language>())
        {
            foreach (KeyboardLayoutStyle style in Enum.GetValues<KeyboardLayoutStyle>())
                data.Add(language, style);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(EveryLayout))]
    public void EveryLayoutHasFourLetterRowsAndARowOfDigits(Language language, KeyboardLayoutStyle style)
    {
        var layout = KeyboardLayout.Create(language, style);

        Assert.Equal(5, layout.Rows.Count);
        Assert.All(layout.Rows, row => Assert.NotEmpty(row));
    }

    [Theory]
    [MemberData(nameof(EveryLayout))]
    public void SpaceLeadsTheThirdRowAndBackspaceTheFourthAndNeitherAppearsElsewhere(
        Language language, KeyboardLayoutStyle style)
    {
        var layout = KeyboardLayout.Create(language, style);

        Assert.Equal(KeyboardKey.Space, layout.Rows[2][0]);
        Assert.Equal(KeyboardKey.Backspace, layout.Rows[3][0]);
        // One of each, on their own row alone: a second Space would cost a dwell on every sweep.
        IEnumerable<KeyboardKey> keys = layout.Rows.SelectMany(row => row);
        Assert.Single(keys, key => key.Kind == KeyboardKeyKind.Space);
        Assert.Single(keys, key => key.Kind == KeyboardKeyKind.Backspace);
    }

    [Theory]
    [MemberData(nameof(EveryLayout))]
    public void TheDecoratorKeyIsFirstOnTheFirstRowExactlyWhenTheLanguageHasDecorators(
        Language language, KeyboardLayoutStyle style)
    {
        var layout = KeyboardLayout.Create(language, style);

        IEnumerable<KeyboardKey> decoratorKeys = layout.Rows
            .SelectMany(row => row)
            .Where(key => key.Kind == KeyboardKeyKind.Decorators);

        if (layout.Decorators.Count == 0)
        {
            Assert.Empty(decoratorKeys);
            return;
        }

        // First in the row is first in the scan, so it costs a single dwell to reach.
        Assert.Equal(KeyboardKey.Decorators, layout.Rows[0][0]);
        Assert.Single(decoratorKeys);
    }

    [Theory]
    [MemberData(nameof(EveryLayout))]
    public void EveryKeyThatIsNotAnActionTypesSomething(Language language, KeyboardLayoutStyle style)
    {
        var layout = KeyboardLayout.Create(language, style);

        foreach (KeyboardKey key in layout.Rows.SelectMany(row => row))
        {
            if (key.Kind != KeyboardKeyKind.Character)
                continue;
            Assert.False(string.IsNullOrEmpty(key.Text));
            Assert.Equal(key.Text, key.Label);
        }
    }

    [Theory]
    [InlineData(Language.English)]
    [InlineData(Language.Portuguese)]
    [InlineData(Language.Arabic)]
    public void BothArrangementsOfALanguageOfferTheSameCharacters(Language language)
    {
        string[] alphabetical = CharactersOf(language, KeyboardLayoutStyle.Alphabetical);
        string[] speed = CharactersOf(language, KeyboardLayoutStyle.Speed);

        // Sorted rather than set-compared, so a duplicate fails too — the same length and the same
        // distinct members can still hide one letter typed twice and another missing.
        Assert.Equal(alphabetical.OrderBy(text => text, StringComparer.Ordinal).ToArray(),
            speed.OrderBy(text => text, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void EnglishAlphabeticalRunsAToZThenTheApostrophe()
    {
        var layout = KeyboardLayout.Create(Language.English, KeyboardLayoutStyle.Alphabetical);

        Assert.Equal(new[] { "A", "B", "C", "D", "E", "F", "G" }, TextOf(layout.Rows[0]));
        // Letters only: where Space and Backspace sit is the business of the theory above, and pinning
        // their labels here would make this class fail on the day one of them becomes a different glyph.
        Assert.Equal(new[] { "V", "W", "X", "Y", "Z", "'" }, TextOf(layout.Rows[3]));
        Assert.Equal(new[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" }, TextOf(layout.Rows[4]));
    }

    [Fact]
    public void EnglishSpeedLeadsWithTheMostUsedLetters()
    {
        var layout = KeyboardLayout.Create(Language.English, KeyboardLayoutStyle.Speed);

        Assert.Equal(new[] { "E", "T", "O", "L", "F", "B", "Q" }, TextOf(layout.Rows[0]));
    }

    [Fact]
    public void EnglishHasNoDecoratorsAtAll()
    {
        var layout = KeyboardLayout.Create(Language.English, KeyboardLayoutStyle.Alphabetical);

        Assert.Empty(layout.Decorators);
    }

    [Fact]
    public void ArabicReadsRightToLeftAndUsesItsOwnDigits()
    {
        var layout = KeyboardLayout.Create(Language.Arabic, KeyboardLayoutStyle.Alphabetical);

        Assert.True(layout.IsRightToLeft);
        Assert.Equal(new[] { "٠", "١", "٢", "٣", "٤", "٥", "٦", "٧", "٨", "٩" },
            TextOf(layout.Rows[4]));
    }

    [Fact]
    public void ArabicOffersTheHarakatAsDecoratorsWithTheFathatanFirst()
    {
        var layout = KeyboardLayout.Create(Language.Arabic, KeyboardLayoutStyle.Alphabetical);

        Assert.Equal(9, layout.Decorators.Count);
        Assert.Equal("\u064B", layout.Decorators[0]); // ARABIC FATHATAN
        // Each is a single combining mark: two would backspace as two, and the popup shows each on
        // one dotted circle.
        Assert.All(layout.Decorators, mark => Assert.Single(mark));
    }

    [Fact]
    public void PortugueseSpellsItsAccentedLettersAsKeysRatherThanDecorators()
    {
        var layout = KeyboardLayout.Create(Language.Portuguese, KeyboardLayoutStyle.Alphabetical);

        Assert.Empty(layout.Decorators);
        Assert.Contains("Ã", TextOf(layout.Rows[0]));
        Assert.Contains("Ç", TextOf(layout.Rows[0]));
    }

    [Fact]
    public void TheDefaultLayoutIsEnglishAlphabetical()
    {
        Assert.Equal(
            TextOf(KeyboardLayout.Create(Language.English, KeyboardLayoutStyle.Alphabetical).Rows[0]),
            TextOf(KeyboardLayout.CreateDefault().Rows[0]));
    }

    private static string[] CharactersOf(Language language, KeyboardLayoutStyle style) =>
        KeyboardLayout.Create(language, style).Rows
            .SelectMany(row => row)
            .Where(key => key.Kind == KeyboardKeyKind.Character)
            .Select(key => key.Text!)
            .ToArray();

    private static string[] TextOf(IReadOnlyList<KeyboardKey> row) =>
        row.Where(key => key.Kind == KeyboardKeyKind.Character).Select(key => key.Text!).ToArray();
}
