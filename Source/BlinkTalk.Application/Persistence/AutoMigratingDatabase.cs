using System;
using System.Collections.Generic;
using System.Text;
using BlinkTalk.Application.Abstractions;
using BlinkTalk.Application.Text;

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
	/// <summary>Rows per INSERT batch while seeding; 3 parameters each, well under SQLite's 999-parameter limit.</summary>
	private const int SeedBatchSize = 300;

	/// <summary>The WordSequences columns that hold a Words.ID.</summary>
	private static readonly string[] SequenceWordColumns =
	{
		"PrecedingWord3ID", "PrecedingWord2ID", "PrecedingWord1ID", "SuggestedWordID"
	};

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
		if (currentVersion < 2)
			UpdateSchemaToV2();
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
	/// Adds Words.SearchWord — the case- and accent-folded form the prefix lookups match against,
	/// because SQLite's own folding stops at ASCII (see <see cref="TextFold"/>). Existing databases
	/// carry the person's learned vocabulary, so this migrates in place rather than re-seeding.
	/// <para>
	/// It also merges rows that differ only in case. Until now a typed word was upper cased before
	/// being looked up, so selecting "the" created a second row "THE" alongside the seeded one and
	/// the two split the usage counts between them. Words that differ in their accents are left
	/// alone: "ou" and "où" are different words.
	/// </para>
	/// </summary>
	private void UpdateSchemaToV2()
	{
		InTransaction(() =>
		{
			Database.ExecuteNonQuery("ALTER TABLE Words ADD COLUMN SearchWord TEXT NOT NULL DEFAULT ''");

			MergeWordsDifferingOnlyInCase();

			// Backfill row by row: SearchWord is computed in C# because the folding rules are
			// Unicode-aware and SQLite has no expression that can reproduce them.
			foreach (DataRow row in Database.ExecuteQuery("select ID, Word from Words").Rows)
			{
				Database.ExecuteNonQuery(
					"update Words set SearchWord = @search where ID = @id",
					("@search", TextFold.Fold((string)row["Word"]!)),
					("@id", Convert.ToInt32(row["ID"])));
			}

			Database.ExecuteNonQuery("CREATE INDEX IX_Words_SearchWord ON Words (SearchWord)");
			// UPDATE, not INSERT: V1 already wrote the single DbInfo row, and a second one would
			// make the version read ambiguous.
			Database.ExecuteNonQuery("UPDATE DbInfo SET Version = 2");
		});
	}

	/// <summary>
	/// Collapses each group of Words rows sharing a <see cref="TextFold.FoldCase"/> key onto one
	/// survivor, folding the counts in and repointing the learned word sequences at it.
	/// </summary>
	private void MergeWordsDifferingOnlyInCase()
	{
		var groups = new Dictionary<string, List<WordRow>>(StringComparer.Ordinal);
		foreach (DataRow row in Database.ExecuteQuery(
			"select ID, Word, LanguageUsageCount, UserSelectionCount from Words").Rows)
		{
			var word = new WordRow(
				Convert.ToInt32(row["ID"]),
				(string)row["Word"]!,
				Convert.ToInt32(row["LanguageUsageCount"]),
				Convert.ToInt32(row["UserSelectionCount"]));
			string key = TextFold.FoldCase(word.Word);
			if (!groups.TryGetValue(key, out List<WordRow>? group))
				groups[key] = group = new List<WordRow>(1);
			group.Add(word);
		}

		var merges = new List<(int OldId, int NewId)>();
		foreach (List<WordRow> group in groups.Values)
		{
			if (group.Count == 1)
				continue;

			// The seeded row wins: it is the one carrying the language frequency, and its spelling
			// is the natural casing the word list was authored in.
			group.Sort((left, right) =>
			{
				int byLanguage = right.LanguageUsageCount.CompareTo(left.LanguageUsageCount);
				if (byLanguage != 0)
					return byLanguage;
				int byUser = right.UserSelectionCount.CompareTo(left.UserSelectionCount);
				return byUser != 0 ? byUser : left.Id.CompareTo(right.Id);
			});
			WordRow survivor = group[0];

			int userSelectionCount = 0;
			int languageUsageCount = 0;
			foreach (WordRow word in group)
			{
				// Summed, because both rows accumulated selections independently. The language
				// frequency is taken at its maximum instead: only the seeded row has one, so
				// summing would be the same number but would double it if a list ever held both
				// casings of a word.
				userSelectionCount += word.UserSelectionCount;
				if (word.LanguageUsageCount > languageUsageCount)
					languageUsageCount = word.LanguageUsageCount;
				if (word.Id != survivor.Id)
					merges.Add((word.Id, survivor.Id));
			}

			Database.ExecuteNonQuery(
				"update Words set UserSelectionCount = @user, LanguageUsageCount = @language where ID = @id",
				("@user", userSelectionCount),
				("@language", languageUsageCount),
				("@id", survivor.Id));
		}

		if (merges.Count == 0)
			return;

		// A temp table keeps the repointing to one statement per column however many merges there
		// are. IF EXISTS because the connection outlives a failed migration attempt.
		Database.ExecuteNonQuery("DROP TABLE IF EXISTS temp.MergeMap");
		Database.ExecuteNonQuery("CREATE TEMP TABLE MergeMap (OldID INTEGER PRIMARY KEY, NewID INTEGER NOT NULL)");
		foreach ((int oldId, int newId) in merges)
		{
			Database.ExecuteNonQuery(
				"insert into MergeMap (OldID, NewID) values (@old, @new)",
				("@old", oldId),
				("@new", newId));
		}

		foreach (string column in SequenceWordColumns)
		{
			Database.ExecuteNonQuery(
				$"update WordSequences set {column} = (select NewID from MergeMap where OldID = {column}) " +
				$"where {column} in (select OldID from MergeMap)");
		}

		CollapseDuplicateWordSequences();
		Database.ExecuteNonQuery("delete from Words where ID in (select OldID from MergeMap)");
		Database.ExecuteNonQuery("DROP TABLE temp.MergeMap");
	}

	/// <summary>
	/// Repointing the sequence columns can leave two rows describing the same 4-word window. Folds
	/// each such group onto its lowest-id row so the n-gram scoring still sees one row per window.
	/// </summary>
	private void CollapseDuplicateWordSequences()
	{
		const string WindowColumns = "PrecedingWord3ID, PrecedingWord2ID, PrecedingWord1ID, SuggestedWordID";
		Database.ExecuteNonQuery($@"
				update WordSequences set
					UsageCount = (
						select sum(Duplicate.UsageCount) from WordSequences Duplicate
						where Duplicate.PrecedingWord3ID = WordSequences.PrecedingWord3ID
							and Duplicate.PrecedingWord2ID = WordSequences.PrecedingWord2ID
							and Duplicate.PrecedingWord1ID = WordSequences.PrecedingWord1ID
							and Duplicate.SuggestedWordID = WordSequences.SuggestedWordID),
					LastUsedDate = (
						select max(Duplicate.LastUsedDate) from WordSequences Duplicate
						where Duplicate.PrecedingWord3ID = WordSequences.PrecedingWord3ID
							and Duplicate.PrecedingWord2ID = WordSequences.PrecedingWord2ID
							and Duplicate.PrecedingWord1ID = WordSequences.PrecedingWord1ID
							and Duplicate.SuggestedWordID = WordSequences.SuggestedWordID)
				where ID in (
					select min(ID) from WordSequences group by {WindowColumns} having count(1) > 1)");
		Database.ExecuteNonQuery($@"
				delete from WordSequences where ID not in (
					select min(ID) from WordSequences group by {WindowColumns})");
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
		var sql = new StringBuilder(
			"insert or ignore into Words (Word, SearchWord, LanguageUsageCount, UserSelectionCount) values ");
		var parameters = new (string, object?)[batch.Count * 3];
		for (int i = 0; i < batch.Count; i++)
		{
			if (i > 0)
				sql.Append(',');
			sql.Append("(@w").Append(i).Append(",@s").Append(i).Append(",@c").Append(i).Append(",0)");
			parameters[i * 3] = ("@w" + i, batch[i].Word);
			parameters[i * 3 + 1] = ("@s" + i, TextFold.Fold(batch[i].Word));
			parameters[i * 3 + 2] = ("@c" + i, batch[i].LanguageUsageCount);
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

	/// <summary>A Words row read into memory for the case-merge pass.</summary>
	private readonly struct WordRow
	{
		public readonly int Id;
		public readonly int LanguageUsageCount;
		public readonly int UserSelectionCount;
		public readonly string Word;

		public WordRow(int id, string word, int languageUsageCount, int userSelectionCount)
		{
			Id = id;
			Word = word;
			LanguageUsageCount = languageUsageCount;
			UserSelectionCount = userSelectionCount;
		}
	}
}
