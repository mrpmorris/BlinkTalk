using System;
using System.Collections.Generic;
using System.Text;
using BlinkTalk.Application.Abstractions;

namespace BlinkTalk.Application.Persistence;

/// <summary>
/// Owns the database schema. On startup it brings the database up to the current schema version
/// (creating everything from SQL when DbInfo says version 0, i.e. a brand-new file), seeds the
/// Words dictionary from the bundled word list the first time, and performs the same maintenance
/// the original AutoMigratingDatabase did: prune learned word sequences older than 30 days.
/// Schema creation and seeding each run in their own transaction, so an interrupted first launch
/// leaves nothing half-built.
/// </summary>
public sealed class AutoMigratingDatabase
{
	/// <summary>Rows per INSERT batch while seeding; 2 parameters each, well under SQLite's 999-parameter limit.</summary>
	private const int SeedBatchSize = 400;

	public ISqliteDatabase Database { get; }

	private readonly IClock Clock;
	private readonly ISeedWordSource? SeedWordSource;

	public AutoMigratingDatabase(ISqliteDatabase database, IClock clock, ISeedWordSource? seedWordSource = null)
	{
		Database = database;
		Clock = clock;
		SeedWordSource = seedWordSource;
	}

	public void Migrate()
	{
		UpdateSchema();
		SeedWordsIfEmpty();
		PerformDbMaintenance();
	}

	/// <summary>
	/// The schema the prediction SQL depends on. Matches the previously shipped English.db:
	/// null preceding-word ids are stored as the sentinel -1 (NOT NULL), not SQL NULL.
	/// The encoding pragma must come before the first table is created — SQLite fixes a
	/// database's text encoding at creation and silently ignores the pragma afterwards. UTF-8 is
	/// already the default; stating it keeps the accented word lists explicit rather than implied.
	/// </summary>
	private void UpdateSchema()
	{
		int currentVersion = GetSchemaVersion();

		if (currentVersion < 1)
			UpdateSchemaToV1();
	}

	private void UpdateSchemaToV1()
	{
		// Outside the transaction: PRAGMA encoding is not transactional, and it must be set
		// while the database is still empty for SQLite to honour it.
		Database.ExecuteNonQuery("PRAGMA encoding = 'UTF-8'");

		// SQLite DDL is transactional, so a failure part-way through leaves no half-built
		// schema behind — the next launch retries from scratch rather than finding some
		// tables present and skipping the rest.
		InTransaction(() => Database.ExecuteNonQuery(@"
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
				CREATE INDEX IX_Words_Word ON Words (Word);
				CREATE INDEX IX_Words_LanguageUsageCount ON Words (LanguageUsageCount DESC);
				CREATE INDEX IX_Words_UserSelectionCount ON Words (UserSelectionCount DESC);
				CREATE INDEX IX_WordSequences_PrecedingWord3ID ON WordSequences (PrecedingWord3ID);
				CREATE INDEX IX_WordSequences_PrecedingWord2ID ON WordSequences (PrecedingWord2ID);
				CREATE INDEX IX_WordSequences_PrecedingWord1ID ON WordSequences (PrecedingWord1ID);
				CREATE INDEX IX_WordSequences_SuggestedWordID ON WordSequences (SuggestedWordID);

				INSERT INTO DbInfo (`Version`) VALUES (1);
			"));
	}

	/// <summary>
	/// The schema version, or 0 when the database is brand new. DbInfo itself may not exist yet,
	/// and querying a missing table is an error rather than an empty result, so probe sqlite_master
	/// first.
	/// </summary>
	private int GetSchemaVersion()
	{
		object? schemaTable = Database.ExecuteScalar(
			"select name from sqlite_master where type = 'table' and name = 'DbInfo'");
		if (schemaTable == null)
			return 0;

		// Convert rather than cast: SQLite hands back INTEGER as long, so unboxing straight to
		// int (or int?) throws InvalidCastException.
		object? version = Database.ExecuteScalar("select Version from DbInfo");
		return version == null ? 0 : Convert.ToInt32(version);
	}

	/// <summary>
	/// Runs <paramref name="work"/> inside a transaction, rolling back if it throws. The rollback
	/// is itself guarded so a failure there cannot mask the original exception.
	/// </summary>
	private void InTransaction(Action work)
	{
		Database.ExecuteNonQuery("BEGIN TRANSACTION");
		try
		{
			work();
			Database.ExecuteNonQuery("COMMIT");
		}
		catch
		{
			try
			{
				Database.ExecuteNonQuery("ROLLBACK");
			}
			catch
			{
				// Ignored: the original exception below is the one worth reporting.
			}
			throw;
		}
	}

	private void SeedWordsIfEmpty()
	{
		if (SeedWordSource == null)
			return;
		if (Convert.ToInt32(Database.ExecuteScalar("select count(1) from Words")) > 0)
			return;

		InTransaction(() =>
		{
			var batch = new List<(string Word, int LanguageUsageCount)>(SeedBatchSize);
			foreach (var word in SeedWordSource.GetWords())
			{
				batch.Add(word);
				if (batch.Count == SeedBatchSize)
				{
					InsertWords(batch);
					batch.Clear();
				}
			}
			if (batch.Count > 0)
				InsertWords(batch);
		});
	}

	private void InsertWords(List<(string Word, int LanguageUsageCount)> batch)
	{
		var sql = new StringBuilder("insert or ignore into Words (Word, LanguageUsageCount, UserSelectionCount) values ");
		var parameters = new (string, object?)[batch.Count * 2];
		for (int i = 0; i < batch.Count; i++)
		{
			if (i > 0)
				sql.Append(',');
			sql.Append("(@w").Append(i).Append(",@c").Append(i).Append(",0)");
			parameters[i * 2] = ("@w" + i, batch[i].Word);
			parameters[i * 2 + 1] = ("@c" + i, batch[i].LanguageUsageCount);
		}
		Database.ExecuteNonQuery(sql.ToString(), parameters);
	}

	private void PerformDbMaintenance()
	{
		int cutoff = DateInt.FromDate(Clock.UtcNow.Date.AddDays(-30));
		Database.ExecuteNonQuery(
			"delete from WordSequences where LastUsedDate <= @cutoff",
			("@cutoff", cutoff));
	}
}
