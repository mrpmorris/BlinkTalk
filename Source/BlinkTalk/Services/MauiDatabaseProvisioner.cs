using System.IO;
using BlinkTalk.Application.Persistence;
using Microsoft.Maui.Storage;

namespace BlinkTalk.Services
{
    /// <summary>
    /// Copies the bundled, read-only database asset out to the writable app-data directory on
    /// first run, then hands back that path. The shipped DB lives inside the app package (and is
    /// read-only on Android), but the learned word-sequence tables must be writable.
    /// </summary>
    public sealed class MauiDatabaseProvisioner : IDatabaseProvisioner
    {
        public string EnsureDatabase(string languageName)
        {
            string fileName = languageName + ".db";
            string targetPath = Path.Combine(FileSystem.Current.AppDataDirectory, fileName);

            if (!File.Exists(targetPath))
            {
                using Stream source = FileSystem.Current.OpenAppPackageFileAsync(fileName).GetAwaiter().GetResult();
                using FileStream destination = File.Create(targetPath);
                source.CopyTo(destination);
            }

            return targetPath;
        }
    }
}
