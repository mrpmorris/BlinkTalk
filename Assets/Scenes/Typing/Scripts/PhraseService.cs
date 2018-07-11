using BlinkTalk.Typing.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlinkTalk.Typing
{
    public static class PhraseService
    {
        /// <summary>
        /// This method will run a 4 word sliding window across the sentence and update
        /// the database so we can learn which word sequences the user uses most often.
        /// For a 5 word sentence the entries will be
        /// null, null, null, word1
        /// null, null, word1, word2
        /// null, word1, word2, word3
        /// word1, word2, word3, word4
        /// word2, word3, word4, word5
        /// </summary>
        /// <param name="wordIds"></param>
        public static void IncrementPhraseUsage(IEnumerable<int> wordIds)
        {
            IEnumerable<PhraseWindow> phraseWindows = PhraseWindow.CreatePhraseWindows(wordIds);
            foreach (PhraseWindow phraseWindow in phraseWindows)
                IncrementPhraseWindow(phraseWindow);
        }

        public static List<string> GetWordSuggestions(IEnumerable<int> wordIds, string currentWord, int numberOfWords)
        {
            // First ensure there are at least 3 words
            var nullableWordIds = new List<int?> { null, null, null };
            nullableWordIds.AddRange(wordIds.Cast<int?>());
            // Only take the last 3 values
            nullableWordIds = nullableWordIds
                .Select(x => x) // IEnumerable<string> instead of List<string>
                .Reverse() // Otherwise .Reverse is a List<T> method and not a LINQ extension
                .Take(3)
                .Reverse()
                .ToList();

            string nullableWordIdsAsString = ConvertNullableIntsToCommaList(nullableWordIds);
            string scoreSQL = CreateScoreSql(nullableWordIds);

            string sqlConditions =
                $" PrecedingWord3Id in ({nullableWordIdsAsString})" +
                $" and PrecedingWord2Id in ({nullableWordIdsAsString})" +
                $" and PrecedingWord1Id in ({nullableWordIdsAsString})";
            if (!string.IsNullOrEmpty(currentWord))
                sqlConditions += $" and Words.Word like '{currentWord}%'";

            string sql = $"" +
                $"Select {scoreSQL}, Words.Word from WordSequences join Words on Words.Id = WordSequences.SuggestedWordId" +
                $" where {sqlConditions}" +
                $" order by (Score1 + Score2 + Score3) desc, WordSequences.UsageCount desc, LastUsedDate desc, Words.UserSelectionCount desc";
            DataTable data = PersistenceService.DB.ExecuteQuery(sql);
            return data.Rows
                .Select(x => (string)x["Word"])
                .Distinct()
                .Take(numberOfWords)
                .ToList();
        }

        private static string ConvertNullableIntsToCommaList(IEnumerable<int?> nullableWordIds)
        {
            IEnumerable<string> stringValues = nullableWordIds
                .Select(x => x.ToStringOrDefault());
            return string.Join(",", stringValues);
        }

        private static string CreateScoreSql(List<int?> nullableWordIds)
        {
            if (nullableWordIds.Count != 3)
                throw new ArgumentException("Must be 3 elements", nameof(nullableWordIds));

            var calculatedColumns = new List<string>();
            calculatedColumns.Add(CreateScoreSqlColumn(3, nullableWordIds, 3, 2, 1));
            calculatedColumns.Add(CreateScoreSqlColumn(2, nullableWordIds, 2, 3, 2));
            calculatedColumns.Add(CreateScoreSqlColumn(1, nullableWordIds, 1, 2, 3));
            return string.Join(",", calculatedColumns);
        }

        private static string CreateScoreSqlColumn(int precedingWordNumber, List<int?> nullableWordIds, params int[] scores)
        {
            string columnName = $"PrecedingWord{precedingWordNumber}Id";
            string result = $"case {columnName}" +
                $" when {nullableWordIds[0].ToStringOrDefault()} then {scores[0]}" +
                $" when {nullableWordIds[1].ToStringOrDefault()} then {scores[1]}" +
                $" when {nullableWordIds[2].ToStringOrDefault()} then {scores[2]}" +
                $" else 0 end Score{precedingWordNumber}";
            return result;
        }

        private static void IncrementPhraseWindow(PhraseWindow phraseWindow)
        {
            string sqlConditions =
                $"PrecedingWord3Id = {phraseWindow.PrecedingWord3Id.ToStringOrDefault()}" +
                $" and PrecedingWord2Id = {phraseWindow.PrecedingWord2Id.ToStringOrDefault()}" +
                $" and PrecedingWord1Id = {phraseWindow.PrecedingWord1Id.ToStringOrDefault()}" +
                $" and SuggestedWordId = {phraseWindow.SuggestedWordId}";

            int today = PersistenceService.DB.TodayAsInt();
            string sql = $"Select ID from WordSequences where {sqlConditions} limit 1";
            DataTable data = PersistenceService.DB.ExecuteQuery(sql);
            if (data.Rows.Count == 0)
            {
                sql = "Insert into WordSequences(PrecedingWord3Id, PrecedingWord2Id, PrecedingWord1Id, SuggestedWordId, UsageCount, LastUsedDate)" +
                    $" values (" +
                    $" {phraseWindow.PrecedingWord3Id.ToStringOrDefault()}," +
                    $" {phraseWindow.PrecedingWord2Id.ToStringOrDefault()}," +
                    $" {phraseWindow.PrecedingWord1Id.ToStringOrDefault()}," +
                    $" {phraseWindow.SuggestedWordId}, 1, {today})";
            }
            else
            {
                sql = $"Update WordSequences set UsageCount = UsageCount + 1, LastUsedDate = {today} " +
                    $" where {sqlConditions}";
            }
            PersistenceService.DB.ExecuteNonQuery(sql);
        }


    }
}