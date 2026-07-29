using System;
using System.Collections.Generic;
using System.Linq;
using BlinkTalk.Application.Persistence;
using BlinkTalk.Application.Text;

namespace BlinkTalk.Application.Prediction;

/// <summary>
/// Dictionary-level word lookups against the Words table: tracking how often the user
/// picks each word, creating new words, and prefix-based suggestions ranked by the user's
/// own usage then general language frequency. Ported from the original static WordService,
/// now injected and with user text bound as parameters rather than interpolated.
/// </summary>
public sealed class WordService : IWordService
{
    private readonly ISqliteDatabase Database;

    public WordService(ISqliteDatabase database)
    {
        Database = database;
    }

    public void DecreaseWordUsage(int wordId)
    {
        Database.ExecuteNonQuery(
            "Update Words set UserSelectionCount = UserSelectionCount - 1 where ID = @id",
            ("@id", wordId));
    }

    /// <summary>
    /// Prefix suggestions, matched against the folded SearchWord so what the person types finds
    /// accented words: CAFE finds "café", كتب finds كَتَبَ.
    /// </summary>
    public List<string> GetWordSuggestions(string? currentWord, int numberOfWords)
    {
        string prefix = TextFold.Fold(currentWord);

        string conditions = prefix.Length == 0 ? "" : "where SearchWord like @prefix";
        string sql =
            $"Select Word from Words {conditions} " +
            $"order by UserSelectionCount desc, LanguageUsageCount desc limit {numberOfWords}";

        DataTable data = prefix.Length == 0
            ? Database.ExecuteQuery(sql)
            : Database.ExecuteQuery(sql, ("@prefix", prefix + "%"));

        return data.Rows.Select(x => (string)x["Word"]!).ToList();
    }

    public void IncreaseWordUsage(string word, out int wordId)
    {
        wordId = FindBestMatchingWordId(word);
        if (wordId == -1)
            wordId = CreateWord(word);

        Database.ExecuteNonQuery(
            "Update Words set UserSelectionCount = UserSelectionCount + 1 where Id = @id",
            ("@id", wordId));
    }

    private int CreateWord(string word)
    {
        Database.ExecuteNonQuery(
            "Insert into Words (Word, SearchWord, LanguageUsageCount, UserSelectionCount) values (@word, @search, 0, 0)",
            ("@word", word),
            ("@search", TextFold.Fold(word)));
        return GetWordId(word);
    }

    /// <summary>
    /// The row a typed word should credit, or -1 for a word the dictionary has never seen.
    /// <para>
    /// The keyboard types in upper case while the word lists are authored in their natural casing,
    /// and the accent keys are optional, so an exact match is the exception rather than the rule.
    /// Preferring an existing row over creating one is what keeps a person's learning on the same
    /// row as the language frequency instead of splitting it across near-duplicates.
    /// </para>
    /// </summary>
    private int FindBestMatchingWordId(string word)
    {
        DataTable candidates = Database.ExecuteQuery(
            "Select ID, Word, UserSelectionCount, LanguageUsageCount from Words where SearchWord = @search",
            ("@search", TextFold.Fold(word)));
        if (candidates.Rows.Count == 0)
            return -1;

        string caseFolded = TextFold.FoldCase(word);
        int bestId = -1;
        int bestRank = -1;
        int bestUserSelectionCount = -1;
        int bestLanguageUsageCount = -1;
        foreach (DataRow row in candidates.Rows)
        {
            string candidate = (string)row["Word"]!;
            // The same spelling beats the same accents, which beats merely folding the same way.
            int rank =
                candidate == word ? 2
                : TextFold.FoldCase(candidate) == caseFolded ? 1
                : 0;
            int userSelectionCount = Convert.ToInt32(row["UserSelectionCount"]);
            int languageUsageCount = Convert.ToInt32(row["LanguageUsageCount"]);

            bool better =
                rank > bestRank
                || (rank == bestRank && userSelectionCount > bestUserSelectionCount)
                || (rank == bestRank && userSelectionCount == bestUserSelectionCount
                    && languageUsageCount > bestLanguageUsageCount);
            if (!better)
                continue;

            bestId = Convert.ToInt32(row["ID"]);
            bestRank = rank;
            bestUserSelectionCount = userSelectionCount;
            bestLanguageUsageCount = languageUsageCount;
        }
        return bestId;
    }

    private int GetWordId(string word)
    {
        object? id = Database.ExecuteScalar(
            "Select ID from Words where Word = @word",
            ("@word", word));
        return id == null ? -1 : Convert.ToInt32(id);
    }
}
