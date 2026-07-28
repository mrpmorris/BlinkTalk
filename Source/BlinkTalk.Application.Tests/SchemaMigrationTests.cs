using System;
using System.Collections.Generic;
using BlinkTalk.Application.Persistence;

namespace BlinkTalk.Application.Tests;

public class SchemaMigrationTests
{
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
        Assert.Equal(1, Convert.ToInt32(db.ExecuteScalar("select Version from DbInfo")));
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
        Assert.Equal(1, Convert.ToInt32(db.ExecuteScalar("select Version from DbInfo")));
    }

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
