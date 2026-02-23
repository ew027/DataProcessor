using System;
using System.Collections.Generic;

namespace DataProcessor.Engine.Interfaces
{
    public interface IColumn
    {
        string Name { get; }
        string? Description { get; set; }
        Type DataType { get; }
        ColumnType ColumnType { get; }
        bool IsCategorical { get; }
        int Count { get; }
        Dictionary<int, string>? Labels { get; }
        object GetValueAsObject(int index);
        void SetValueFromObject(int index, object? value);
        bool IsNull(int index);
        void AddRow();
        void RemoveRow(int index);
        void AddLabel(int value, string label);
        IColumn Clone(bool convertToDouble);
        void Reorder(int[] newOrder);
    }
}
