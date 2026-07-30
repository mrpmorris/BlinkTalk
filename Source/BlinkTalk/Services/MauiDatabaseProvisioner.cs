using BlinkTalk.Application.Persistence;

namespace BlinkTalk.Services;

/// <summary>
/// Resolves the writable path for the database and ensures the directory exists. The file itself
/// is created and seeded by AutoMigratingDatabase. The name carries the language
/// (e.g. "BlinkTalk-English.db") so each language keeps its own dictionary and learned n-grams,
/// matching the word list it was seeded from.
/// </summary>
public sealed class MauiDatabaseProvisioner : IDatabaseProvisioner
{
    public bool DatabaseExists() =>
        File.Exists(Path.Combine(GetWritableDirectory(), $"BlinkTalk-{AppLanguage.Name}.db"));

    public string GetDatabasePath()
    {
        string targetDirectory = GetWritableDirectory();
        Directory.CreateDirectory(targetDirectory);
        return Path.Combine(targetDirectory, $"BlinkTalk-{AppLanguage.Name}.db");
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
