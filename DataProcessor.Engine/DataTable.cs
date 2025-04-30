using DataProcessor.Engine.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataProcessor.Engine
{
    public class DataTable
    {
        private readonly Dictionary<string, IColumn> _columns;
        private int _rowCount;

        public DataTable()
        {
            _columns = new Dictionary<string, IColumn>(StringComparer.OrdinalIgnoreCase);
            _rowCount = 0;
        }

        public IReadOnlyDictionary<string, IColumn> Columns => _columns;
        public int RowCount => _rowCount;

        // Add a new column of a specified type
        public Column<T> AddColumn<T>(string columnName, bool nullable = true)
        {
            if (_columns.ContainsKey(columnName))
            {
                throw new ArgumentException($"Column '{columnName}' already exists.");
            }

            var column = new Column<T>(columnName, nullable);

            // Add default values for existing rows
            for (int i = 0; i < _rowCount; i++)
            {
                column.AddRow();
            }

            _columns.Add(columnName, column);
            return column;
        }

        // Get a strongly-typed column
        public Column<T> GetColumn<T>(string columnName)
        {
            if (!_columns.TryGetValue(columnName, out var column))
            {
                throw new KeyNotFoundException($"Column '{columnName}' not found.");
            }

            if (column is Column<T> typedColumn)
            {
                return typedColumn;
            }

            throw new InvalidCastException($"Column '{columnName}' is of type {column.DataType.Name}, not {typeof(T).Name}.");
        }

        // Add a new row
        public int AddRow()
        {
            foreach (var column in _columns.Values)
            {
                column.AddRow();
            }

            return _rowCount++;
        }

        // Remove a row
        public void RemoveRow(int index)
        {
            if (index < 0 || index >= _rowCount)
            {
                throw new IndexOutOfRangeException($"Row index {index} is out of range.");
            }

            foreach (var column in _columns.Values)
            {
                column.RemoveRow(index);
            }

            _rowCount--;
        }

        // Strongly-typed getter
        public T GetValue<T>(int rowIndex, string columnName)
        {
            return GetColumn<T>(columnName).GetValue(rowIndex);
        }

        // Strongly-typed setter
        public void SetValue<T>(int rowIndex, string columnName, T value)
        {
            GetColumn<T>(columnName).SetValue(rowIndex, value);
        }

        // Get row as dictionary
        public Dictionary<string, object> GetRowAsDictionary(int rowIndex)
        {
            var result = new Dictionary<string, object>();

            foreach (var column in _columns)
            {
                result.Add(column.Key, column.Value.GetValueAsObject(rowIndex));
            }

            return result;
        }
    }
}
