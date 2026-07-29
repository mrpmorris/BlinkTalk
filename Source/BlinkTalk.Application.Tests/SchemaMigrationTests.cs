using System;
using System.Collections.Generic;
using BlinkTalk.Application.Persistence;

namespace BlinkTalk.Application.Tests;

public class SchemaMigrationTests
{
    private const int CurrentSchemaVersion = 2;

    [Fact]
    public void MigratingAnAlreadyCreatedDatabaseIsANoOp()
    {
        using var db = new MicrosoftDataSqliteDatabase(":memory:");
        new AutoMigratingDatabase(db, new FixedClock()).Migrate();
        db.ExecuteNonQuery("INSERT INTO Words(Word, LanguageUsageCount, UserSelectionCount) VALUES ('KEPT', 1, 1)");

        // Re-running must not re-apply the schema script: the CREATEs would fail on the existing
        // tables, and a second DbInfo row would make the version read ambiguous.
        new AutoMigratingDatabase(db, new FixedClock()).Migrate();

        Assert.Equal(1, Convert.ToInt32(db.ExecuteScalar("select count(1) from DbInfo")));
        Assert.Equal(CurrentSchemaVersion, Convert.ToInt32(db.ExecuteScalar("select Version from DbInfo")));
        Assert.Equal(1, Convert.ToInt32(db.ExecuteScalar("select count(1) from Words where Word = 'KEPT'")));
    }

    [Fact]
    public void FailedSeedRollsBackWithoutLeavingPartialWords()
    {
        using var db = new MicrosoftDataSqliteDatabase(":memory:");
        var sut = new AutoMigratingDatabase(db, new FixedClock(), new ThrowingSeedWordSource());

        Assert.Throws<InvalidOperationException>(() => sut.Migrate());

        // The schema transaction committed, but the seed's did not — an aborted first run must
        // leave the dictionary empty so the next launch seeds it properly rather than skipping.
        Assert.Equal(0, Convert.ToInt32(db.ExecuteScalar("select count(1) from Words")));
        Assert.Equal(CurrentSchemaVersion, Convert.ToInt32(db.ExecuteScalar("select Version from DbInfo")));
    }

    // --- V1 -> V2: the SearchWord column, and merging rows that differ only in case ---

    [Fact]
    public void UpgradingFromV1BackfillsTheFoldedSearchWord()
    {
        using var db = NewV1Database(("café", 500, 0), ("Straße", 400, 0));

        new AutoMigratingDatabase(db, new FixedClock()).Migrate();

        Assert.Equal(CurrentSchemaVersion, Convert.ToInt32(db.ExecuteScalar("select Version from DbInfo")));
        Assert.Equal("CAFE", SearchWordOf(db, "café"));
        Assert.Equal("STRASSE", SearchWordOf(db, "Straße"));
    }

    [Fact]
    public void UpgradingFromV1MergesRowsThatDifferOnlyInCase()
    {
        // What the old typing path produced: a seeded "the" carrying the language frequency, plus
        // a "THE" the person's own selections accumulated on.
        using var db = NewV1Database(("the", 900, 2), ("THE", 0, 5));

        new AutoMigratingDatabase(db, new FixedClock()).Migrate();

        var rows = db.ExecuteQuery("select Word, LanguageUsageCount, UserSelectionCount from Words").Rows;
        Assert.Single(rows);
        Assert.Equal("the", (string)rows[0]["Word"]!);
        Assert.Equal(900, Convert.ToInt32(rows[0]["LanguageUsageCount"]));
        Assert.Equal(7, Convert.ToInt32(rows[0]["UserSelectionCount"]));
    }

    [Fact]
    public void UpgradingFromV1KeepsWordsThatDifferInTheirAccentsApart()
    {
        using var db = NewV1Database(("ou", 900, 0), ("où", 800, 0));

        new AutoMigratingDatabase(db, new FixedClock()).Migrate();

        Assert.Equal(2, Convert.ToInt32(db.ExecuteScalar("select count(1) from Words")));
    }

    [Fact]
    public void MergingRepointsLearnedSequencesAndFoldsDuplicateWindowsTogether()
    {
        using var db = NewV1Database(("the", 900, 0), ("THE", 0, 0), ("end", 100, 0));
        int the = WordId(db, "the");
        int upperThe = WordId(db, "THE");
        int end = WordId(db, "end");
        // The same window learned twice, once against each casing of "the".
        db.ExecuteNonQuery(
            "insert into WordSequences(PrecedingWord3ID,PrecedingWord2ID,PrecedingWord1ID,SuggestedWordID,UsageCount,LastUsedDate) values " +
            $"(-1,-1,{the},{end},3,20260601)," +
            $"(-1,-1,{upperThe},{end},4,20260602)");

        new AutoMigratingDatabase(db, new FixedClock()).Migrate();

        var rows = db.ExecuteQuery("select PrecedingWord1ID, UsageCount, LastUsedDate from WordSequences").Rows;
        Assert.Single(rows);
        Assert.Equal(the, Convert.ToInt32(rows[0]["PrecedingWord1ID"]));
        Assert.Equal(7, Convert.ToInt32(rows[0]["UsageCount"]));
        Assert.Equal(20260602, Convert.ToInt32(rows[0]["LastUsedDate"]));
    }

    /// <summary>
    /// A database as V1 left it: the V1 schema exactly, with no SearchWord column. Written by hand
    /// rather than by calling the migrator so the upgrade path is genuinely exercised.
    /// </summary>
    private static MicrosoftDataSqliteDatabase NewV1Database(
        params (string Word, int LanguageUsageCount, int UserSelectionCount)[] words)
    {
        var db = new MicrosoftDataSqliteDatabase(":memory:");
        db.ExecuteNonQuery(@"
            CREATE TABLE Words (
                ID INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                Word TEXT NOT NULL UNIQUE,
                LanguageUsageCount INTEGER NOT NULL,
                UserSelectionCount INTEGER NOT NULL
            );
            CREATE TABLE WordSequences (
                ID INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                PrecedingWord3ID INTEGER NOT NULL,
                PrecedingWord2ID INTEGER NOT NULL,
                PrecedingWord1ID INTEGER NOT NULL,
                SuggestedWordID INTEGER NOT NULL,
                UsageCount INTEGER NOT NULL DEFAULT 0,
                LastUsedDate INTEGER NOT NULL
            );
            CREATE TABLE DbInfo (
                Version INTEGER NOT NULL
            );
            INSERT INTO DbInfo (Version) VALUES (1);
        ");
        foreach (var word in words)
        {
            db.ExecuteNonQuery(
                "insert into Words (Word, LanguageUsageCount, UserSelectionCount) values (@word, @language, @user)",
                ("@word", word.Word),
                ("@language", word.LanguageUsageCount),
                ("@user", word.UserSelectionCount));
        }
        return db;
    }

    private static string SearchWordOf(ISqliteDatabase db, string word) =>
        Convert.ToString(db.ExecuteScalar("select SearchWord from Words where Word = @word", ("@word", word)))!;

    private static int WordId(ISqliteDatabase db, string word) =>
        Convert.ToInt32(db.ExecuteScalar("select ID from Words where Word = @word", ("@word", word)));

    /// <summary>Yields a couple of words, then fails part-way — a truncated or corrupt word list.</summary>
    private sealed class ThrowingSeedWordSource : ISeedWordSource
    {
        public IEnumerable<(string Word, int LanguageUsageCount)> GetWords()
        {
            yield return ("ALPHA", 1);
            yield return ("BETA", 2);
            throw new InvalidOperationException("corrupt word list");
        }
    }
}
