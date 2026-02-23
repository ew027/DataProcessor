using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Meta.Numerics.Statistics.Distributions;

using DataProcessor.Engine.Operations;

namespace DataProcessor.Engine {
    public class Engine {
        private Dictionary<string, DataStore> m_DataArrays;
        private Dictionary<string, List<object>> m_Operations;
        private List<string> m_LogEntries;
        private Dictionary<string, string> m_fileHandles;
        private ErrorStatus m_errorStatus;

        public Engine() {
            m_DataArrays = new Dictionary<string, DataStore>();
            m_Operations = new Dictionary<string, List<object>>();
            m_LogEntries = new List<string>();
            m_fileHandles = new Dictionary<string, string>();
            m_errorStatus = new ErrorStatus();
        }

        // ── Public data access ────────────────────────────────────────────────

        public DataStore GetDataStore(string name) => m_DataArrays[name];

        public IReadOnlyList<string> GetDataStoreNames() =>
            m_DataArrays.Keys.ToList();

        public void AddDataStore(DataStore ds, string name) =>
            m_DataArrays[name] = ds;

        // Legacy compatibility wrappers
        public DataStore GetDataArray(string dataName) => m_DataArrays[dataName];
        public void AddDataArray(DataStore da, string dataName) =>
            m_DataArrays[dataName] = da;

        public void ExportDataArray(string dataName, string filePath) {
            m_DataArrays[dataName].Export(filePath);
            m_LogEntries.Add($"Dataset \"{dataName}\" exported to {filePath}\n");
        }

        public void AppendDataArrayToCsv(string dataName, string filePath) {
            m_DataArrays[dataName].AppendToCsv(filePath);
            m_LogEntries.Add($"Dataset \"{dataName}\" appended to {filePath}\n");
        }

        // ── Script loading ────────────────────────────────────────────────────

        public void LoadScript(string filePath, string operationName) {
            using var sr = new StreamReader(filePath);
            string? command;
            while ((command = sr.ReadLine()) != null)
                AddOperation(command, operationName);
        }

        public void AddOperation(string command, string operationName) {
            if (string.IsNullOrEmpty(command)) return;

            if (command[0] == '#')
                command = "Comment|" + command;

            string[] cmdArray = command.Split(new char[] { '|' }, 2);
            if (cmdArray.Length < 2) return;

            string cmdName = "DataProcessor.Engine.Operations." + cmdArray[0] + "Operation";
            string cmdParams = cmdArray[1];

            if (!m_Operations.ContainsKey(operationName))
                m_Operations.Add(operationName, new List<object>());

            Type? opType = Type.GetType(cmdName);
            if (opType == null) {
                m_LogEntries.Add($"Unknown operation: {cmdArray[0]}");
                return;
            }

            try {
                var instance = Activator.CreateInstance(opType, new object[] { cmdParams });
                if (instance != null) m_Operations[operationName].Add(instance);
            } catch (Exception ex) {
                m_errorStatus.CaughtException = ex;
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = command;
                m_LogEntries.Add($"Error occurred parsing command: {command}");
            }
        }

        public void LoadScriptSpss(string scriptText, string operationName) {
            var parser = new ScriptParser();
            var ops = parser.ParseScript(scriptText);
            if (!m_Operations.ContainsKey(operationName))
                m_Operations[operationName] = new List<object>();
            m_Operations[operationName].AddRange(ops);
        }

        public void ClearOperations(string operationName) {
            if (m_Operations.ContainsKey(operationName))
                m_Operations[operationName].Clear();
        }

        public void Execute(string operationName) {
            m_LogEntries.Add($"Starting execution of \"{operationName}\" on {DateTime.Now.ToUniversalTime()}\n\n");
            m_errorStatus.ErrorLogged = false;

            foreach (var op in m_Operations[operationName]) {
                Type thisType = this.GetType();
                Operation opName = (Operation)op;
                string result = (string)thisType.InvokeMember(
                    opName.GetExecuteMethod(),
                    BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance,
                    null, this, new object[] { op })!;
                m_LogEntries.Add(result);
            }
        }

        public List<string> GetLogEntries() => m_LogEntries;

        public void SaveLogEntries(string filePath) {
            using var sw = new StreamWriter(filePath);
            sw.WriteLine("Data processing log generated on " + DateTime.Now.ToUniversalTime());
            sw.WriteLine();
            foreach (var le in m_LogEntries) sw.WriteLine(le);
        }

        public bool CheckErrorStatus() => m_errorStatus.ErrorLogged;
        public ErrorStatus GetErrorStatus() => m_errorStatus;

        public void AddFileHandle(string handle, string filePath) =>
            m_fileHandles.Add(handle, filePath);

        // ── Execute* methods ──────────────────────────────────────────────────

        public string ExecuteAboveBelow(object op) {
            AboveBelowOperation abo = (AboveBelowOperation)op;
            string commandOutput = $"Executing AboveBelow command: {abo.OriginalCommandString}\n";
            try {
                DataStore d = m_DataArrays[abo.TargetDataArray];
                int rowCount = d.RowCount;

                if (!d.HasColumn(abo.TargetVar))
                    throw new Exception($"Variable to check doesn't exist: {abo.TargetVar}");

                d.AddIntColumnWithData(abo.AboveVar, 0);
                d.AddIntColumnWithData(abo.BelowVar, 0);

                for (int i = 0; i < rowCount; i++) {
                    double val = (double)d.GetValueObject(i, abo.TargetVar);
                    if (val >= 100)
                        d.SetValueObject(i, abo.AboveVar, 100);
                    else
                        d.SetValueObject(i, abo.BelowVar, 100);
                }

                commandOutput += $"Variable {abo.TargetVar} recoded into above/below variables\n";
            } catch (Exception ex) {
                commandOutput += $"Exception occurred: {ex.Message}\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }
            return commandOutput;
        }

        public string ExecuteAggregate(object op) {
            AggregateOperation ao = (AggregateOperation)op;
            string commandOutput = $"Executing Aggregate command: {ao.OriginalCommandString}\n";
            try {
                DataStore d = m_DataArrays[ao.TargetDataArray];
                DataStore nd = new DataStore();

                // Add keep columns to nd (same type, copy metadata)
                foreach (var kc in ao.KeepColumns) {
                    var srcCol = d.GetColumn(kc);
                    switch (srcCol.ColumnType) {
                        case ColumnType.Int: {
                            var col = nd.AddIntColumn(kc);
                            col.Description = srcCol.Description;
                            col.IsCategorical = srcCol.IsCategorical;
                            if (srcCol.Labels != null)
                                foreach (var lbl in srcCol.Labels) col.AddLabel(lbl.Key, lbl.Value);
                            break;
                        }
                        case ColumnType.Double: {
                            var col = nd.AddDoubleColumn(kc);
                            col.Description = srcCol.Description;
                            break;
                        }
                        default: {
                            var col = nd.AddStringColumn(kc);
                            col.Description = srcCol.Description;
                            col.IsCategorical = srcCol.IsCategorical;
                            if (srcCol.Labels != null)
                                foreach (var lbl in srcCol.Labels) col.AddLabel(lbl.Key, lbl.Value);
                            break;
                        }
                    }
                }

                nd.AddIntColumn("TotalN");

                // Add mean columns as double
                foreach (var mc in ao.MeanColumns) {
                    var dest = nd.AddDoubleColumn(mc);
                    dest.Description = d.GetColumn(mc).Description;
                }

                // Add count columns as int
                foreach (var cc in ao.CountColumns) {
                    var dest = nd.AddIntColumn(cc);
                    dest.Description = d.GetColumn(cc).Description;
                }

                // Accumulators keyed by column name
                var meanData  = new Dictionary<string, List<object>>();
                var countData = new Dictionary<string, List<object>>();
                var keepData  = new Dictionary<string, object?>();

                foreach (var mc in ao.MeanColumns)  meanData[mc]  = new List<object>();
                foreach (var cc in ao.CountColumns) countData[cc] = new List<object>();

                int brkColType = (int)d.GetColumn(ao.BreakVar).ColumnType; // 1=Int, 2=Dbl
                int prevBrkVal = 0;
                int brkCount   = 0;
                int rowCount   = d.RowCount;

                void FlushGroup() {
                    int rowIdx = nd.AddRow();
                    foreach (var kc in ao.KeepColumns)
                        nd.SetValueObject(rowIdx, kc, keepData[kc]);
                    nd.SetValueObject(rowIdx, "TotalN", brkCount);
                    foreach (var mc in ao.MeanColumns)
                        nd.SetValueObject(rowIdx, mc, GetMean(meanData[mc], d.GetColumn(mc).ColumnType));
                    foreach (var cc in ao.CountColumns)
                        nd.SetValueObject(rowIdx, cc, GetCount(countData[cc], d.GetColumn(cc).ColumnType));
                    foreach (var mc in ao.MeanColumns)  meanData[mc].Clear();
                    foreach (var cc in ao.CountColumns) countData[cc].Clear();
                }

                for (int i = 0; i < rowCount; i++) {
                    int curBrkVal = (int)d.GetValueObject(i, ao.BreakVar);

                    if (i == 0) {
                        prevBrkVal = curBrkVal;
                        foreach (var kc in ao.KeepColumns) keepData[kc] = d.GetValueObject(i, kc);
                    }

                    if (i > 0 && curBrkVal != prevBrkVal) {
                        FlushGroup();
                        prevBrkVal = curBrkVal;
                        brkCount = 0;
                        foreach (var kc in ao.KeepColumns) keepData[kc] = d.GetValueObject(i, kc);
                    }

                    foreach (var mc in ao.MeanColumns) {
                        var colType = d.GetColumn(mc).ColumnType;
                        object raw = d.GetValueObject(i, mc);
                        if (colType == ColumnType.Int) {
                            if ((int)raw != int.MinValue) meanData[mc].Add(raw);
                        } else {
                            if (!double.IsNaN((double)raw)) meanData[mc].Add(raw);
                        }
                    }

                    foreach (var cc in ao.CountColumns) {
                        object raw = d.GetValueObject(i, cc);
                        if ((int)raw != int.MinValue) countData[cc].Add(raw);
                    }

                    brkCount++;
                }

                // Flush the final group
                if (rowCount > 0) FlushGroup();

                m_DataArrays[ao.NewDataArray] = nd;

                commandOutput += $"Data from dataset '{ao.TargetDataArray}' aggregated to '{ao.NewDataArray}'\n";
            } catch (Exception ex) {
                commandOutput += $"Exception occurred: {ex.Message}\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }
            return commandOutput;
        }

        private double GetMean(List<object> values, ColumnType colType) {
            if (values.Count == 0) return double.NaN;
            double total = 0;
            foreach (var v in values) {
                if (colType == ColumnType.Int) total += (int)v;
                else total += (double)v;
            }
            return total / values.Count;
        }

        private int GetCount(List<object> values, ColumnType colType) {
            int total = 0;
            foreach (var v in values) {
                if (colType == ColumnType.Int) { if ((int)v != int.MinValue) total++; }
                else { if (!double.IsNaN((double)v)) total++; }
            }
            return total;
        }

        public string ExecuteAddVariableLabel(object op) {
            AddVariableLabelOperation avl = (AddVariableLabelOperation)op;
            string commandOutput = $"Executing AddVariableLabel command: {avl.OriginalCommandString}\n";
            try {
                DataStore d = m_DataArrays[avl.TargetDataArray];
                if (!d.HasColumn(avl.TargetVar))
                    throw new Exception("Target variable not found");
                d.GetColumn(avl.TargetVar).Description = avl.VariableLabel;
                commandOutput += $"Variable label for {avl.TargetVar} changed to '{avl.VariableLabel}'.\n";
            } catch (Exception ex) {
                commandOutput += $"Exception occurred: {ex.Message}\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }
            return commandOutput;
        }

        public string ExecuteAddValueLabels(object op) {
            AddValueLabelsOperation avl = (AddValueLabelsOperation)op;
            string commandOutput = $"Executing AddValueLabels command: {avl.OriginalCommandString}\n";
            try {
                DataStore d = m_DataArrays[avl.TargetDataArray];
                if (!d.HasColumn(avl.TargetVar))
                    throw new Exception("Target variable not found");
                var col = d.GetColumn(avl.TargetVar);
                foreach (var kv in avl.ValueLabels) col.AddLabel(kv.Key, kv.Value);
                commandOutput += $"{avl.ValueLabels.Count} value labels added to {avl.TargetVar}.\n";
            } catch (Exception ex) {
                commandOutput += $"Exception occurred: {ex.Message}\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }
            return commandOutput;
        }

        public string ExecuteBlankSurvey(object op) {
            BlankSurveyOperation bso = (BlankSurveyOperation)op;
            string commandOutput = $"Executing BlankSurvey command: {bso.OriginalCommandString}\n";
            try {
                DataStore d = m_DataArrays[bso.TargetDataArray];
                int rowCount = d.RowCount;

                d.AddIntColumnWithData(bso.TargetVar, 0);

                int blankSurveyCount = 0;

                for (int i = 0; i < rowCount; i++) {
                    int nonBlankCount = 0;

                    foreach (string varname in bso.Variables) {
                        if (d.GetColumn(varname).ColumnType == ColumnType.Int) {
                            int val = (int)d.GetValueObject(i, varname);
                            if (val != int.MinValue && val != 99) nonBlankCount++;
                        }
                    }

                    if (nonBlankCount < 4) {
                        d.SetValueObject(i, bso.TargetVar, 1);
                        blankSurveyCount++;
                    }
                }

                commandOutput += $"{blankSurveyCount} empty surveys found. Flag var {bso.TargetVar} created to indicate records.\n";
            } catch (Exception ex) {
                commandOutput += $"Exception occurred: {ex.Message}\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }
            return commandOutput;
        }

        public string ExecuteComputeExpr(object op) {
            ComputeExprOperation ceo = (ComputeExprOperation)op;
            string commandOutput = $"Executing ComputeExpr command: {ceo.OriginalCommandString}\n";
            try {
                DataStore d = m_DataArrays[ceo.TargetDataArray];
                int rowCount = d.RowCount;

                if (!d.HasColumn(ceo.TargetVar)) {
                    var newCol = d.AddDoubleColumn(ceo.TargetVar);
                    // default to 0.0 (already done by AddDoubleColumn)
                }

                Eval eval = new Eval();
                eval.DataArrayName = ceo.TargetDataArray;
                eval.ProcessSymbol += ProcessSymbol;

                for (int i = 0; i < rowCount; i++) {
                    bool okToCheck = true;

                    if (ceo.HasFilter) {
                        okToCheck = ceo.FilterValues.ContainsKey((int)d.GetValueObject(i, ceo.FilterVar));
                    }

                    if (okToCheck)
                        d.SetValueObject(i, ceo.TargetVar, eval.Execute(ceo.Expression, i));
                }

                commandOutput += $"Variable {ceo.TargetVar} created with result of expression\n";
            } catch (Exception ex) {
                commandOutput += $"Exception occurred: {ex.Message}\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }
            return commandOutput;
        }

        public string ExecuteComputeSum(object op) {
            ComputeSumOperation cso = (ComputeSumOperation)op;
            string commandOutput = $"Executing ComputeSum command: {cso.OriginalCommandString}\n";
            try {
                DataStore d = m_DataArrays[cso.TargetDataArray];
                bool targetIsDouble = false;

                foreach (var col in cso.VarsToSum) {
                    if (!d.HasColumn(col)) throw new Exception($"Unknown variable in list: {col}");
                    if (d.GetColumn(col).ColumnType == ColumnType.Double) targetIsDouble = true;
                }

                if (targetIsDouble)
                    d.AddDoubleColumn(cso.TargetVar);
                else
                    d.AddIntColumn(cso.TargetVar);

                int rowCount = d.RowCount;

                for (int i = 0; i < rowCount; i++) {
                    if (targetIsDouble) {
                        double total = 0;
                        foreach (var col in cso.VarsToSum) {
                            var ct = d.GetColumn(col).ColumnType;
                            object raw = d.GetValueObject(i, col);
                            if (ct == ColumnType.Double) { if (!double.IsNaN((double)raw)) total += (double)raw; }
                            else { if ((int)raw != int.MinValue) total += (int)raw; }
                        }
                        d.SetValueObject(i, cso.TargetVar, total);
                    } else {
                        int total = 0;
                        foreach (var col in cso.VarsToSum) {
                            int raw = (int)d.GetValueObject(i, col);
                            if (raw != int.MinValue) total += raw;
                        }
                        d.SetValueObject(i, cso.TargetVar, total);
                    }
                }

                commandOutput += $"Variable {cso.TargetVar} created as the sum of the specified variables\n";
            } catch (Exception ex) {
                commandOutput += $"Exception occurred: {ex.Message}\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }
            return commandOutput;
        }

        public string ExecuteCreateIntVar(object op) {
            CreateIntVarOperation cv = (CreateIntVarOperation)op;
            string commandOutput = $"Executing CreateIntVar command: {cv.OriginalCommandString}\n";
            try {
                DataStore d = m_DataArrays[cv.TargetDataArray];
                if (!d.HasColumn(cv.NewVarName))
                    d.AddIntColumnWithData(cv.NewVarName, cv.Value);
                commandOutput += $"Variable {cv.NewVarName} created with value {cv.Value}\n";
            } catch (Exception ex) {
                commandOutput += $"Exception occurred: {ex.Message}\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }
            return commandOutput;
        }

        public string ExecuteIPCheck(object op) {
            IPCheckOperation ipco = (IPCheckOperation)op;
            string commandOutput = $"Executing IPCheck command: {ipco.OriginalCommandString}\n";
            try {
                DataStore d = m_DataArrays[ipco.TargetDataArray];
                int rowCount = d.RowCount;

                if (!d.HasColumn(ipco.TargetVar))
                    d.AddIntColumnWithData(ipco.TargetVar, 0);

                if (!d.HasColumn(ipco.IPVar))
                    throw new Exception($"Variable {ipco.IPVar} does not exist.");

                var ipAddrs = new Dictionary<string, int>();

                for (int i = 0; i < rowCount; i++) {
                    string ip = (string)d.GetValueObject(i, ipco.IPVar);
                    if (ipAddrs.ContainsKey(ip)) ipAddrs[ip]++;
                    else ipAddrs[ip] = 1;
                }

                var keysToRemove = ipAddrs.Where(a => a.Value == 1).Select(a => a.Key).ToList();
                foreach (var key in keysToRemove) ipAddrs.Remove(key);

                for (int i = 0; i < rowCount; i++) {
                    string ip = (string)d.GetValueObject(i, ipco.IPVar);
                    if (ipAddrs.ContainsKey(ip))
                        d.SetValueObject(i, ipco.TargetVar, 1);
                }

                commandOutput += $"Variable {ipco.TargetVar} created and records with suspicious IP addresses flagged\n";
            } catch (Exception ex) {
                commandOutput += $"Exception occurred: {ex.Message}\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }
            return commandOutput;
        }

        public string ExecuteGenderRecode(object op) {
            GenderRecodeOperation gro = (GenderRecodeOperation)op;
            string commandOutput = $"Executing GenderRecode command: {gro.OriginalCommandString}\n";
            try {
                DataStore d = m_DataArrays[gro.TargetDataArray];
                int rowCount = d.RowCount;

                if (!d.HasColumn(gro.TargetVar))
                    d.AddIntColumnWithData(gro.TargetVar, 0);

                int recodedCount = 0;

                for (int i = 0; i < rowCount; i++) {
                    bool okToCheck = true;
                    if (gro.HasFilter)
                        okToCheck = gro.FilterValues.ContainsKey((int)d.GetValueObject(i, gro.FilterVar));

                    if (okToCheck && (int)d.GetValueObject(i, gro.GenderVar) != gro.Gender) {
                        d.SetValueObject(i, gro.GenderVar, gro.Gender);
                        d.SetValueObject(i, gro.TargetVar, 1);
                        recodedCount++;
                    }
                }

                commandOutput += $"Variable {gro.TargetVar} created and {recodedCount} records where gender has been recoded have been flagged\n";
            } catch (Exception ex) {
                commandOutput += $"Exception occurred: {ex.Message}\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }
            return commandOutput;
        }

        public string ExecuteRecode(object op) {
            RecodeOperation ro = (RecodeOperation)op;
            string commandOutput = $"Executing Recode command: {ro.OriginalCommandString}\n";
            try {
                DataStore d = m_DataArrays[ro.TargetDataArray];
                int rowCount = d.RowCount;

                if (d.GetColumn(ro.RecodeVar).ColumnType != ColumnType.Int)
                    throw new Exception("Target variable is not an integer");

                for (int i = 0; i < rowCount; i++) {
                    bool okToRecode = true;
                    if (ro.HasFilter)
                        okToRecode = ro.FilterValues.ContainsKey((int)d.GetValueObject(i, ro.FilterVar));

                    if (okToRecode) {
                        int curval = (int)d.GetValueObject(i, ro.RecodeVar);
                        if (ro.Recodes.ContainsKey(curval))
                            d.SetValueObject(i, ro.RecodeVar, ro.Recodes[curval]);
                        else if (ro.HasElseRecode)
                            d.SetValueObject(i, ro.RecodeVar, ro.ElseRecode);
                    }
                }

                commandOutput += $"Variable {ro.RecodeVar} recoded\n";
            } catch (Exception ex) {
                commandOutput += $"Exception occurred: {ex.Message}\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }
            return commandOutput;
        }

        public string ExecuteRecodeInto(object op) {
            RecodeIntoOperation ro = (RecodeIntoOperation)op;
            string commandOutput = $"Executing RecodeInto command: {ro.OriginalCommandString}\n";
            try {
                DataStore d = m_DataArrays[ro.TargetDataArray];
                int rowCount = d.RowCount;

                if (d.GetColumn(ro.RecodeVar).ColumnType != ColumnType.Int)
                    throw new Exception("Recode variable is not an integer");

                if (!d.HasColumn(ro.TargetVar))
                    d.AddIntColumnWithData(ro.TargetVar, 0);

                for (int i = 0; i < rowCount; i++) {
                    int curval = (int)d.GetValueObject(i, ro.RecodeVar);
                    if (ro.Recodes.ContainsKey(curval))
                        d.SetValueObject(i, ro.TargetVar, ro.Recodes[curval]);
                    else if (ro.HasElseRecode)
                        d.SetValueObject(i, ro.TargetVar, ro.ElseRecode);
                }

                commandOutput += $"Variable {ro.RecodeVar} recoded into {ro.TargetVar}\n";
            } catch (Exception ex) {
                commandOutput += $"Exception occurred: {ex.Message}\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }
            return commandOutput;
        }

        public string ExecuteRecodeRange(object op) {
            RecodeRangeOperation ro = (RecodeRangeOperation)op;
            string commandOutput = $"Executing RecodeRange command: {ro.OriginalCommandString}\n";
            try {
                DataStore d = m_DataArrays[ro.TargetDataArray];
                int rowCount = d.RowCount;

                if (!d.HasColumn(ro.TargetVar))
                    d.AddIntColumnWithData(ro.TargetVar, 0);

                var rcdType = d.GetColumn(ro.RecodeVar).ColumnType;

                for (int i = 0; i < rowCount; i++) {
                    double curval;
                    object raw = d.GetValueObject(i, ro.RecodeVar);
                    curval = rcdType == ColumnType.Int ? (int)raw : (double)raw;

                    foreach (var recode in ro.RecodeRangeItems) {
                        if (recode.NoMinBound && curval <= recode.HighBound) {
                            d.SetValueObject(i, ro.TargetVar, recode.RecodeValue); break;
                        } else if (recode.NoMaxBound && curval >= recode.LowBound) {
                            d.SetValueObject(i, ro.TargetVar, recode.RecodeValue); break;
                        } else if (curval >= recode.LowBound && curval <= recode.HighBound) {
                            d.SetValueObject(i, ro.TargetVar, recode.RecodeValue); break;
                        } else if (recode.IsElse) {
                            d.SetValueObject(i, ro.TargetVar, recode.RecodeValue); break;
                        }
                    }
                }

                commandOutput += $"Variable {ro.RecodeVar} recoded into {ro.TargetVar}\n";
            } catch (Exception ex) {
                commandOutput += $"Exception occurred: {ex.Message}\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }
            return commandOutput;
        }

        public string ExecuteRenameVars(object op) {
            RenameVarsOperation rvo = (RenameVarsOperation)op;
            string commandOutput = $"Executing RenameVars command: {rvo.OriginalCommandString}\n";
            try {
                DataStore d = m_DataArrays[rvo.TargetDataArray];
                var renamedVars = new System.Text.StringBuilder();

                foreach (var kv in rvo.Renames) {
                    if (d.HasColumn(kv.Key) && !d.HasColumn(kv.Value)) {
                        d.RenameColumn(kv.Key, kv.Value);
                        renamedVars.Append($"{kv.Key} => {kv.Value},");
                    }
                }

                commandOutput += $"The following renames were executed: {renamedVars.ToString().TrimEnd(',')}\n";
            } catch (Exception ex) {
                commandOutput += $"Exception occurred: {ex.Message}\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }
            return commandOutput;
        }

        public string ExecuteSigTest(object op) {
            SigTestOperation sto = (SigTestOperation)op;
            string commandOutput = $"Executing SigTest command: {sto.OriginalCommandString}\n";
            try {
                DataStore d = m_DataArrays[sto.TargetDataArray];

                d.AddDoubleColumn(sto.TargetTvalueVar);
                d.AddIntColumn(sto.TargetIndicatorVar);
                // Dataset expected to have 1 row; set initial values
                d.SetValueObject(0, sto.TargetTvalueVar, double.NaN);
                d.SetValueObject(0, sto.TargetIndicatorVar, 0);

                // Copy variable label from first category variable
                string firstVarname = sto.VarNameStem + "_" + sto.Categories[0];
                string varLabel = d.GetColumn(firstVarname).Description ?? "";
                d.GetColumn(sto.TargetTvalueVar).Description = varLabel;
                d.GetColumn(sto.TargetIndicatorVar).Description = varLabel;

                var schoolDataLookup = new Dictionary<int, double>();
                foreach (var cat in sto.Categories) {
                    string vname = sto.VarNameStem + "_" + cat;
                    schoolDataLookup[cat] = (double)d.GetValueObject(0, vname);
                }

                int schoolN = (int)d.GetValueObject(0, sto.SchoolNVar);

                double validSchSum = sto.Categories.Sum(c => schoolDataLookup[c]);
                double validSchoolN = schoolN * validSchSum / 100;

                if (validSchoolN < 5) {
                    commandOutput += $"Valid school N is not sufficient for sig testing for {sto.VarNameStem} ({validSchoolN})\n";
                    return commandOutput;
                }

                double sumProduct = 0, sumPct = 0;
                foreach (var cat in sto.Categories) {
                    sumProduct += schoolDataLookup[cat] * sto.ScoreKeyLookup[cat];
                    sumPct     += schoolDataLookup[cat];
                }
                double schoolMeanScore = sumProduct / sumPct;

                sumProduct = 0; sumPct = 0;
                foreach (var cat in sto.Categories) {
                    sumProduct += sto.NationalDataLookup[cat] * sto.ScoreKeyLookup[cat];
                    sumPct     += sto.NationalDataLookup[cat];
                }
                double nationalMeanScore = sumProduct / sumPct;

                var step2Sch = new Dictionary<int, double>();
                var step2Nat = new Dictionary<int, double>();
                foreach (var cat in sto.Categories) {
                    double sd = sto.ScoreKeyLookup[cat] - schoolMeanScore;
                    step2Sch[cat] = sd * sd;
                    double nd2 = sto.ScoreKeyLookup[cat] - nationalMeanScore;
                    step2Nat[cat] = nd2 * nd2;
                }

                sumProduct = 0; sumPct = 0;
                foreach (var cat in sto.Categories) {
                    sumProduct += schoolDataLookup[cat] * step2Sch[cat];
                    sumPct     += schoolDataLookup[cat];
                }
                double schStdDev = Math.Sqrt((validSchoolN / (validSchoolN - 1)) * (sumProduct / sumPct));

                double validNatSum = sto.Categories.Sum(c => sto.NationalDataLookup[c]);
                double validNationalN = sto.NationalN * validNatSum / 100;

                sumProduct = 0; sumPct = 0;
                foreach (var cat in sto.Categories) {
                    sumProduct += sto.NationalDataLookup[cat] * step2Nat[cat];
                    sumPct     += sto.NationalDataLookup[cat];
                }
                double natStdDev = Math.Sqrt((validNationalN / (validNationalN - 1)) * (sumProduct / sumPct));

                double schStdErr    = Math.Max(schStdDev, sto.StdDevFloor) / Math.Sqrt(validSchoolN);
                double natStdErr    = sto.NationalDesignEffect * natStdDev / Math.Sqrt(validNationalN);
                double pooledStdErr = Math.Sqrt(schStdErr * schStdErr + natStdErr * natStdErr);

                double scoreDifference = schoolMeanScore - nationalMeanScore;
                double tvalue          = Math.Abs(scoreDifference / pooledStdErr);
                double degFreedom      = validSchoolN - 1;

                var dist = new StudentDistribution(Math.Round(degFreedom, 0));
                double significance = dist.RightProbability(tvalue) * 2;

                int prelimIndicator = 0;
                if (significance < 0.05 && schoolMeanScore > nationalMeanScore)       prelimIndicator =  1;
                else if (significance < 0.05 && schoolMeanScore < nationalMeanScore)   prelimIndicator = -1;

                int scoreDifferenceSign = (scoreDifference > 0) ? 1 : -1;

                // Use column index of first category variable as tiebreaker
                int uniqueValue = d.GetColumnIndex(firstVarname);
                double signedTValue = scoreDifferenceSign * sto.QuestionIndicator * Math.Round(tvalue, 8)
                                      + (0.000000000001 * uniqueValue);
                int finalIndicator = prelimIndicator * sto.QuestionIndicator;

                d.SetValueObject(0, sto.TargetTvalueVar,    signedTValue);
                d.SetValueObject(0, sto.TargetIndicatorVar, finalIndicator);

                commandOutput += "Varname,sch mean,nat mean,sch stddev,nat stddev,sch stderr,nat stderr,pooled err,score diff,tvalue,sig,signed t,final ind\n";
                commandOutput += $"{sto.VarNameStem},{schoolMeanScore},{nationalMeanScore},{schStdDev},{natStdDev},{schStdErr},{natStdErr},{pooledStdErr},{scoreDifference},{tvalue},{significance},{signedTValue},{finalIndicator}\n";
            } catch (Exception ex) {
                commandOutput += $"Exception occurred: {ex.Message}\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }
            return commandOutput;
        }

        public string ExecuteSplitVar(object op) {
            SplitVarOperation svo = (SplitVarOperation)op;
            string commandOutput = $"Executing SplitVar command: {svo.OriginalCommandString}\n";
            try {
                DataStore d = m_DataArrays[svo.TargetDataArray];
                int rowCount = d.RowCount;

                if (d.GetColumn(svo.TargetVar).ColumnType != ColumnType.Int)
                    throw new Exception("Target variable is not an integer");

                string newVarLabel = d.GetColumn(svo.TargetVar).Description ?? "";

                // Maps split value → column name
                var svlu = new Dictionary<int, string>();

                foreach (var s in svo.SplitValues) {
                    string colName = svo.TargetVar + "_" + s;
                    d.AddIntColumnWithData(colName, 0);
                    d.GetColumn(colName).Description = newVarLabel;
                    svlu[s] = colName;
                }

                string missingColName = svo.TargetVar + "_" + svo.MissingValue;
                d.AddIntColumnWithData(missingColName, 0);
                svlu[int.MinValue] = missingColName;

                for (int i = 0; i < rowCount; i++) {
                    bool okToSplit = true;
                    if (svo.HasFilter)
                        okToSplit = svo.FilterValues.ContainsKey((int)d.GetValueObject(i, svo.FilterVar));

                    if (okToSplit) {
                        // AddIntColumnWithData pre-filled with 0; set the matching one to 100
                        int curval = (int)d.GetValueObject(i, svo.TargetVar);
                        string targetColName = svlu.ContainsKey(curval) ? svlu[curval] : svlu[int.MinValue];
                        d.SetValueObject(i, targetColName, 100);
                    } else {
                        // Set all split columns to MinValue for this row
                        foreach (var kv in svlu)
                            d.SetValueObject(i, kv.Value, int.MinValue);
                    }
                }

                commandOutput += $"Variable {svo.TargetVar} has been split into separate variables for each value\n";
            } catch (Exception ex) {
                commandOutput += $"Exception occurred: {ex.Message}\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }
            return commandOutput;
        }

        public string ExecuteSort(object op) {
            SortOperation so = (SortOperation)op;
            string commandOutput = $"Executing Sort command: {so.OriginalCommandString}\n";
            try {
                DataStore d = m_DataArrays[so.TargetDataArray];
                int rowCount = d.RowCount;

                int[] sortData    = new int[rowCount];
                int[] reorderRef  = new int[rowCount];

                for (int i = 0; i < rowCount; i++) {
                    sortData[i]   = (int)d.GetValueObject(i, so.TargetVar);
                    reorderRef[i] = i;
                }

                Array.Sort(sortData, reorderRef);
                d.ReorderRows(reorderRef);

                commandOutput += $"Dataset sorted by {so.TargetVar}\n";
            } catch (Exception ex) {
                commandOutput += $"Exception occurred: {ex.Message}\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }
            return commandOutput;
        }

        protected void ProcessSymbol(object? sender, SymbolEventArgs e) {
            DataStore d = m_DataArrays[e.DataArrayName];
            if (d.HasColumn(e.Name)) {
                object raw = d.GetValueObject(e.Row, e.Name);
                if (raw is int iv)
                    e.Result = (iv == int.MinValue) ? double.NaN : (double)iv;
                else if (raw is double dv)
                    e.Result = dv;
                else
                    e.Result = 0.0;
            } else {
                e.Status = SymbolStatus.UndefinedSymbol;
            }
        }

        public string ExecuteSetRemoveFlag(object op) {
            SetRemoveFlagOperation srfo = (SetRemoveFlagOperation)op;
            string commandOutput = $"Executing SetRemoveFlag command: {srfo.OriginalCommandString}\n";
            try {
                DataStore d = m_DataArrays[srfo.TargetDataArray];
                int rowCount = d.RowCount;

                if (!d.HasColumn(srfo.TargetVar))
                    d.AddIntColumnWithData(srfo.TargetVar, 0);

                foreach (var ri in srfo.RecordsToRemove)
                    d.SetValueObject(ri, srfo.TargetVar, 1);

                commandOutput += $"Variable {srfo.TargetVar} created and records to be removed have been flagged\n";
            } catch (Exception ex) {
                commandOutput += $"Exception occurred: {ex.Message}\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }
            return commandOutput;
        }

        public string ExecuteRemoveRecords(object op) {
            RemoveRecordsOperation rro = (RemoveRecordsOperation)op;
            string commandOutput = $"Executing RemoveRecords command: {rro.OriginalCommandString}\n";
            try {
                DataStore d = m_DataArrays[rro.TargetDataArray];

                if (!d.HasColumn(rro.TargetVar)) {
                    commandOutput += $"{rro.TargetVar} does not exist, no records have been removed\n";
                    return commandOutput;
                }

                int rowCount = d.RowCount;
                int recordsRemoved = 0;

                // Iterate backwards so row removal doesn't affect unvisited indices
                for (int i = rowCount - 1; i >= 0; i--) {
                    if ((int)d.GetValueObject(i, rro.TargetVar) == 1) {
                        d.RemoveRow(i);
                        recordsRemoved++;
                    }
                }

                commandOutput += $"{recordsRemoved} records flagged in {rro.TargetVar} have been removed\n";
            } catch (Exception ex) {
                commandOutput += $"Exception occurred: {ex.Message}\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }
            return commandOutput;
        }

        public string ExecuteKeepVars(object op) {
            KeepVarsOperation kvo = (KeepVarsOperation)op;
            string commandOutput = $"Executing KeepVars command: {kvo.OriginalCommandString}\n";
            try {
                DataStore d = m_DataArrays[kvo.TargetDataArray];
                DataStore extracted = d.Extract(kvo.VarsToKeep);

                if (kvo.CreateNewArray)
                    m_DataArrays[kvo.NewDataArray] = extracted;
                else
                    m_DataArrays[kvo.TargetDataArray] = extracted;

                commandOutput += "Dataset variables reordered.\n";
            } catch (Exception ex) {
                commandOutput += $"Exception occurred: {ex.Message}\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }
            return commandOutput;
        }

        public string ExecuteComment(object op) {
            CommentOperation co = (CommentOperation)op;
            return co.CommentText + "\n";
        }

        public string ExecuteExport(object op) {
            ExportOperation eo = (ExportOperation)op;
            string commandOutput = $"Executing Export command: {eo.OriginalCommandString}\n";
            try {
                DataStore d = m_DataArrays[eo.TargetDataArray];

                string filePath = eo.Filename.StartsWith("!")
                    ? m_fileHandles[eo.Filename]
                    : eo.Filename;

                if (eo.Mode == ExportMode.New) {
                    if (eo.FileType == ExportType.Csv) d.Export(filePath);
                    else d.ExportToSpss(filePath);
                } else {
                    if (eo.FileCopyReqd) {
                        string src = eo.FileToCopy.StartsWith("!")
                            ? m_fileHandles[eo.FileToCopy]
                            : eo.FileToCopy;
                        File.Copy(src, filePath, true);
                    }
                    d.AppendToCsv(filePath);
                }

                string exportMode = (eo.Mode == ExportMode.New) ? "exported" : "appended";
                commandOutput += $"Dataset {eo.TargetDataArray} {exportMode} to {filePath}\n";
            } catch (Exception ex) {
                commandOutput += $"Exception occurred: {ex.Message}\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }
            return commandOutput;
        }
    }
}
