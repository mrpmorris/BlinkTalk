using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace BlinkTalk.Application.Persistence;

/// <summary>
/// Reads a word-list pack: a zip containing a single CSV of Word,LanguageUsageCount rows, with
/// or without a header row (e.g. LanguagePacks/French.zip → French.csv). Shared by the app's
/// seed-word source and the tests so both parse the asset identically.
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
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                // A "Word,LanguageUsageCount" header fails to parse and is skipped like any other
                // malformed line. Skipping the first line unconditionally would instead discard
                // the single most frequent word of every list that has no header — which is all
                // of the shipped ones.
                if (TryParseLine(line, out string word, out int count))
                    yield return (word, count);
            }
        }
    }

    private static bool TryParseLine(string line, out string word, out int count)
    {
        word = "";
        count = 0;

        int comma = line.LastIndexOf(',');
        if (comma <= 0)
            return false;
        // Invariant parsing: the counts are bare integers, but the app runs under the person's
        // chosen language and a culture-sensitive parse would read a digit group separator.
        if (!int.TryParse(line.Substring(comma + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out count))
            return false;

        word = line.Substring(0, comma).Trim();
        return word.Length > 0;
    }
}
