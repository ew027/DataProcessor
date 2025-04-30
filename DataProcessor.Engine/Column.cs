using DataProcessor.Engine.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataProcessor.Engine {
    public class Column<T> : IColumn {
        private readonly List<T> _values;
        private readonly string _name;
        private readonly bool _nullable;

        private Dictionary<int, string> _labels;

        public Column(string name, bool nullable = true)
        {
            _name = name;
            _values = new List<T>();
            _nullable = nullable;
        }

        public string Name => _name;
        public Type DataType => typeof(T);
        public int Count => _values.Count;

        public T GetValue(int index)
        {
            return _values[index];
        }

        public void SetValue(int index, T value)
        {
            if (index >= _values.Count)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range.");
            }

            if (!_nullable && EqualityComparer<T>.Default.Equals(value, default(T)))
            {
                throw new ArgumentException("Null values are not allowed for this column.");
            }

            _values[index] = value;
        }

        public object GetValueAsObject(int index)
        {
            return GetValue(index);
        }

        public void SetValueFromObject(int index, object value)
        {
            if (value is T typedValue)
            {
                SetValue(index, typedValue);
            }
            else if (value == null || value == DBNull.Value)
            {
                SetValue(index, default(T));
            }
            else
            {
                // Try to convert
                try
                {
                    T convertedValue = (T)Convert.ChangeType(value, typeof(T));
                    SetValue(index, convertedValue);
                }
                catch
                {
                    throw new InvalidCastException($"Cannot convert value of type {value.GetType().Name} to {typeof(T).Name}");
                }
            }
        }

        public bool IsNull(int index)
        {
            return EqualityComparer<T>.Default.Equals(_values[index], default(T));
        }

        public void AddRow()
        {
            _values.Add(default(T));
        }

        public void RemoveRow(int index)
        {
            _values.RemoveAt(index);
        }
        
        public string Description { get; set; }

        //public List<Label> Labels { get; set; }

        public bool IsCategorical { get; set; }
        public Dictionary<int, string> Labels { get; }

        //public static Column Clone(Column colToClone, bool toDbl) {
        //    Column col = new Column();
        //    col.Description = colToClone.Description;
        //    col.Name = colToClone.Name;
        //    if (!toDbl) {
        //        col.IsCategorical = colToClone.IsCategorical;
        //        col.Labels = colToClone.Labels;
        //        col.Type = colToClone.Type;
        //    } else {
        //        col.Type = ColumnType.Double;
        //    }

        //    return col;
        //}

        public void AddLabel(int value, string label) {
            if (_labels == null) {
                _labels = new Dictionary<int, string>();
            }

            this.IsCategorical = true;
            this.Labels.Add(value, label);
        }

        public void RemoveLabel(int value)
        {
            if (_labels != null)
            {
                this.Labels.Remove(value);
            }
        }

        public void SetLabels(Dictionary<int, string> labels)
        {
            _labels = labels;
        }
    }

    public enum ColumnType : int {
        Int = 1,
        Double = 2,
        String = 3
    }
}
