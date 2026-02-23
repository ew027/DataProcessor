using DataProcessor.Engine.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DataProcessor.Engine
{
    /// <summary>
    /// Strongly-typed columnar data store. Replaces the legacy DataArray class.
    /// Dictionary preserves insertion order in .NET 7+ so iteration order matches add order.
    /// </summary>
    public class DataStore
    {
        private readonly Dictionary<string, IColumn> _columns;
        private readonly List<string> _columnNames;  // authoritative insertion-order list
        private int _rowCount;

        public DataStore()
        {
            _columns = new Dictionary<string, IColumn>(StringComparer.OrdinalIgnoreCase);
            _columnNames = new List<string>();
            _rowCount = 0;
        }

        // ── Basic properties ────────────────────────────────────────────────

        public int RowCount => _rowCount;
        public IReadOnlyList<string> ColumnNames => _columnNames;
        public IReadOnlyDictionary<string, IColumn> Columns => _columns;

        // ── Column existence / access ────────────────────────────────────────

        public bool HasColumn(string name) => _columns.ContainsKey(name);

        /// <summary>Returns the IColumn interface for type inspection. Throws if not found.</summary>
        public IColumn GetColumn(string name)
        {
            if (!_columns.TryGetValue(name, out var col))
                throw new KeyNotFoundException($"Column '{name}' not found.");
            return col;
        }

        /// <summary>Returns the strongly-typed column. Throws if not found or wrong type.</summary>
        public Column<T> GetColumn<T>(string columnName)
        {
            if (!_columns.TryGetValue(columnName, out var column))
                throw new KeyNotFoundException($"Column '{columnName}' not found.");
            if (column is Column<T> typed)
                return typed;
            throw new InvalidCastException(
                $"Column '{columnName}' is of type {column.DataType.Name}, not {typeof(T).Name}.");
        }

        // ── Typed value access ───────────────────────────────────────────────

        public T GetValue<T>(int rowIndex, string columnName) =>
            GetColumn<T>(columnName).GetValue(rowIndex);

        public void SetValue<T>(int rowIndex, string columnName, T value) =>
            GetColumn<T>(columnName).SetValue(rowIndex, value);

        public object GetValueObject(int row, string name) =>
            _columns.TryGetValue(name, out var col)
                ? col.GetValueAsObject(row)
                : throw new KeyNotFoundException($"Column '{name}' not found.");

        public void SetValueObject(int row, string name, object? value) {
            if (!_columns.TryGetValue(name, out var col))
                throw new KeyNotFoundException($"Column '{name}' not found.");
            col.SetValueFromObject(row, value);
        }

        // ── Generic column add ───────────────────────────────────────────────

        public Column<T> AddColumn<T>(string columnName, bool nullable = true)
        {
            if (_columns.ContainsKey(columnName))
                throw new ArgumentException($"Column '{columnName}' already exists.");
            var column = new Column<T>(columnName, nullable);
            for (int i = 0; i < _rowCount; i++) column.AddRow();
            _columns.Add(columnName, column);
            _columnNames.Add(columnName);
            return column;
        }

        // ── Typed column add helpers ─────────────────────────────────────────

        public Column<int> AddIntColumn(string name) => AddColumn<int>(name);
        public Column<double> AddDoubleColumn(string name) => AddColumn<double>(name);
        public Column<string> AddStringColumn(string name) => AddColumn<string>(name);

        /// <summary>Adds an int column and fills all existing rows with <paramref name="value"/>.</summary>
        public Column<int> AddIntColumnWithData(string name, int value)
        {
            var col = AddIntColumn(name);
            for (int i = 0; i < _rowCount; i++) col.SetValue(i, value);
            return col;
        }

        // ── Row operations ────────────────────────────────────────────────────

        /// <summary>Appends one default row to every column. Returns the index of the new row.</summary>
        public int AddRow()
        {
            foreach (var col in _columns.Values) col.AddRow();
            return _rowCount++;
        }

        public void RemoveRow(int index)
        {
            if (index < 0 || index >= _rowCount)
                throw new IndexOutOfRangeException($"Row index {index} is out of range.");
            foreach (var col in _columns.Values) col.RemoveRow(index);
            _rowCount--;
        }

        /// <summary>Appends one row, setting values from the dictionary. Missing columns get defaults.</summary>
        public int LoadRow(Dictionary<string, object> rowValues)
        {
            int idx = AddRow();
            foreach (var name in _columnNames)
            {
                if (rowValues.TryGetValue(name, out var val))
                    _columns[name].SetValueFromObject(idx, val);
                // else leave at default (0 / NaN / null already set by AddRow)
            }
            return idx;
        }

        // ── Structural operations ─────────────────────────────────────────────

        public void RenameColumn(string oldName, string newName)
        {
            if (!_columns.TryGetValue(oldName, out var col))
                throw new KeyNotFoundException($"Column '{oldName}' not found.");
            if (_columns.ContainsKey(newName))
                throw new ArgumentException($"Column '{newName}' already exists.");
            _columns.Remove(oldName);
            _columns[newName] = col;
            int idx = _columnNames.IndexOf(oldName);
            if (idx >= 0) _columnNames[idx] = newName;
        }

        /// <summary>
        /// Returns a new DataStore containing only the specified columns.
        /// Column data is shared by reference (same semantics as the old DataArray.KeepVars).
        /// </summary>
        public DataStore Extract(IEnumerable<string> columnNames)
        {
            var result = new DataStore();
            result._rowCount = _rowCount;
            foreach (var name in columnNames)
            {
                if (!_columns.TryGetValue(name, out var col))
                    throw new KeyNotFoundException($"Column '{name}' not found.");
                result._columns[name] = col;
                result._columnNames.Add(name);
            }
            return result;
        }

        /// <summary>Reorders all rows according to newOrder (index array).</summary>
        public void ReorderRows(int[] newOrder)
        {
            foreach (var col in _columns.Values) col.Reorder(newOrder);
        }

        /// <summary>Returns the zero-based insertion-order index of a column, or -1 if not found.</summary>
        public int GetColumnIndex(string name) => _columnNames.IndexOf(name);

        // ── Row as dictionary ────────────────────────────────────────────────

        public Dictionary<string, object> GetRowAsDictionary(int rowIndex)
        {
            var result = new Dictionary<string, object>();
            foreach (var name in _columnNames)
                result[name] = _columns[name].GetValueAsObject(rowIndex);
            return result;
        }

        // ── CSV export ────────────────────────────────────────────────────────

        public void Export(string filePath)
        {
            using var sw = new StreamWriter(filePath);
            sw.Write(BuildCsvContent(true));
        }

        public void AppendToCsv(string filePath)
        {
            using var sw = File.AppendText(filePath);
            sw.Write(BuildCsvContent(false));
        }

        private string BuildCsvContent(bool includeHeaders)
        {
            var sb = new StringBuilder();
            if (includeHeaders)
                sb.AppendLine(string.Join(",", _columnNames));

            for (int i = 0; i < _rowCount; i++)
            {
                var parts = new List<string>(_columnNames.Count);
                foreach (var name in _columnNames)
                {
                    var col = _columns[name];
                    object raw = col.GetValueAsObject(i);
                    parts.Add(FormatCsvValue(col.ColumnType, raw));
                }
                sb.AppendLine(string.Join(",", parts));
            }
            return sb.ToString();
        }

        private static string FormatCsvValue(ColumnType type, object raw)
        {
            switch (type)
            {
                case ColumnType.Int:
                    int iv = (int)raw;
                    return iv == int.MinValue ? "" : iv.ToString();
                case ColumnType.Double:
                    double dv = (double)raw;
                    return double.IsNaN(dv) ? "" : dv.ToString();
                case ColumnType.String:
                    string sv = (string)raw ?? "";
                    return sv.Contains(',') ? $"\"{sv}\"" : sv;
                default:
                    return raw?.ToString() ?? "";
            }
        }

        public void ExportToSpss(string filePath) =>
            throw new NotImplementedException("SPSS export not yet implemented.");
    }
}
