using System.IO;
using BlinkTalk.Application.Persistence;
using Microsoft.Maui.Storage;

namespace BlinkTalk.Services
{
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
            string targetPath = Path.Combine(FileSystem.Current.AppDataDirectory, WritableFileName);

            if (!File.Exists(targetPath))
            {
                using Stream source = FileSystem.Current.OpenAppPackageFileAsync(sourceFileName).GetAwaiter().GetResult();
                using FileStream destination = File.Create(targetPath);
                source.CopyTo(destination);
            }

            return targetPath;
        }
    }
}
