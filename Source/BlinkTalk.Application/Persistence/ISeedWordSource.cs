using System.Collections.Generic;

namespace BlinkTalk.Application.Persistence;

/// <summary>
/// Supplies the language dictionary used to seed the Words table when the database is first
/// created. Implemented in the app by reading the bundled word-list asset (a zipped CSV of
/// Word,LanguageUsageCount rows); tests read the same asset from disk.
/// </summary>
public interface ISeedWordSource
{
    IEnumerable<(string Word, int LanguageUsageCount)> GetWords();
}
