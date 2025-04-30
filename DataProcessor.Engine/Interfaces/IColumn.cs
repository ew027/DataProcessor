using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataProcessor.Engine.Interfaces
{
    public interface IColumn
    {
        string Name { get; }
        string Description { get; set; }
        Type DataType { get; }
        bool IsCategorical { get; }
        int Count { get; }
        object GetValueAsObject(int index);
        void SetValueFromObject(int index, object value);
        bool IsNull(int index);
        void AddRow();
        void RemoveRow(int index);
    }
}
