using System;
using BlinkTalk.Application.Abstractions;

namespace BlinkTalk.Application.Persistence;

/// <summary>
/// Wraps an ISqliteDatabase and performs the same startup maintenance the original
/// AutoMigratingDatabase did: prune learned word sequences older than 30 days. Migration
/// hooks can be added here later (the original left a commented-out version-check example).
/// </summary>
public sealed class AutoMigratingDatabase
{
    public ISqliteDatabase Database { get; }

    private readonly IClock Clock;

    public AutoMigratingDatabase(ISqliteDatabase database, IClock clock)
    {
        Database = database;
        Clock = clock;
    }

    public void Migrate()
    {
        PerformDbMaintenance();
    }

    private void PerformDbMaintenance()
    {
        int cutoff = DateInt.FromDate(Clock.UtcNow.Date.AddDays(-30));
        Database.ExecuteNonQuery(
            "delete from WordSequences where LastUsedDate <= @cutoff",
            ("@cutoff", cutoff));
    }
}
