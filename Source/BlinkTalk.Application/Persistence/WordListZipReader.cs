using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace BlinkTalk.Application.Persistence;

/// <summary>
/// Reads a bundled word-list asset: a zip containing a single CSV with a header row and
/// Word,LanguageUsageCount data rows (e.g. Resources/Raw/en-GB.zip → English.csv). Shared by
/// the app's seed-word source and the tests so both parse the asset identically.
/// </summary>
public static class WordListZipReader
{
    public static IEnumerable<(string Word, int LanguageUsageCount)> Read(Stream zipStream)
    {
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
        foreach (var entry in archive.Entries)
        {
            if (!entry.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                continue;

            // Explicit UTF-8: the word lists are authored as UTF-8 and non-English ones carry
            // accented characters, so we never want to fall back to the machine's ANSI codepage.
            // detectEncodingFromByteOrderMarks still strips a BOM if one is present.
            using var reader = new StreamReader(entry.Open(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            reader.ReadLine(); // header: Word,LanguageUsageCount
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                int comma = line.LastIndexOf(',');
                if (comma <= 0)
                    continue;
                string word = line.Substring(0, comma).Trim();
                if (word.Length == 0 || !int.TryParse(line.Substring(comma + 1), out int count))
                    continue;
                yield return (word, count);
            }
        }
    }
}
