using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DataProcessor.Engine {

    // ── Non-generic Column shim (legacy, used only by DataArray) ────────────
    // Kept internal to this file so DataArray remains self-contained.
    internal class Column {
        private Dictionary<int, string>? _labels;

        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public bool IsCategorical { get; set; }
        public ColumnType Type { get; set; }
        public Dictionary<int, string>? Labels => _labels;

        public void AddLabel(int value, string label) {
            _labels ??= new Dictionary<int, string>();
            IsCategorical = true;
            _labels[value] = label;
        }

        public static Column Clone(Column colToClone, bool toDbl) {
            var col = new Column();
            col.Description = colToClone.Description;
            col.Name = colToClone.Name;
            if (!toDbl) {
                col.IsCategorical = colToClone.IsCategorical;
                if (colToClone._labels != null)
                    col._labels = new Dictionary<int, string>(colToClone._labels);
                col.Type = colToClone.Type;
            } else {
                col.Type = ColumnType.Double;
            }
            return col;
        }
    }

    // ── DataArray (legacy) ───────────────────────────────────────────────────
    [Obsolete("Use DataStore instead. DataArray will be removed in a future version.")]
    public class DataArray {
        public List<List<Object>> Data { get; set; }

        internal List<Column> Columns { get; set; }

        public Dictionary<string, int> ColumnPositions { get; set; }

        public int RowCount() {
            return Data[0].Count;
        }

        public DataArray() {
            Data = new List<List<object>>();
            Columns = new List<Column>();
            ColumnPositions = new Dictionary<string, int>();
        }

        public int AddIntColumn(string colName) {
            Column col = new Column();
            col.Name = colName;
            col.IsCategorical = false;
            col.Type = ColumnType.Int;
            Columns.Add(col);
            ColumnPositions.Add(col.Name, Columns.Count - 1);
            Data.Add(new List<object>());
            return Columns.Count - 1;
        }

        public int AddStringColumn(string colName) {
            Column col = new Column();
            col.Name = colName;
            col.IsCategorical = false;
            col.Type = ColumnType.String;
            Columns.Add(col);
            ColumnPositions.Add(col.Name, Columns.Count - 1);
            Data.Add(new List<object>());
            return Columns.Count - 1;
        }

        public int AddDoubleColumn(string colName) {
            Column col = new Column();
            col.Name = colName;
            col.IsCategorical = false;
            col.Type = ColumnType.Double;
            Columns.Add(col);
            ColumnPositions.Add(col.Name, Columns.Count - 1);
            Data.Add(new List<object>());
            return Columns.Count - 1;
        }

        public int AddIntColumnWithData(string colName, int data) {
            Column col = new Column();
            col.Name = colName;
            col.IsCategorical = false;
            col.Type = ColumnType.Int;
            Columns.Add(col);
            int colPos = Columns.Count - 1;
            ColumnPositions.Add(col.Name, colPos);
            Data.Add(new List<object>());
            int rowCount = RowCount();
            for (int i = 0; i < rowCount; i++) Data[colPos].Add(data);
            return colPos;
        }

        public int AddStringColumnWithData(string colName, string data) {
            Column col = new Column();
            col.Name = colName;
            col.IsCategorical = false;
            col.Type = ColumnType.String;
            Columns.Add(col);
            int colPos = Columns.Count - 1;
            ColumnPositions.Add(col.Name, colPos);
            Data.Add(new List<object>());
            int rowCount = RowCount();
            for (int i = 0; i < rowCount; i++) Data[colPos].Add(data);
            return colPos;
        }

        internal int AddColumn(Column col) {
            Columns.Add(col);
            ColumnPositions.Add(col.Name, Columns.Count - 1);
            Data.Add(new List<object>());
            return Columns.Count - 1;
        }

        public void RemoveColumn(string colName) {
            Data.RemoveAt(ColumnPositions[colName]);
            Columns.RemoveAt(ColumnPositions[colName]);
            ColumnPositions.Clear();
            for (int i = 0; i < Columns.Count; i++) ColumnPositions.Add(Columns[i].Name, i);
        }

        public void LoadDataRow(Dictionary<string, object> rowValues) {
            foreach (Column col in Columns) {
                if (rowValues.ContainsKey(col.Name)) {
                    this.Data[this.ColumnPositions[col.Name]].Add(rowValues[col.Name]);
                } else {
                    if (col.Type == ColumnType.Int)
                        this.Data[this.ColumnPositions[col.Name]].Add(Int32.MinValue);
                    else if (col.Type == ColumnType.Double)
                        this.Data[this.ColumnPositions[col.Name]].Add(Double.NaN);
                }
            }
        }

        public void Export(string filePath) {
            using var sw = new StreamWriter(filePath);
            sw.Write(CreateOutputData(true));
        }

        public void AppendToCsv(string filePath) {
            using var sw = File.AppendText(filePath);
            sw.Write(CreateOutputData(false));
        }

        private string CreateOutputData(bool includeHeaders) {
            var sb = new StringBuilder();
            if (includeHeaders) {
                string headerRow = "";
                foreach (var col in Columns) headerRow += col.Name + ",";
                sb.AppendLine(headerRow.TrimEnd(','));
            }
            int rowCount = RowCount();
            for (int i = 0; i < rowCount; i++) {
                string outputData = "";
                foreach (var col in Columns) {
                    switch (col.Type) {
                        case ColumnType.Int:
                            int ival = (int)Data[ColumnPositions[col.Name]][i];
                            outputData += (ival != Int32.MinValue) ? ival + "," : ",";
                            break;
                        case ColumnType.Double:
                            double dval = (double)Data[ColumnPositions[col.Name]][i];
                            outputData += (!Double.IsNaN(dval)) ? dval + "," : ",";
                            break;
                        case ColumnType.String:
                            string sval = (string)Data[ColumnPositions[col.Name]][i];
                            outputData += sval.Contains(',') ? $"\"{sval}\"," : sval + ",";
                            break;
                    }
                }
                sb.AppendLine(outputData.TrimEnd(','));
            }
            return sb.ToString();
        }

        public void ExportToSpss(string filePath) =>
            throw new NotImplementedException("SPSS export not yet implemented. Use DataStore instead.");
    }
}
