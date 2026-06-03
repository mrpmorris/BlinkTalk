using System;
using BlinkTalk.Application.Abstractions;

namespace BlinkTalk.Application.Persistence
{
    /// <summary>
    /// Wraps an ISqliteDatabase and performs the same startup maintenance the original
    /// AutoMigratingDatabase did: prune learned word sequences older than 30 days. Migration
    /// hooks can be added here later (the original left a commented-out version-check example).
    /// </summary>
    public sealed class AutoMigratingDatabase
    {
        private readonly ISqliteDatabase _database;
        private readonly IClock _clock;

        public AutoMigratingDatabase(ISqliteDatabase database, IClock clock)
        {
            _database = database;
            _clock = clock;
        }

        public ISqliteDatabase Database => _database;

        public void Migrate()
        {
            PerformDbMaintenance();
        }

        private void PerformDbMaintenance()
        {
            int cutoff = DateInt.FromDate(_clock.UtcNow.Date.AddDays(-30));
            _database.ExecuteNonQuery(
                "delete from WordSequences where LastUsedDate <= @cutoff",
                ("@cutoff", cutoff));
        }
    }
}
