using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BlinkTalk.Application.Persistence;
using BlinkTalk.Application.Prediction;

namespace BlinkTalk.Application.Tests;

public class PredictionTests
{
    [Fact]
    public void IncrementPhraseUsageInsertsThenIncrementsWindows()
    {
        using var db = NewInMemoryDb();
        db.ExecuteNonQuery(
            "INSERT INTO Words(ID,Word,SearchWord,UserSelectionCount,LanguageUsageCount) VALUES (1,'i','I',0,0),(2,'am','AM',0,0)");
        var phrase = new PhraseService(db, new FixedClock());

        phrase.IncrementPhraseUsage(new[] { 1, 2 });
        phrase.IncrementPhraseUsage(new[] { 1, 2 });

        // Window (null,null,1 -> 2) should now have UsageCount 2.
        var rows = db.ExecuteQuery(
            "select UsageCount from WordSequences where PrecedingWord1Id = 1 and SuggestedWordId = 2").Rows;
        Assert.Single(rows);
        Assert.Equal(2, Convert.ToInt32(rows[0]["UsageCount"]));
    }

    [Fact]
    public void PhrasePrefixFiltersSuggestions()
    {
        using var db = NewInMemoryDb();
        db.ExecuteNonQuery(
            "INSERT INTO Words(ID,Word,SearchWord,UserSelectionCount,LanguageUsageCount) VALUES " +
            "(1,'hello','HELLO',0,0),(2,'apple','APPLE',0,0),(3,'ant','ANT',0,0)");
        db.ExecuteNonQuery(
            "INSERT INTO WordSequences(PrecedingWord3Id,PrecedingWord2Id,PrecedingWord1Id,SuggestedWordId,UsageCount,LastUsedDate) VALUES " +
            "(-1,-1,1,2,5,20260101)," +   // apple
            "(-1,-1,1,3,5,20260101)");    // ant

        var phrase = new PhraseService(db, new FixedClock());
        var result = phrase.GetWordSuggestions(new[] { 1 }, "ap", 6);

        Assert.Equal(new[] { "apple" }, result);
    }

    [Fact]
    public void PhraseSuggestionsRankContextMatchAboveUsageThenUsageWithinSameScore()
    {
        using var db = NewInMemoryDb();
        db.ExecuteNonQuery(
            "INSERT INTO Words(ID,Word,SearchWord,UserSelectionCount,LanguageUsageCount) VALUES " +
            "(1,'hello','HELLO',0,0),(2,'xword','XWORD',0,0),(3,'yword','YWORD',0,0),(4,'zword','ZWORD',0,0)");
        // Two strong context matches (preceding word 1 == hello), differing only by usage count.
        db.ExecuteNonQuery(
            "INSERT INTO WordSequences(PrecedingWord3Id,PrecedingWord2Id,PrecedingWord1Id,SuggestedWordId,UsageCount,LastUsedDate) VALUES " +
            "(-1,-1,1,2,1,20260101)," +   // xword, usage 1
            "(-1,-1,1,3,9,20260101)," +   // yword, usage 9
            "(-1,-1,-1,4,100,20260101)"); // zword, no context match, usage 100

        var phrase = new PhraseService(db, new FixedClock());
        var result = phrase.GetWordSuggestions(new[] { 1 }, "", 6);

        // yword and xword share the higher score (context match); yword wins on usage.
        // zword scores lower despite far higher usage, so it ranks last.
        Assert.Equal(new[] { "yword", "xword", "zword" }, result);
    }

    [Fact]
    public void RealDatabaseSchemaSupportsDictionaryAndPhraseQueries()
    {
        string temp = Path.Combine(Path.GetTempPath(), "blinktalk_test_" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            using var db = new MicrosoftDataSqliteDatabase(temp);
            new AutoMigratingDatabase(db, new FixedClock(), new ZipSeedWordSource()).Migrate();
            var words = new WordService(db);

            // Dictionary prefix lookup returns real suggestions (schema/columns match the ported SQL).
            // The word list stores words in their natural casing; the prefix is matched against the
            // folded SearchWord seeded alongside each one, so the match ignores case (and accents).
            var dictionary = words.GetWordSuggestions("th", 6);
            Assert.NotEmpty(dictionary);
            Assert.All(dictionary, w => Assert.StartsWith("TH", w, StringComparison.OrdinalIgnoreCase));

            // Apostrophes used to break the original (interpolated SQL); now bound as a parameter.
            var apostrophe = words.GetWordSuggestions("don'", 6);
            Assert.NotNull(apostrophe);

            // The phrase query runs end-to-end against the real schema.
            var phrase = new PhraseService(db, new FixedClock());
            var suggestions = phrase.GetWordSuggestions(Array.Empty<int>(), "th", 6);
            Assert.NotNull(suggestions);
        }
        finally
        {
            File.Delete(temp);
        }
    }

    [Fact]
    public void WordServiceCreatesAndIncrementsWords()
    {
        using var db = NewInMemoryDb();
        var words = new WordService(db);

        words.IncreaseWordUsage("Hello", out int id1);
        words.IncreaseWordUsage("hello", out int id2); // same word, case-insensitive

        Assert.Equal(id1, id2);
        var row = db.ExecuteQuery("select UserSelectionCount from Words where ID = @id", ("@id", id1)).Rows;
        Assert.Equal(2, Convert.ToInt32(row[0]["UserSelectionCount"]));
    }

    [Theory]
    [InlineData("CAFE")]  // typed without the accent, which is all the keyboard used to allow
    [InlineData("CAFÉ")]  // typed with it
    [InlineData("cafe")]
    public void AccentedDictionaryWordsAreFoundHoweverTheyAreTyped(string typed)
    {
        using var db = NewInMemoryDb(new FakeSeedWordSource(("café", 500)));
        var words = new WordService(db);

        Assert.Equal(new[] { "café" }, words.GetWordSuggestions(typed, 6));
    }

    [Fact]
    public void SelectingAWordCreditsTheSeededRowRatherThanCreatingANearDuplicate()
    {
        using var db = NewInMemoryDb(new FakeSeedWordSource(("café", 500)));
        var words = new WordService(db);

        words.IncreaseWordUsage("CAFE", out int wordId);

        // One row, still spelled as the word list spells it, now carrying the selection. Before
        // SearchWord existed this inserted a second row "CAFE" and the counts diverged.
        Assert.Equal(1, Convert.ToInt32(db.ExecuteScalar("select count(1) from Words")));
        var row = db.ExecuteQuery("select Word, UserSelectionCount from Words where ID = @id", ("@id", wordId)).Rows;
        Assert.Equal("café", (string)row[0]["Word"]!);
        Assert.Equal(1, Convert.ToInt32(row[0]["UserSelectionCount"]));
    }

    [Fact]
    public void SelectingAnAccentedWordPrefersTheRowWithTheSameAccents()
    {
        // "cafe" and "café" both fold to CAFE, so both are candidates; the accents decide.
        using var db = NewInMemoryDb(new FakeSeedWordSource(("cafe", 900), ("café", 500)));
        var words = new WordService(db);

        words.IncreaseWordUsage("CAFÉ", out int wordId);

        Assert.Equal("café", (string)db.ExecuteQuery(
            "select Word from Words where ID = @id", ("@id", wordId)).Rows[0]["Word"]!);
    }

    [Fact]
    public void UnknownWordsAreStillLearnedWithTheirSearchKey()
    {
        using var db = NewInMemoryDb();
        var words = new WordService(db);

        words.IncreaseWordUsage("CRÈCHE", out int wordId);

        var row = db.ExecuteQuery("select Word, SearchWord from Words where ID = @id", ("@id", wordId)).Rows;
        Assert.Equal("CRÈCHE", (string)row[0]["Word"]!);
        Assert.Equal("CRECHE", (string)row[0]["SearchWord"]!);
        Assert.Contains("CRÈCHE", words.GetWordSuggestions("CRE", 6));
    }

    [Fact]
    public void PhrasePrefixIgnoresAccentsToo()
    {
        using var db = NewInMemoryDb();
        db.ExecuteNonQuery(
            "INSERT INTO Words(ID,Word,SearchWord,UserSelectionCount,LanguageUsageCount) VALUES " +
            "(1,'un','UN',0,0),(2,'été','ETE',0,0),(3,'enfant','ENFANT',0,0)");
        db.ExecuteNonQuery(
            "INSERT INTO WordSequences(PrecedingWord3Id,PrecedingWord2Id,PrecedingWord1Id,SuggestedWordId,UsageCount,LastUsedDate) VALUES " +
            "(-1,-1,1,2,5,20260101)," +
            "(-1,-1,1,3,5,20260101)");

        var phrase = new PhraseService(db, new FixedClock());

        Assert.Equal(new[] { "été" }, phrase.GetWordSuggestions(new[] { 1 }, "ET", 6));
    }

    // --- Parity against the bundled word list (English.zip, linked into the test output) ---

    private sealed class ZipSeedWordSource : ISeedWordSource
    {
        public IEnumerable<(string Word, int LanguageUsageCount)> GetWords()
        {
            using var zip = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "English.zip"));
            foreach (var word in WordListZipReader.Read(zip))
                yield return word;
        }
    }

    private static MicrosoftDataSqliteDatabase NewInMemoryDb(ISeedWordSource? seedWordSource = null)
    {
        var db = new MicrosoftDataSqliteDatabase(":memory:");
        new AutoMigratingDatabase(db, new FixedClock(), seedWordSource).Migrate();
        return db;
    }
}
