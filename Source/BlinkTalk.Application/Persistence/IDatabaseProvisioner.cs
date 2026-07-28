namespace BlinkTalk.Application.Persistence;

/// <summary>
/// Resolves the writable location of the database file (creating the directory if needed).
/// The database itself is created and seeded by <see cref="AutoMigratingDatabase"/> — nothing
/// is copied out of the app package. Implemented in the app using MAUI FileSystem APIs.
/// </summary>
public interface IDatabaseProvisioner
{
    /// <returns>The absolute path the writable database file should live at.</returns>
    string GetDatabasePath();
}
