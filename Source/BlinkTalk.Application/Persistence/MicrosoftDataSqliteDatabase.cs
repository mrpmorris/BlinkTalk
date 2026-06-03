using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace BlinkTalk.Application.Persistence
{
    /// <summary>
    /// ISqliteDatabase backed by Microsoft.Data.Sqlite (ADO.NET). Holds a single open
    /// connection to the writable database file. Cross-platform via SQLitePCLRaw, which the
    /// Microsoft.Data.Sqlite package brings in for Android/iOS/MacCatalyst/Windows.
    /// </summary>
    public sealed class MicrosoftDataSqliteDatabase : ISqliteDatabase, IDisposable
    {
        private readonly SqliteConnection _connection;

        public MicrosoftDataSqliteDatabase(string databaseFilePath)
        {
            _connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = databaseFilePath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                // We hold a single long-lived connection, so pooling buys nothing and would keep
                // the file handle open after Dispose (which breaks file cleanup, e.g. in tests).
                Pooling = false
            }.ToString());
            _connection.Open();
        }

        public DataTable ExecuteQuery(string sql, params (string name, object? value)[] parameters)
        {
            using var command = CreateCommand(sql, parameters);
            using var reader = command.ExecuteReader();

            var rows = new List<DataRow>();
            while (reader.Read())
            {
                var values = new Dictionary<string, object?>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < reader.FieldCount; i++)
                    values[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                rows.Add(new DataRow(values));
            }
            return new DataTable(rows);
        }

        public int ExecuteNonQuery(string sql, params (string name, object? value)[] parameters)
        {
            using var command = CreateCommand(sql, parameters);
            return command.ExecuteNonQuery();
        }

        private SqliteCommand CreateCommand(string sql, (string name, object? value)[] parameters)
        {
            var command = _connection.CreateCommand();
            command.CommandText = sql;
            if (parameters != null)
            {
                foreach (var (name, value) in parameters)
                    command.Parameters.AddWithValue(name, value ?? DBNull.Value);
            }
            return command;
        }

        public void Dispose() => _connection.Dispose();
    }
}
