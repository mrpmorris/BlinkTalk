using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BlinkTalk.Application.Persistence;

/// <summary>
/// Downloads a language pack (a zipped word-list CSV, see <see cref="WordListZipReader"/>) from
/// the project's GitHub repository into memory. Progress is reported as a fraction 0..1, or null
/// when the server did not send a Content-Length (the UI shows an indeterminate bar then).
/// </summary>
public sealed class LanguagePackDownloader
{
    public const string UrlFormat = "https://github.com/mrpmorris/BlinkTalk/raw/refs/heads/master/LanguagePacks/{0}.zip";

    private readonly HttpClient HttpClient;

    public LanguagePackDownloader(HttpClient httpClient)
    {
        HttpClient = httpClient;
    }

    public async Task<byte[]> DownloadAsync(string languageName, IProgress<double?> progress, CancellationToken cancellationToken)
    {
        string url = string.Format(UrlFormat, languageName);

        // ResponseHeadersRead so bytes stream through the progress loop instead of being
        // buffered whole before we see them.
        using HttpResponseMessage response = await HttpClient.GetAsync(
            url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        long? totalBytes = response.Content.Headers.ContentLength;
        progress.Report(totalBytes.HasValue ? 0d : (double?)null);

        using Stream source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var destination = new MemoryStream(totalBytes.HasValue ? (int)totalBytes.Value : 0);

        byte[] buffer = new byte[81920];
        long received = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
        {
            destination.Write(buffer, 0, read);
            received += read;
            if (totalBytes.HasValue)
                progress.Report(Math.Min(1d, (double)received / totalBytes.Value));
        }

        return destination.ToArray();
    }
}
