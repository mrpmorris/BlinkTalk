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

    /// <summary>
    /// The first column of the first row, or null if the query returned no rows (or a NULL).
    /// Note SQLite returns INTEGER as <see cref="long"/>, so callers wanting an int should use
    /// <see cref="System.Convert.ToInt32(object)"/> as they do with <see cref="DataRow"/> columns.
    /// </summary>
    object? ExecuteScalar(string sql, params (string name, object? value)[] parameters);
}
