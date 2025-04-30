using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

using Spss;

namespace DataProcessor.Engine {
    public class DataArray {
        public List<List<Object>> Data { get; set; }

        public List<Column> Columns { get; set; }

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

            // return new column position
            return Columns.Count - 1;
        }

        public int AddStringColumn(String colName) {
            Column col = new Column();
            col.Name = colName;
            col.IsCategorical = false;
            col.Type = ColumnType.String;

            Columns.Add(col);

            ColumnPositions.Add(col.Name, Columns.Count - 1);

            Data.Add(new List<object>());

            // return new column position
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

            // return new column position
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

            for (int i = 0; i < rowCount; i++) {
                Data[colPos].Add(data);
            }

            // return new column position
            return colPos;
        }

        public int AddStringColumnWithData(String colName, string data) {
            Column col = new Column();
            col.Name = colName;
            col.IsCategorical = false;
            col.Type = ColumnType.String;

            Columns.Add(col);

            int colPos = Columns.Count - 1;
            ColumnPositions.Add(col.Name, colPos);

            Data.Add(new List<object>());

            int rowCount = RowCount();

            for (int i = 0; i < rowCount; i++) {
                Data[colPos].Add(data);
            }

            // return new column position
            return colPos;
        }



        public int AddColumn(Column col) {
            Columns.Add(col);

            ColumnPositions.Add(col.Name, Columns.Count - 1);

            Data.Add(new List<object>());

            // return new column position
            return Columns.Count - 1;
        }

        public void RemoveColumn(string colName) {
            Data.RemoveAt(ColumnPositions[colName]);

            Columns.RemoveAt(ColumnPositions[colName]);

            // recreate the position dictionary
            ColumnPositions.Clear();

            for (int i = 0; i < Columns.Count; i++) {
                ColumnPositions.Add(Columns[i].Name, i);
            }
        }

        public void LoadDataRow(Dictionary<string, object> rowValues) {
            foreach (Column col in Columns) {
                if (rowValues.ContainsKey(col.Name)) {
                    this.Data[this.ColumnPositions[col.Name]].Add(rowValues[col.Name]);
                } else {
                    if (col.Type == ColumnType.Int) {
                        this.Data[this.ColumnPositions[col.Name]].Add(Int32.MinValue);
                    } else if (col.Type == ColumnType.Double) {
                        this.Data[this.ColumnPositions[col.Name]].Add(Double.NaN);
                    }
                }
            }
        }

        /// <summary>
        /// Exports a DataArray to a CSV file
        /// </summary>
        /// <param name="filePath">The full path to save the CSV file to</param>
        public void Export(string filePath) {
            StreamWriter sw = new StreamWriter(filePath);

            sw.Write(CreateOutputData(true));

            sw.Flush();
            sw.Close();
        }

        /// <summary>
        /// Appends a DataArray to an existing CSV file. The caller is required to determine whether the CSV data structure is suitable for appending to.
        /// </summary>
        /// <param name="filePath">The full path of the existing CSV file</param>
        public void AppendToCsv(string filePath) {
            StreamWriter sw = File.AppendText(filePath);

            sw.Write(CreateOutputData(false));

            sw.Flush();
            sw.Close();
        }

        private string CreateOutputData(bool includeHeaders) {
            StringBuilder sb = new StringBuilder();

            if (includeHeaders) {
                string headerRow = "";

                foreach (var col in Columns) {
                    headerRow += col.Name + ",";
                }

                sb.AppendLine(headerRow.TrimEnd(','));
            }

            int rowCount = RowCount();

            for (int i = 0; i < rowCount; i++) {
                string outputData = "";

                foreach (var col in Columns) {
                    switch (col.Type) {
                        case ColumnType.Int:
                            int ival = (int)Data[ColumnPositions[col.Name]][i];
                            if (ival != Int32.MinValue) {
                                outputData += ival + ",";
                            } else {
                                outputData += ",";
                            }
                            break;
                        case ColumnType.Double:
                            double dval = (double)Data[ColumnPositions[col.Name]][i];
                            if (!Double.IsNaN(dval)) {
                                outputData += dval + ",";
                            } else {
                                outputData += ",";
                            }
                            break;
                        case ColumnType.String:
                            string sval = (string)Data[ColumnPositions[col.Name]][i];
                            if (sval.Contains(',')) {
                                outputData += "\"" + sval + "\",";
                            } else {
                                outputData += sval + ",";
                            }
                            break;
                    }
                }

                sb.AppendLine(outputData.TrimEnd(','));
            }

            return sb.ToString();
        }

        private int GetMaxStringLength(string varname) {
            int maxLength = 1;
            int colPos = this.ColumnPositions[varname];
            int rowcount = this.RowCount();

            for (int i = 0; i < rowcount; i++) {
                if (((string)this.Data[colPos][i]).Length > maxLength) {
                    maxLength = ((string)this.Data[colPos][i]).Length;
                }
            }

            return maxLength;
        }

        public void ExportToSpss(string filePath) {
            // delete the file if it already exists
            if (File.Exists(filePath)) {
                File.Delete(filePath);
            }

            using (SpssDataDocument doc = SpssDataDocument.Create(filePath)) {
                doc.IsCompressed = true;

                // add the metadata
                foreach (var col in this.Columns) {
                    if (col.Type == ColumnType.Int) {
                        SpssNumericVariable numVar = new SpssNumericVariable();
                        numVar.Name = col.Name;
                        numVar.Label = string.IsNullOrEmpty(col.Description) ? "" : col.Description;

                        if (col.IsCategorical && col.Labels != null) {
                            foreach (var label in col.Labels) {
                                numVar.ValueLabels.Add(label.Key, label.Value);
                            }
                        }

                        numVar.WriteWidth = 8;
                        numVar.PrintWidth = 8;
                        numVar.WriteDecimal = 0;
                        numVar.PrintDecimal = 0;

                        numVar.ColumnWidth = 8;

                        doc.Variables.Add(numVar);
                    } else if (col.Type == ColumnType.Double) {
                        SpssNumericVariable numVar = new SpssNumericVariable();
                        numVar.Name = col.Name;
                        numVar.Label = string.IsNullOrEmpty(col.Description) ? "" : col.Description;

                        numVar.WriteWidth = 8;
                        numVar.PrintWidth = 8;
                        numVar.WriteDecimal = 4;
                        numVar.PrintDecimal = 4;

                        numVar.ColumnWidth = 8;

                        doc.Variables.Add(numVar);
                    } else if (col.Type == ColumnType.String) {
                        SpssStringVariable strVar = new SpssStringVariable();
                        strVar.Name = col.Name;
                        strVar.Label = string.IsNullOrEmpty(col.Description) ? "" : col.Description;

                        strVar.Length = this.GetMaxStringLength(col.Name);
                        //Console.WriteLine(col.Name + " - " + strVar.Length);

                        //strVar.ColumnWidth = 8;

                        doc.Variables.Add(strVar);
                    }
                }

                doc.CommitDictionary();

                int rowCount = RowCount();

                for (int i = 0; i < rowCount; i++) {
                    SpssCase row = doc.Cases.New();

                    foreach (var col in Columns) {
                        switch (col.Type) {
                            case ColumnType.Int:
                                int ival = (int)Data[ColumnPositions[col.Name]][i];
                                if (ival != Int32.MinValue) {
                                    row[col.Name] = ival;
                                } 
                                break;
                            case ColumnType.Double:
                                double dval = (double)Data[ColumnPositions[col.Name]][i];
                                if (!Double.IsNaN(dval)) {
                                    row[col.Name] = dval;
                                } 
                                break;
                            case ColumnType.String:
                                string sval = (string)Data[ColumnPositions[col.Name]][i];
                                row[col.Name] = sval;
                                break;
                        }
                    }

                    row.Commit();
                }
            }
        }
    }
}
