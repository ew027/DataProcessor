using DataProcessor.Engine.Interfaces;
using System;
using System.Collections.Generic;

namespace DataProcessor.Engine {
    public class Column<T> : IColumn {
        private readonly List<T> _values;
        private readonly string _name;
        private readonly bool _nullable;
        private Dictionary<int, string>? _labels;

        public Column(string name, bool nullable = true) {
            _name = name;
            _values = new List<T>();
            _nullable = nullable;
        }

        public string Name => _name;
        public Type DataType => typeof(T);
        public int Count => _values.Count;

        public ColumnType ColumnType =>
            typeof(T) == typeof(int) ? ColumnType.Int :
            typeof(T) == typeof(double) ? ColumnType.Double :
            ColumnType.String;

        public T GetValue(int index) => _values[index];

        public void SetValue(int index, T value) {
            if (index >= _values.Count)
                throw new IndexOutOfRangeException($"Index {index} is out of range.");
            if (!_nullable && EqualityComparer<T>.Default.Equals(value, default(T)))
                throw new ArgumentException("Null values are not allowed for this column.");
            _values[index] = value;
        }

        public object GetValueAsObject(int index) => GetValue(index)!;

        public void SetValueFromObject(int index, object? value) {
            if (value is T typedValue) {
                SetValue(index, typedValue);
            } else if (value == null || value == DBNull.Value) {
                SetValue(index, default(T)!);
            } else {
                try {
                    T convertedValue = (T)Convert.ChangeType(value, typeof(T));
                    SetValue(index, convertedValue);
                } catch {
                    throw new InvalidCastException(
                        $"Cannot convert value of type {value.GetType().Name} to {typeof(T).Name}");
                }
            }
        }

        public bool IsNull(int index) =>
            EqualityComparer<T>.Default.Equals(_values[index], default(T));

        public void AddRow() => _values.Add(default(T)!);

        public void RemoveRow(int index) => _values.RemoveAt(index);

        public void Reorder(int[] newOrder) {
            var reordered = new List<T>(newOrder.Length);
            foreach (var idx in newOrder) reordered.Add(_values[idx]);
            _values.Clear();
            _values.AddRange(reordered);
        }

        public string? Description { get; set; }
        public bool IsCategorical { get; set; }

        // Returns null when no labels have been set; callers should check IsCategorical first.
        public Dictionary<int, string>? Labels => _labels;

        public void AddLabel(int value, string label) {
            _labels ??= new Dictionary<int, string>();
            IsCategorical = true;
            _labels[value] = label;
        }

        public void RemoveLabel(int value) {
            _labels?.Remove(value);
        }

        public void SetLabels(Dictionary<int, string> labels) => _labels = labels;

        public IColumn Clone(bool convertToDouble) {
            if (convertToDouble) {
                var clone = new Column<double>(_name, _nullable);
                clone.Description = Description;
                return clone;
            } else {
                var clone = new Column<T>(_name, _nullable);
                clone.Description = Description;
                clone.IsCategorical = IsCategorical;
                if (_labels != null)
                    clone._labels = new Dictionary<int, string>(_labels);
                return clone;
            }
        }
    }

    public enum ColumnType : int {
        Int = 1,
        Double = 2,
        String = 3
    }
}
