using System;
using System.Collections.Generic;
using System.Linq;
using BlinkTalk.Application.Abstractions;
using BlinkTalk.Application.Persistence;

namespace BlinkTalk.Application.Prediction
{
    /// <summary>
    /// The n-gram phrase model. A 4-word sliding window over each committed sentence is stored
    /// in WordSequences; suggestions for the next word are scored by how well the last three
    /// typed words match the preceding-word slots, then by usage, recency and the user's own
    /// selection count. The scoring SQL is kept identical to the original so suggestion ordering
    /// matches; only the user-entered prefix is bound as a parameter (the original interpolated it).
    /// </summary>
    public sealed class PhraseService : IPhraseService
    {
        private readonly ISqliteDatabase _database;
        private readonly IClock _clock;

        public PhraseService(ISqliteDatabase database, IClock clock)
        {
            _database = database;
            _clock = clock;
        }

        /// <summary>
        /// Runs a 4-word sliding window across the sentence and updates the database so we learn
        /// which word sequences the user uses most often. For a 5 word sentence the entries are
        /// (null,null,null,w1) (null,null,w1,w2) (null,w1,w2,w3) (w1,w2,w3,w4) (w2,w3,w4,w5).
        /// </summary>
        public void IncrementPhraseUsage(IEnumerable<int> wordIds)
        {
            IEnumerable<PhraseWindow> phraseWindows = PhraseWindow.CreatePhraseWindows(wordIds);
            foreach (PhraseWindow phraseWindow in phraseWindows)
                IncrementPhraseWindow(phraseWindow);
        }

        public List<string> GetWordSuggestions(IEnumerable<int> wordIds, string? currentWord, int numberOfWords)
        {
            // First ensure there are at least 3 words
            var nullableWordIds = new List<int?> { null, null, null };
            nullableWordIds.AddRange(wordIds.Cast<int?>());
            // Only take the last 3 values
            nullableWordIds = nullableWordIds
                .Select(x => x)
                .Reverse()
                .Take(3)
                .Reverse()
                .ToList();

            string nullableWordIdsAsString = ConvertNullableIntsToCommaList(nullableWordIds);
            string scoreSql = CreateScoreSql(nullableWordIds);

            string sqlConditions =
                $" PrecedingWord3Id in ({nullableWordIdsAsString})" +
                $" and PrecedingWord2Id in ({nullableWordIdsAsString})" +
                $" and PrecedingWord1Id in ({nullableWordIdsAsString})";
            bool hasPrefix = !string.IsNullOrEmpty(currentWord);
            if (hasPrefix)
                sqlConditions += " and Words.Word like @prefix";

            string sql =
                $"Select {scoreSql}, Words.Word from WordSequences join Words on Words.Id = WordSequences.SuggestedWordId" +
                $" where {sqlConditions}" +
                $" order by (Score1 + Score2 + Score3) desc, WordSequences.UsageCount desc, LastUsedDate desc, Words.UserSelectionCount desc";

            DataTable data = hasPrefix
                ? _database.ExecuteQuery(sql, ("@prefix", currentWord + "%"))
                : _database.ExecuteQuery(sql);

            return data.Rows
                .Select(x => (string)x["Word"]!)
                .Distinct()
                .Take(numberOfWords)
                .ToList();
        }

        private static string ToStringOrDefault(int? value) => value?.ToString() ?? "-1";

        private static string ConvertNullableIntsToCommaList(IEnumerable<int?> nullableWordIds)
        {
            return string.Join(",", nullableWordIds.Select(ToStringOrDefault));
        }

        private static string CreateScoreSql(List<int?> nullableWordIds)
        {
            if (nullableWordIds.Count != 3)
                throw new ArgumentException("Must be 3 elements", nameof(nullableWordIds));

            var calculatedColumns = new List<string>
            {
                CreateScoreSqlColumn(3, nullableWordIds, 3, 2, 1),
                CreateScoreSqlColumn(2, nullableWordIds, 2, 3, 2),
                CreateScoreSqlColumn(1, nullableWordIds, 1, 2, 3)
            };
            return string.Join(",", calculatedColumns);
        }

        private static string CreateScoreSqlColumn(int precedingWordNumber, List<int?> nullableWordIds, params int[] scores)
        {
            string columnName = $"PrecedingWord{precedingWordNumber}Id";
            return $"case {columnName}" +
                $" when {ToStringOrDefault(nullableWordIds[0])} then {scores[0]}" +
                $" when {ToStringOrDefault(nullableWordIds[1])} then {scores[1]}" +
                $" when {ToStringOrDefault(nullableWordIds[2])} then {scores[2]}" +
                $" else 0 end Score{precedingWordNumber}";
        }

        private void IncrementPhraseWindow(PhraseWindow phraseWindow)
        {
            string sqlConditions =
                $"PrecedingWord3Id = {ToStringOrDefault(phraseWindow.PrecedingWord3Id)}" +
                $" and PrecedingWord2Id = {ToStringOrDefault(phraseWindow.PrecedingWord2Id)}" +
                $" and PrecedingWord1Id = {ToStringOrDefault(phraseWindow.PrecedingWord1Id)}" +
                $" and SuggestedWordId = {phraseWindow.SuggestedWordId}";

            int today = DateInt.Today(_clock);
            DataTable data = _database.ExecuteQuery($"Select ID from WordSequences where {sqlConditions} limit 1");
            if (data.Rows.Count == 0)
            {
                _database.ExecuteNonQuery(
                    "Insert into WordSequences(PrecedingWord3Id, PrecedingWord2Id, PrecedingWord1Id, SuggestedWordId, UsageCount, LastUsedDate)" +
                    $" values ({ToStringOrDefault(phraseWindow.PrecedingWord3Id)}," +
                    $" {ToStringOrDefault(phraseWindow.PrecedingWord2Id)}," +
                    $" {ToStringOrDefault(phraseWindow.PrecedingWord1Id)}," +
                    $" {phraseWindow.SuggestedWordId}, 1, {today})");
            }
            else
            {
                _database.ExecuteNonQuery(
                    $"Update WordSequences set UsageCount = UsageCount + 1, LastUsedDate = {today} where {sqlConditions}");
            }
        }
    }
}
