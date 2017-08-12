using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts
{
    public static class WordList
    {
        private const int MaximumNumberOfWordsInSequence = 4;
        private const string LastWordColumnName = "Word4ID";
        private static readonly AutoMigratingDatabase db = new AutoMigratingDatabase("English.db");
        private static List<int> mostRecentlyUsedWordIds;

        private enum WordSequenceSqlConditionsPurpose
        {
            ExactMatch,
            ForPredictiveSearching
        }

        static WordList()
        {
            ResetState();
        }

        public static List<string> GetSuggestions(string startOfWord, char[] potentialNextChars)
        {
            startOfWord = (startOfWord ?? "");
            List<string> result = new List<string>();
            result = SuggestFromWordHistory(startOfWord, potentialNextChars);
            result.AddRange(SuggestFromPartiallyEnteredWord(startOfWord, potentialNextChars, result));
            return result;
        }

        public static void ResetState()
        {
            mostRecentlyUsedWordIds = Enumerable.Repeat(-1, MaximumNumberOfWordsInSequence).ToList();
        }

        public static void UpdateWordUsage(string word)
        {
            if (string.IsNullOrEmpty(word))
                return;

            word = word.Replace("'", "''").ToLowerInvariant();
            UpdateIndividualWordUsage(word);
            UpdateWordSequenceUsage(word);
        }

        private static void UpdateIndividualWordUsage(string word)
        {
            int wordCount = 0;
            string sql = "select UserSelectionCount from Words where Word ='" + word + "' limit 1;";
            var dbResult = db.ExecuteQuery(sql);
            if (dbResult.Rows.Count == 0)
            {
                sql = "insert into Words(Word, LanguageUsageCount, UserSelectionCount) " +
                    "values ('" + word + "', 0, 1);";
                wordCount = 1;
            }
            else
            {
                wordCount = (int)dbResult.Rows[0]["UserSelectionCount"] + 1;
                sql = "update Words set UserSelectionCount = " + wordCount
                    + " where word = '" + word + "';";

            }
            db.ExecuteNonQuery(sql);
        }

        private static void UpdateWordSequenceUsage(string word)
        {
            int wordId;
            if (!TryGetWordId(word, out wordId))
                return;

            if (mostRecentlyUsedWordIds[2] == wordId)
                return;

            mostRecentlyUsedWordIds.Add(wordId);
            if (mostRecentlyUsedWordIds.Count > MaximumNumberOfWordsInSequence)
                mostRecentlyUsedWordIds.RemoveAt(0);

            var sql = new StringBuilder();
            int wordSequenceId;
            int usageCount;
            if (!TryGetWordSequenceIdAndUsageCount(out wordSequenceId, out usageCount))
            {
                sql.Append("insert into WordSequences (");
                sql.Append(string.Join(",", Enumerable.Range(1, MaximumNumberOfWordsInSequence).Select(x => "Word" + x + "ID").ToArray()));
                sql.Append(", UsageCount, LastUsedDate) values(");
                sql.Append(string.Join(",", mostRecentlyUsedWordIds.Select(x => x + "").ToArray()));
                sql.Append(", 1, " + db.TodayAsInt() + ");");
            }
            else
            {
                sql.Append("update WordSequences set UsageCount = " + (usageCount + 1) + ", LastUsedDate=" + db.TodayAsInt());
                sql.Append(" where ID = " + wordSequenceId + ";");
            }
            db.ExecuteNonQuery(sql + "");
        }

        private static string GetWordSequenceSqlConditions(WordSequenceSqlConditionsPurpose purpose)
        {
            int firstWordNumber;
            int numberOfWords;
            int firstIndexNumber; //So the SQL has Word2ID= when we are looking at the value of Word3
            var conditions = new List<string>();

            switch (purpose)
            {
                case WordSequenceSqlConditionsPurpose.ExactMatch:
                    firstWordNumber = 1;
                    numberOfWords = MaximumNumberOfWordsInSequence;
                    firstIndexNumber = 0;
                    break;

                case WordSequenceSqlConditionsPurpose.ForPredictiveSearching:
                    firstWordNumber = 1;
                    numberOfWords = MaximumNumberOfWordsInSequence - 1;
                    firstIndexNumber = 1;
                    break;

                default:
                    throw new NotImplementedException(purpose + "");
            }

            int currentWordIndex = firstIndexNumber;
            int currentWordNumber = firstWordNumber;
            for (int i = firstWordNumber; i <= numberOfWords + 1 - firstWordNumber; i++)
            {
                string wordColumnName = "Word" + currentWordNumber + "ID";
                int wordIDToSearchFor = mostRecentlyUsedWordIds[currentWordIndex];
                switch(purpose)
                {
                    case WordSequenceSqlConditionsPurpose.ExactMatch:
                        conditions.Add(wordColumnName + "=" + wordIDToSearchFor);
                        break;

                    case WordSequenceSqlConditionsPurpose.ForPredictiveSearching:
                        if (wordIDToSearchFor != -1)
                            conditions.Add("(" + wordColumnName + "=" + wordIDToSearchFor + " or " + wordColumnName + "= -1)");
                        break;

                    default:
                        throw new NotImplementedException(purpose + "");
                }
                currentWordIndex++;
                currentWordNumber++;
            }
            if (conditions.Count == 0)
                return "";
            return string.Join(" and ", conditions.ToArray());
        }

        private static bool TryGetWordSequenceIdAndUsageCount(out int wordSequenceId, out int usageCount)
        {
            wordSequenceId = 0;
            usageCount = 0;

            string sql = "select ID, UsageCount from WordSequences where " + GetWordSequenceSqlConditions(WordSequenceSqlConditionsPurpose.ExactMatch) + " limit 1;";
            DataTable dbResult = db.ExecuteQuery(sql);
            if (dbResult.Rows.Count == 0)
                return false;
            wordSequenceId = (int)dbResult.Rows[0]["ID"];
            usageCount = (int)dbResult.Rows[0]["UsageCount"];
            return true;
        }

        private static bool TryGetWordId(string word, out int id)
        {
            string sql = "select ID from Words where Word='" + word + "';";
            DataTable dbResult = db.ExecuteQuery(sql);
            if (dbResult.Rows.Count == 0)
            {
                id = 0;
                return false;
            }
            id = (int)dbResult.Rows[0]["ID"];
            return true;
        }

        private static string CreateStartOfWordsSql(string startOfWord, char[] potentialNextChars)
        {
            startOfWord = (startOfWord ?? "").ToLowerInvariant();
            potentialNextChars = potentialNextChars ?? new char[0];
            string result = string.Join(" or ", potentialNextChars.Select(x => "Words.Word like '" + startOfWord + x + "%'").ToArray());
            return result;
        }

        private static List<string> SuggestFromPartiallyEnteredWord(string startOfWord, char[] potentialNextChars, List<string> alreadyIncludedWords)
        {
            alreadyIncludedWords = alreadyIncludedWords ?? new List<string>();
            if (alreadyIncludedWords.Count == 3)
                return new List<string>();

            string excludedWordsSql =
                string.Join(" and ", alreadyIncludedWords.Select(x => "Words.Word != '" + x + "'").ToArray());
            string wordBeginningsSql = CreateStartOfWordsSql(startOfWord, potentialNextChars);

            var sql = new StringBuilder();
            sql.Append("select * from Words where ");
            if (!string.IsNullOrEmpty(excludedWordsSql))
            {
                sql.Append(excludedWordsSql);
                if (!string.IsNullOrEmpty(wordBeginningsSql))
                    sql.Append(" and ");
            }
            sql.Append(" (" + wordBeginningsSql + ")");
            sql.Append(" order by UserSelectionCount desc, LanguageUsageCount desc limit 3;");
            var dbResult = db.ExecuteQuery(sql + "");
            var result = dbResult.Rows.Select(row => (string)row["Word"]).ToList();
            return result;
        }

        private static List<string> SuggestFromWordHistory(string startOfWord, char[] potentialNextChars)
        {
            string wordStartSql = CreateStartOfWordsSql(startOfWord, potentialNextChars);
            string wordSequenceSql = GetWordSequenceSqlConditions(WordSequenceSqlConditionsPurpose.ForPredictiveSearching);
            var sql = new StringBuilder();
            sql.Append("select Words.Word as SuggestedWord, ");
            sql.Append(" case when Word1ID = -1 then 1 else 2 end score1,");
            sql.Append(" case when Word2ID = -1 then 1 else 2 end score2,");
            sql.Append(" case when Word3ID = -1 then 1 else 2 end score3");
            sql.Append(" from WordSequences");
            sql.Append(" left join Words on Words.ID = WordSequences." + LastWordColumnName);
            if (!string.IsNullOrEmpty(wordStartSql) || !string.IsNullOrEmpty(wordSequenceSql))
                sql.Append(" where ");
            if (!string.IsNullOrEmpty(wordSequenceSql))
            {
                sql.Append(wordSequenceSql);
                if (!string.IsNullOrEmpty(wordSequenceSql))
                    sql.Append(" and ");
            }
            if (!string.IsNullOrEmpty(wordStartSql))
                sql.Append("(" + wordStartSql + ")");
            sql.Append(" order by (score1 + score2 + score3) desc, UsageCount desc, LastUsedDate desc");
            DataTable dbResult = db.ExecuteQuery("select distinct SuggestedWord from (" +sql + ") limit 3;");
            var values = dbResult.Rows.Select(x => (string)x["SuggestedWord"]).ToList();
            return values;
        }


    }
}