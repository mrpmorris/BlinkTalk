using System;
using System.IO;
using BlinkTalk.Application.Persistence;
using Microsoft.Maui.Storage;

namespace BlinkTalk.Services;

/// <summary>
/// Copies the bundled, read-only database asset out to the writable app-data directory on
/// first run, then hands back that path. The shipped DB lives inside the app package (and is
/// read-only on Android), but the learned word-sequence tables must be writable. The bundled
/// asset is named after the language (e.g. "English.db"); the writable copy is always
/// "BlinkTalk.db".
/// </summary>
public sealed class MauiDatabaseProvisioner : IDatabaseProvisioner
{
    private const string WritableFileName = "BlinkTalk.db";

    public string EnsureDatabase(string languageName)
    {
        string sourceFileName = languageName + ".db";
        string targetDirectory = GetWritableDirectory();
        Directory.CreateDirectory(targetDirectory);
        string targetPath = Path.Combine(targetDirectory, WritableFileName);

        if (!File.Exists(targetPath))
        {
            using Stream source = FileSystem.Current.OpenAppPackageFileAsync(sourceFileName).GetAwaiter().GetResult();
            using FileStream destination = File.Create(targetPath);
            source.CopyTo(destination);
        }

        return targetPath;
    }

    /// <summary>
    /// The directory the writable database lives in. On Windows the MAUI default
    /// (<see cref="FileSystem.AppDataDirectory"/>) nests the file under a publisher folder taken
    /// from the package manifest and the application id (e.g. ...\Local\User Name\com.airsoftware...\Data).
    /// We instead use a clean per-user "BlinkTalk" folder under LocalApplicationData — Windows
    /// resolves LocalApplicationData per signed-in user, so each Windows account still gets its own
    /// database. This matches the WebView2 folder created in App.xaml.cs. Other platforms keep the
    /// platform default app-data directory.
    /// </summary>
    private static string GetWritableDirectory()
    {
#if WINDOWS
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BlinkTalk");
#else
        return FileSystem.Current.AppDataDirectory;
#endif
    }
}
