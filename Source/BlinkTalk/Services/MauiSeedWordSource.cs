using BlinkTalk.Application.Persistence;

namespace BlinkTalk.Services;

/// <summary>
/// Reads the bundled word-list asset (a zipped CSV in Resources/Raw) to seed the Words dictionary
/// when AutoMigratingDatabase first creates the database. The asset is named after
/// <see cref="AppLanguage.Name"/>, so a French UI seeds from "French.zip".
/// </summary>
public sealed class MauiSeedWordSource : ISeedWordSource
{
    public IEnumerable<(string Word, int LanguageUsageCount)> GetWords()
    {
        string assetName = AppLanguage.Name + ".zip";

        // The non-English word lists aren't all bundled yet; fall back rather than failing
        // startup, since without a dictionary the person cannot type at all.
        if (!AssetExists(assetName))
            assetName = AppLanguage.Fallback + ".zip";

        using Stream zip = FileSystem.Current.OpenAppPackageFileAsync(assetName).GetAwaiter().GetResult();
        foreach (var word in WordListZipReader.Read(zip))
            yield return word;
    }

    /// <summary>
    /// Asks MAUI whether the asset is bundled, rather than opening it and catching: the platforms
    /// don't agree on the exception type for a missing package file.
    /// </summary>
    private static bool AssetExists(string assetName) =>
        FileSystem.Current.AppPackageFileExistsAsync(assetName).GetAwaiter().GetResult();
}
