namespace BlinkTalk.Application.Persistence;

/// <summary>
/// Ensures the bundled, read-only database asset has been copied to a writable location
/// and returns that path. On Android (and packaged apps generally) the shipped DB lives
/// inside the app package and cannot be written to, so the n-gram tables must be copied
/// out to app data on first run. Implemented in the app using MAUI FileSystem APIs.
/// </summary>
public interface IDatabaseProvisioner
{
    /// <param name="languageName">e.g. "English" → "English.db".</param>
    /// <returns>The absolute path to the writable database file.</returns>
    string EnsureDatabase(string languageName);
}
