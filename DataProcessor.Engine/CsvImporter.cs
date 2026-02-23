using System;
using System.Collections.Generic;
using System.IO;

namespace DataProcessor.Engine
{
    public static class CsvImporter
    {
        /// <summary>
        /// Reads a CSV file into a DataStore. Column types are inferred per column:
        /// tries int first, then double, then falls back to string.
        /// Empty cells map to Int32.MinValue (int) or Double.NaN (double).
        /// </summary>
        public static DataStore FromCsv(string filePath, bool hasHeaders = true)
        {
            var lines = File.ReadAllLines(filePath);
            if (lines.Length == 0)
                return new DataStore();

            int dataStart = 0;
            List<string> headers;

            if (hasHeaders)
            {
                headers = ParseCsvLine(lines[0]);
                dataStart = 1;
            }
            else
            {
                // Generate column names Col1, Col2, ...
                int colCount = ParseCsvLine(lines[0]).Count;
                headers = new List<string>(colCount);
                for (int i = 0; i < colCount; i++) headers.Add($"Col{i + 1}");
            }

            int colC = headers.Count;

            // Read all data rows; skip only trailing empty lines at end-of-file.
            int lastNonEmpty = lines.Length - 1;
            while (lastNonEmpty >= dataStart && string.IsNullOrWhiteSpace(lines[lastNonEmpty]))
                lastNonEmpty--;

            var rawRows = new List<List<string>>();
            for (int i = dataStart; i <= lastNonEmpty; i++)
            {
                // Empty mid-file lines → treated as a row where all cells are empty (missing).
                rawRows.Add(string.IsNullOrEmpty(lines[i])
                    ? new List<string>(System.Linq.Enumerable.Repeat("", colC))
                    : ParseCsvLine(lines[i]));
            }

            // Infer column types by scanning all values in each column
            var colTypes = new ColumnType[colC];
            for (int c = 0; c < colC; c++)
                colTypes[c] = InferColumnType(rawRows, c);

            // Build DataStore
            var ds = new DataStore();

            for (int c = 0; c < colC; c++)
            {
                switch (colTypes[c])
                {
                    case ColumnType.Int:    ds.AddIntColumn(headers[c]);    break;
                    case ColumnType.Double: ds.AddDoubleColumn(headers[c]); break;
                    default:               ds.AddStringColumn(headers[c]); break;
                }
            }

            foreach (var row in rawRows)
            {
                int rowIdx = ds.AddRow();
                for (int c = 0; c < colC && c < row.Count; c++)
                {
                    string cell = row[c];
                    switch (colTypes[c])
                    {
                        case ColumnType.Int:
                            if (string.IsNullOrEmpty(cell))
                                ds.SetValueObject(rowIdx, headers[c], int.MinValue);
                            else if (int.TryParse(cell, out int iv))
                                ds.SetValueObject(rowIdx, headers[c], iv);
                            else
                                ds.SetValueObject(rowIdx, headers[c], int.MinValue);
                            break;
                        case ColumnType.Double:
                            if (string.IsNullOrEmpty(cell))
                                ds.SetValueObject(rowIdx, headers[c], double.NaN);
                            else if (double.TryParse(cell, out double dv))
                                ds.SetValueObject(rowIdx, headers[c], dv);
                            else
                                ds.SetValueObject(rowIdx, headers[c], double.NaN);
                            break;
                        default:
                            ds.SetValueObject(rowIdx, headers[c], cell);
                            break;
                    }
                }
            }

            return ds;
        }

        private static ColumnType InferColumnType(List<List<string>> rows, int colIndex)
        {
            bool canBeInt = true;
            bool canBeDouble = true;

            foreach (var row in rows)
            {
                if (colIndex >= row.Count) continue;
                string cell = row[colIndex];
                if (string.IsNullOrEmpty(cell)) continue; // missing → OK for any type

                if (canBeInt && !int.TryParse(cell, out _))
                    canBeInt = false;
                if (canBeDouble && !double.TryParse(cell, out _))
                    canBeDouble = false;

                if (!canBeDouble) break; // already worst case
            }

            if (canBeInt) return ColumnType.Int;
            if (canBeDouble) return ColumnType.Double;
            return ColumnType.String;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            int i = 0;
            while (i <= line.Length)
            {
                if (i == line.Length)
                {
                    result.Add("");
                    break;
                }

                if (line[i] == '"')
                {
                    // Quoted field
                    i++;
                    var sb = new System.Text.StringBuilder();
                    while (i < line.Length)
                    {
                        if (line[i] == '"')
                        {
                            i++;
                            if (i < line.Length && line[i] == '"')
                            {
                                sb.Append('"');
                                i++;
                            }
                            else break;
                        }
                        else
                        {
                            sb.Append(line[i++]);
                        }
                    }
                    result.Add(sb.ToString());
                    if (i < line.Length && line[i] == ',') i++;
                }
                else
                {
                    int comma = line.IndexOf(',', i);
                    if (comma < 0)
                    {
                        result.Add(line.Substring(i));
                        break;
                    }
                    result.Add(line.Substring(i, comma - i));
                    i = comma + 1;
                }
            }
            return result;
        }
    }
}
