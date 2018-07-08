using BlinkTalk.Typing.Persistence;
using System.Collections.Generic;
using System.Linq;

namespace BlinkTalk.Typing
{

    public static class PhraseService
    {
        public static List<string> GetWordSuggestions(IEnumerable<int> wordIds, string currentWord, int numberOfWords)
        {
            var conditions = new List<string>();
            wordIds = wordIds.Reverse().Take(3).Reverse();
            int wordNumber = 1;
            foreach (int wordId in wordIds)
            {
                conditions.Add($"WordSequences.Word{wordNumber}Id = {wordId}");
                wordNumber++;
            }
            if (!string.IsNullOrEmpty(currentWord))
                conditions.Add($"Words.Word like '{currentWord}%'");

            string sqlConditions = string.Join(" and ", conditions);
            if (!string.IsNullOrEmpty(sqlConditions))
                sqlConditions = "where " + sqlConditions;

            string sql = $"" +
                $"Select distinct Words.Word from WordSequences join Words on Words.Id = WordSequences.Word{wordNumber}Id" +
                $" {sqlConditions}" +
                $" order by WordSequences.UsageCount desc, LastUsedDate desc, Words.UserSelectionCount desc" +
                $" limit {numberOfWords}";
            DataTable data = PersistenceService.DB.ExecuteQuery(sql);
            return data.Rows.Select(x => (string)x["Word"]).ToList();
        }

        /// <summary>
        /// This method will run a 4 word sliding window across the sentence and update
        /// the database so we can learn which word sequences the user uses most often.
        /// The entries will be 
        /// Word1, null, null, null
        /// Word1, Word2, null, null
        /// Word1, Word2, Word3, null
        /// Word1, Word2, Word3, Word4
        /// Word2, Word3, Word4, Word5
        /// etc
        /// </summary>
        /// <param name="wordIds"></param>
        public static void IncrementPhraseUsage(IEnumerable<int> wordIds)
        {
            var phraseWindow = new List<int>();
            foreach(int wordId in wordIds)
            {
                phraseWindow.Add(wordId);
                // Ensure a maximum of 4 words
                if (phraseWindow.Count > 4)
                    phraseWindow.RemoveAt(0);
                IncrementPhraseWindow(phraseWindow);
            }
        }

        private static void IncrementPhraseWindow(List<int> wordIds)
        {
            var conditions = new List<string>();
            var sqlWordIds = new List<string>();
            for(int wordIndex = 0; wordIndex < 4; wordIndex++)
            {
                int wordNumber = wordIndex + 1;
                string wordIdValue = wordIndex < wordIds.Count ? wordIds[wordIndex].ToString() : "null";
                conditions.Add($"Word{wordNumber}Id = {wordIdValue}");
                sqlWordIds.Add(wordIdValue);
            }
            string sqlConditions = string.Join(" and ", conditions);

            int today = PersistenceService.DB.TodayAsInt();
            string sql = $"Select ID from WordSequences where {sqlConditions} limit 1";
            DataTable data = PersistenceService.DB.ExecuteQuery(sql);
            if (data.Rows.Count == 0)
            {
                sql = "Insert into WordSequences(Word1Id, Word2Id, Word3Id, Word4Id, UsageCount, LastUsedDate)" +
                    $" values ({sqlWordIds[0]}, {sqlWordIds[1]}, {sqlWordIds[2]}, {sqlWordIds[3]}, 1, {today})";
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