using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using BlinkTalk.Application.Persistence;
using BlinkTalk.Application.Prediction;

namespace BlinkTalk.Application.Tests;

/// <summary>
/// Guards the UTF-8 path from the bundled word list through to a prefix lookup. The English list
/// is pure ASCII, so nothing here is exercised by it — these matter for the accented lists
/// (French/Spanish/German). A list saved as a non-UTF-8 codepage would otherwise surface as
/// mojibake in the person's vocabulary rather than as an exception.
/// </summary>
public class WordListEncodingTests
{
    private const string Csv = "Word,LanguageUsageCount\nÉTÉ,500\nGRÜSSEN,400\nMAÑANA,300\nNAÏVE,200\n";

    private static readonly string[] ExpectedWords = { "GRÜSSEN", "MAÑANA", "NAÏVE", "ÉTÉ" };

    [Theory]
    [InlineData(false)] // UTF-8, no BOM — how the shipped lists are authored
    [InlineData(true)]  // UTF-8 with BOM — the BOM must be stripped, not parsed as part of a word
    public void AccentedWordsSurviveSeedingAndPrefixLookup(bool withBom)
    {
        using var db = new MicrosoftDataSqliteDatabase(":memory:");
        new AutoMigratingDatabase(db, new FixedClock(), new ZipSeed(MakeZip(Csv, new UTF8Encoding(withBom))))
            .Migrate();

        var stored = db.ExecuteQuery("select Word from Words").Rows
            .Select(r => (string)r["Word"]!)
            .OrderBy(w => w, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(ExpectedWords, stored);

        // The accented characters must round-trip through a query parameter too, not just storage.
        var words = new WordService(db);
        Assert.Contains("MAÑANA", words.GetWordSuggestions("MAÑ", 6));
    }

    [Fact]
    public void CreatedDatabaseIsUtf8Encoded()
    {
        // A file-backed database, since the encoding is fixed when the file is first written.
        string path = Path.Combine(Path.GetTempPath(), "blinktalk_encoding_" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            using var db = new MicrosoftDataSqliteDatabase(path);
            new AutoMigratingDatabase(db, new FixedClock()).Migrate();
            Assert.Equal("UTF-8", Convert.ToString(db.ExecuteScalar("PRAGMA encoding")));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ShippedEnglishWordListParsesAndIsAscii()
    {
        using var zip = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "English.zip"));
        var words = WordListZipReader.Read(zip).ToList();

        Assert.NotEmpty(words);
        Assert.All(words, w => Assert.NotEqual(0, w.LanguageUsageCount));
        // The replacement char is what a mis-decoded byte becomes; its absence proves the decode.
        Assert.All(words, w => Assert.DoesNotContain('�', w.Word));
    }

    private sealed class ZipSeed : ISeedWordSource
    {
        private readonly byte[] Zip;

        public ZipSeed(byte[] zip) => Zip = zip;

        public IEnumerable<(string Word, int LanguageUsageCount)> GetWords()
        {
            using var stream = new MemoryStream(Zip);
            foreach (var word in WordListZipReader.Read(stream))
                yield return word;
        }
    }

    private static byte[] MakeZip(string csv, Encoding encoding)
    {
        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var entry = archive.CreateEntry("Words.csv").Open();
            byte[] bytes = encoding.GetPreamble().Concat(encoding.GetBytes(csv)).ToArray();
            entry.Write(bytes, 0, bytes.Length);
        }
        return buffer.ToArray();
    }
}
