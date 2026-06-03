using System.Collections.Generic;

namespace BlinkTalk.Application.Persistence
{
    /// <summary>
    /// Minimal stand-in for the System.Data.DataTable the original Unity SQLite plugin returned,
    /// so the prediction services (WordService/PhraseService) port almost verbatim.
    /// </summary>
    public sealed class DataTable
    {
        public IReadOnlyList<DataRow> Rows { get; }

        public DataTable(IReadOnlyList<DataRow> rows)
        {
            Rows = rows;
        }
    }

    public sealed class DataRow
    {
        private readonly IReadOnlyDictionary<string, object?> _values;

        public DataRow(IReadOnlyDictionary<string, object?> values)
        {
            _values = values;
        }

        public object? this[string column] => _values.TryGetValue(column, out var v) ? v : null;
    }
}
