using System.Collections.Generic;
using System.Linq;

namespace BlinkTalk.Typing.Persistence
{

    public static class PhraseService
    {
        public static List<string> GetWordSuggestions(IEnumerable<int> wordIds, string currentWord, int numberOfWords)
        {
            var conditions = new List<string>();
            wordIds = wordIds.Reverse().Take(3);
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
                $"Select Words.Word from WordSequences join Words on Words.Id = WordSequences.Word{wordNumber}Id " +
                $"{sqlConditions} " +
                $"order by WordSequences.UsageCount desc, LastUsedDate desc, Words.UserSelectionCount desc " +
                $"limit {numberOfWords}";
            DataTable data = PersistenceService.DB.ExecuteQuery(sql);
            return data.Rows.Select(x => (string)x["Word"]).ToList();
        }
    }
}