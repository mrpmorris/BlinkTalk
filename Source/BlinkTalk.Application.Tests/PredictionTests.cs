using System;
using System.IO;
using System.Linq;
using BlinkTalk.Application.Persistence;
using BlinkTalk.Application.Prediction;

namespace BlinkTalk.Application.Tests
{
    public class PredictionTests
    {
        private static MicrosoftDataSqliteDatabase NewInMemoryDb()
        {
            var db = new MicrosoftDataSqliteDatabase(":memory:");
            db.ExecuteNonQuery(
                "CREATE TABLE Words(ID INTEGER PRIMARY KEY AUTOINCREMENT, Word TEXT, UserSelectionCount INT, LanguageUsageCount INT)");
            db.ExecuteNonQuery(
                "CREATE TABLE WordSequences(ID INTEGER PRIMARY KEY AUTOINCREMENT, PrecedingWord3Id INT, PrecedingWord2Id INT, PrecedingWord1Id INT, SuggestedWordId INT, UsageCount INT, LastUsedDate INT)");
            return db;
        }

        [Fact]
        public void PhraseSuggestionsRankContextMatchAboveUsageThenUsageWithinSameScore()
        {
            using var db = NewInMemoryDb();
            db.ExecuteNonQuery(
                "INSERT INTO Words(ID,Word,UserSelectionCount,LanguageUsageCount) VALUES " +
                "(1,'hello',0,0),(2,'xword',0,0),(3,'yword',0,0),(4,'zword',0,0)");
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
        public void PhrasePrefixFiltersSuggestions()
        {
            using var db = NewInMemoryDb();
            db.ExecuteNonQuery(
                "INSERT INTO Words(ID,Word,UserSelectionCount,LanguageUsageCount) VALUES " +
                "(1,'hello',0,0),(2,'apple',0,0),(3,'ant',0,0)");
            db.ExecuteNonQuery(
                "INSERT INTO WordSequences(PrecedingWord3Id,PrecedingWord2Id,PrecedingWord1Id,SuggestedWordId,UsageCount,LastUsedDate) VALUES " +
                "(-1,-1,1,2,5,20260101)," +   // apple
                "(-1,-1,1,3,5,20260101)");    // ant

            var phrase = new PhraseService(db, new FixedClock());
            var result = phrase.GetWordSuggestions(new[] { 1 }, "ap", 6);

            Assert.Equal(new[] { "apple" }, result);
        }

        [Fact]
        public void IncrementPhraseUsageInsertsThenIncrementsWindows()
        {
            using var db = NewInMemoryDb();
            db.ExecuteNonQuery(
                "INSERT INTO Words(ID,Word,UserSelectionCount,LanguageUsageCount) VALUES (1,'i',0,0),(2,'am',0,0)");
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

        // --- Parity against the shipped English.db ---

        private static string CopyRealDbToTemp()
        {
            string source = Path.Combine(AppContext.BaseDirectory, "English.db");
            string temp = Path.Combine(Path.GetTempPath(), "blinktalk_test_" + Guid.NewGuid().ToString("N") + ".db");
            File.Copy(source, temp, overwrite: true);
            return temp;
        }

        [Fact]
        public void RealDatabaseSchemaSupportsDictionaryAndPhraseQueries()
        {
            string temp = CopyRealDbToTemp();
            try
            {
                using var db = new MicrosoftDataSqliteDatabase(temp);
                var words = new WordService(db);

                // Dictionary prefix lookup returns real suggestions (schema/columns match the ported SQL).
                // WordService normalises the query to upper case, and the shipped DB stores words
                // upper case, so results come back upper case and prefixed with "TH".
                var dictionary = words.GetWordSuggestions("th", 6);
                Assert.NotEmpty(dictionary);
                Assert.All(dictionary, w => Assert.StartsWith("TH", w));
                Assert.All(dictionary, w => Assert.Equal(w.ToUpperInvariant(), w));

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
    }
}
