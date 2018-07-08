using BlinkTalk.Typing.Persistence;
using System.Collections.Generic;
using System.Linq;

namespace BlinkTalk.Typing
{
    public static class WordService
    {
        public static void IncreaseWordUsage(string word, out int wordId)
        {
            word = word.ToLowerInvariant();

            wordId = GetWordId(word);
            if (wordId == -1)
                wordId = CreateWord(word);

            string sql = $"Update Words set UserSelectionCount = UserSelectionCount + 1 where Id = {wordId}";
            PersistenceService.DB.ExecuteNonQuery(sql);
        }

        public static void DecreaseWordUsage(int wordId)
        {
            string sql = $"Update Words set UserSelectionCount = UserSelectionCount - 1 where ID = '{wordId}'";
            PersistenceService.DB.ExecuteNonQuery(sql);
        }

        public static List<string> GetWordSuggestions(string currentWord, int numberOfWords)
        {
            currentWord = (currentWord ?? "").ToLowerInvariant();
            string conditions = string.IsNullOrEmpty(currentWord) ? "" : $"where Word like '{currentWord}%'";
            string sql = $"Select Word from Words {conditions} order by UserSelectionCount desc, LanguageUsageCount desc limit {numberOfWords}";
            DataTable data = PersistenceService.DB.ExecuteQuery(sql);
            return data.Rows.Select(x => (string)x["Word"]).ToList();
        }

        private static int GetWordId(string word)
        {
            string sql = $"Select ID from Words where Word = '{word}'";
            DataTable data = PersistenceService.DB.ExecuteQuery(sql);
            int result = -1;
            if (data.Rows.Count == 1)
                result = (int)data.Rows[0]["ID"];
            return result;
        }

        private static int CreateWord(string word)
        {
            string sql = $"Insert into Words (Word, LanguageUsageCount, UserSelectionCount) values ('{word}', 0, 0)";
            PersistenceService.DB.ExecuteNonQuery(sql);
            return GetWordId(word);
        }
    }
}