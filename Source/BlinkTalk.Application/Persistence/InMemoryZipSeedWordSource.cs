using System.Collections.Generic;
using System.IO;

namespace BlinkTalk.Application.Persistence;

/// <summary>
/// Seeds the Words dictionary from a word-list zip already held in memory — the downloaded
/// language pack. The bytes are never written to disk; only the database they seed is.
/// </summary>
public sealed class InMemoryZipSeedWordSource : ISeedWordSource
{
    private readonly byte[] ZipBytes;

    public InMemoryZipSeedWordSource(byte[] zipBytes)
    {
        ZipBytes = zipBytes;
    }

    public IEnumerable<(string Word, int LanguageUsageCount)> GetWords()
    {
        using var zip = new MemoryStream(ZipBytes, writable: false);
        foreach (var word in WordListZipReader.Read(zip))
            yield return word;
    }
}
