using System;
using System.Collections.Generic;
using System.Linq;
using BlinkTalk.Application.Persistence;

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

    public void IncreaseWordUsage(string word, out int wordId)
    {
        word = word.ToUpper();

        wordId = GetWordId(word);
        if (wordId == -1)
            wordId = CreateWord(word);

        Database.ExecuteNonQuery(
            "Update Words set UserSelectionCount = UserSelectionCount + 1 where Id = @id",
            ("@id", wordId));
    }

    public void DecreaseWordUsage(int wordId)
    {
        Database.ExecuteNonQuery(
            "Update Words set UserSelectionCount = UserSelectionCount - 1 where ID = @id",
            ("@id", wordId));
    }

    public List<string> GetWordSuggestions(string? currentWord, int numberOfWords)
    {
        currentWord = (currentWord ?? "").ToUpper();

        string conditions = string.IsNullOrEmpty(currentWord) ? "" : "where Word like @prefix";
        string sql =
            $"Select Word from Words {conditions} " +
            $"order by UserSelectionCount desc, LanguageUsageCount desc limit {numberOfWords}";

        DataTable data = string.IsNullOrEmpty(currentWord)
            ? Database.ExecuteQuery(sql)
            : Database.ExecuteQuery(sql, ("@prefix", currentWord + "%"));

        return data.Rows.Select(x => (string)x["Word"]!).ToList();
    }

    private int GetWordId(string word)
    {
        DataTable data = Database.ExecuteQuery(
            "Select ID from Words where Word = @word",
            ("@word", word));
        int result = -1;
        if (data.Rows.Count == 1)
            result = Convert.ToInt32(data.Rows[0]["ID"]);
        return result;
    }

    private int CreateWord(string word)
    {
        Database.ExecuteNonQuery(
            "Insert into Words (Word, LanguageUsageCount, UserSelectionCount) values (@word, 0, 0)",
            ("@word", word));
        return GetWordId(word);
    }
}
