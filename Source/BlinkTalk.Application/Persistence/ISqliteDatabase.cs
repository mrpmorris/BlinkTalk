namespace BlinkTalk.Application.Persistence;

/// <summary>
/// A thin SQL-executing abstraction over the SQLite database. Parameters are passed
/// separately from the SQL text so user-entered words can be bound safely (the original
/// project interpolated them into the SQL, which broke on apostrophes and was injectable).
/// </summary>
public interface ISqliteDatabase
{
    DataTable ExecuteQuery(string sql, params (string name, object? value)[] parameters);
    int ExecuteNonQuery(string sql, params (string name, object? value)[] parameters);
}
