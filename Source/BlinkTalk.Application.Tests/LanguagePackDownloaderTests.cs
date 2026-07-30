using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BlinkTalk.Application.Persistence;

namespace BlinkTalk.Application.Tests;

/// <summary>
/// Exercises the download of a language pack through a fake HTTP handler: the bytes must
/// round-trip untouched, progress must be a 0..1 fraction only when the server sends a length
/// (null — indeterminate — otherwise), and failure/cancellation must surface as exceptions
/// rather than a truncated pack.
/// </summary>
public class LanguagePackDownloaderTests
{
    [Fact]
    public async Task DownloadedBytesRoundTripAndSeedThroughInMemoryZipSource()
    {
        byte[] zip = MakeZip("Word,LanguageUsageCount\nHELLO,500\nWORLD,400\n");
        var downloader = new LanguagePackDownloader(ClientReturning(Response(zip, contentLength: zip.Length)));

        byte[] downloaded = await downloader.DownloadAsync(Language.English, new NullProgress(), CancellationToken.None);

        Assert.Equal(zip, downloaded);
        var words = new InMemoryZipSeedWordSource(downloaded).GetWords().Select(w => w.Word).ToArray();
        Assert.Equal(new[] { "HELLO", "WORLD" }, words);
    }

    [Fact]
    public async Task ProgressRisesToOneWhenContentLengthIsKnown()
    {
        byte[] payload = new byte[200_000]; // several read-buffer chunks
        var reported = new List<double?>();
        var downloader = new LanguagePackDownloader(ClientReturning(Response(payload, contentLength: payload.Length)));

        await downloader.DownloadAsync(Language.English, new ListProgress(reported), CancellationToken.None);

        Assert.NotEmpty(reported);
        Assert.All(reported, p => Assert.NotNull(p));
        Assert.All(reported, p => Assert.InRange(p!.Value, 0d, 1d));
        Assert.Equal(1d, reported.Last());
        Assert.Equal(reported.OrderBy(p => p), reported); // never goes backwards
    }

    [Fact]
    public async Task ProgressIsNullWhenContentLengthIsUnknown()
    {
        var response = Response(new byte[1000], contentLength: null);
        var reported = new List<double?>();
        var downloader = new LanguagePackDownloader(ClientReturning(response));

        await downloader.DownloadAsync(Language.English, new ListProgress(reported), CancellationToken.None);

        Assert.All(reported, Assert.Null);
    }

    [Fact]
    public async Task MissingPackThrowsRatherThanSeedingNothing()
    {
        // Which language does not matter — the handler answers 404 whatever is asked for, which is
        // what a pack that has not been uploaded yet looks like from here.
        var downloader = new LanguagePackDownloader(
            ClientReturning(new HttpResponseMessage(HttpStatusCode.NotFound)));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => downloader.DownloadAsync(Language.English, new NullProgress(), CancellationToken.None));
    }

    [Fact]
    public async Task CancellationSurfacesAsOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var downloader = new LanguagePackDownloader(
            ClientReturning(Response(new byte[1000], contentLength: 1000)));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => downloader.DownloadAsync(Language.English, new NullProgress(), cts.Token));
    }

    [Fact]
    public void RequestsThePackNamedAfterTheLanguageFromTheGitHubRepository()
    {
        Assert.Equal(
            "https://github.com/mrpmorris/BlinkTalk/raw/refs/heads/master/LanguagePacks/Portuguese.zip",
            string.Format(LanguagePackDownloader.UrlFormat, Language.Portuguese));
    }

    private static HttpResponseMessage Response(byte[] payload, long? contentLength)
    {
        var content = new ByteArrayContent(payload);
        content.Headers.ContentLength = contentLength;
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }

    private static HttpClient ClientReturning(HttpResponseMessage response) =>
        new HttpClient(new StubHandler(response));

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage Response;

        public StubHandler(HttpResponseMessage response) => Response = response;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Response);
        }
    }

    /// <summary>
    /// A synchronous IProgress — Progress&lt;T&gt; posts to a sync context, which xUnit does not
    /// pump, so reports would be lost or arrive after the assertion.
    /// </summary>
    private sealed class ListProgress : IProgress<double?>
    {
        private readonly List<double?> Reports;

        public ListProgress(List<double?> reports) => Reports = reports;

        public void Report(double? value) => Reports.Add(value);
    }

    private sealed class NullProgress : IProgress<double?>
    {
        public void Report(double? value) { }
    }

    private static byte[] MakeZip(string csv)
    {
        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var entry = archive.CreateEntry("Words.csv").Open();
            byte[] bytes = Encoding.UTF8.GetBytes(csv);
            entry.Write(bytes, 0, bytes.Length);
        }
        return buffer.ToArray();
    }
}
