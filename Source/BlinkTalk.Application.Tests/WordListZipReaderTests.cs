using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using BlinkTalk.Application.Persistence;

namespace BlinkTalk.Application.Tests;

/// <summary>
/// Parsing rules for the word-list packs. The header row is optional: none of the shipped packs
/// has one, so a reader that always skipped the first line silently dropped the most frequent
/// word of every language.
/// </summary>
public class WordListZipReaderTests
{
    [Fact]
    public void FirstWordOfAHeaderlessListIsKept()
    {
        var words = Read("the,9755\nof,5312\n");

        Assert.Equal(new[] { ("the", 9755), ("of", 5312) }, words);
    }

    [Fact]
    public void HeaderRowIsSkipped()
    {
        var words = Read("Word,LanguageUsageCount\nthe,9755\nof,5312\n");

        Assert.Equal(new[] { ("the", 9755), ("of", 5312) }, words);
    }

    [Fact]
    public void MalformedLinesAreSkippedWithoutLosingTheRest()
    {
        var words = Read("the,9755\n\nnocount\nof,not-a-number\n,42\nof,5312\n");

        Assert.Equal(new[] { ("the", 9755), ("of", 5312) }, words);
    }

    [Fact]
    public void WordsMayContainACommaBecauseOnlyTheLastOneSeparatesTheCount()
    {
        var words = Read("don't,120\n");

        Assert.Equal(new[] { ("don't", 120) }, words);
    }

    private static (string Word, int LanguageUsageCount)[] Read(string csv)
    {
        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var entry = archive.CreateEntry("Words.csv").Open();
            byte[] bytes = new UTF8Encoding(false).GetBytes(csv);
            entry.Write(bytes, 0, bytes.Length);
        }
        buffer.Position = 0;
        return WordListZipReader.Read(buffer).ToArray();
    }
}
