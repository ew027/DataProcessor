using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

using Meta.Numerics.Statistics.Distributions;

using DataProcessor.Engine.Operations;

namespace DataProcessor.Engine {
    public class Engine {
        private Dictionary<string, DataArray> m_DataArrays;
        private Dictionary<string, List<object>> m_Operations;
        private List<string> m_LogEntries;

        // used to store job-specific data to be accessed by cleaning script
        private Dictionary<string, string> m_fileHandles;

        // logs details of last error
        private ErrorStatus m_errorStatus;

        public Engine() {
            m_DataArrays = new Dictionary<string, DataArray>();
            m_Operations = new Dictionary<string, List<object>>();
            m_LogEntries = new List<string>();
            m_fileHandles = new Dictionary<string, string>();
            m_errorStatus = new ErrorStatus();
        }

        public DataArray GetDataArray(string dataName) {
            return m_DataArrays[dataName];
        }

        public void AddDataArray(DataArray da, string dataName) {
            m_DataArrays.Add(dataName, da);
        }

        public void ExportDataArray(string dataName, string filePath) {
            m_DataArrays[dataName].Export(filePath);

            m_LogEntries.Add("Dataset \"" + dataName + "\" exported to " + filePath + "\n");
        }

        public void AppendDataArrayToCsv(string dataName, string filePath) {
            m_DataArrays[dataName].AppendToCsv(filePath);

            m_LogEntries.Add("Dataset \"" + dataName + "\" appended to " + filePath + "\n");
        }

        public void LoadScript(string filePath, string operationName) {
            using (StreamReader sr = new StreamReader(filePath)) {
                string command;
                while ((command = sr.ReadLine()) != null) {
                    AddOperation(command, operationName);
                }
            }
        }

        public void AddOperation(string command, string operationName) {
            // allow comments to be handled as a command
            if (!String.IsNullOrEmpty(command)) {
                if (command.Substring(0, 1) == "#") {
                    command = "Comment|" + command;
                }

                string[] cmdArray = command.Split(new Char[] { '|' }, 2);

                string cmdName = "DataProcessor.Engine.Operations." + cmdArray[0] + "Operation";
                string cmdParams = cmdArray[1];

                if (!m_Operations.ContainsKey(operationName)) {
                    m_Operations.Add(operationName, new List<object>());
                }

                Type opType = Type.GetType(cmdName);

                try {
                    m_Operations[operationName].Add(Activator.CreateInstance(opType, new object[] { cmdParams }));
                } catch (Exception ex) {
                    m_errorStatus.CaughtException = ex;
                    m_errorStatus.ErrorLogged = true;
                    m_errorStatus.OffendingCommand = command;
                    Console.WriteLine("Error occurred parsing command: " + command);
                    m_LogEntries.Add("Error occurred parsing command: " + command);
                }
            }
        }

        public void Execute(string operationName) {
            m_LogEntries.Add("Starting execution of \"" + operationName + "\" on " + System.DateTime.Now.ToUniversalTime().ToString() + "\n\n");

            // reset the error status
            m_errorStatus.ErrorLogged = false;

            foreach (var op in m_Operations[operationName]) {
                string result = "";

                Type thisType = this.GetType();
                Operation opName = (Operation)op;

                result = (String)thisType.InvokeMember(opName.GetExecuteMethod(), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance, null, this, new Object[] { op });
                                
                m_LogEntries.Add(result);
            }
        }

        public List<string> GetLogEntries() {
            return m_LogEntries;
        }

        // done
        public void SaveLogEntries(string filePath) {
            using (StreamWriter sw = new StreamWriter(filePath)) {
                sw.WriteLine("Data processing log generated on " + System.DateTime.Now.ToUniversalTime().ToString());
                sw.WriteLine();
                foreach (var le in m_LogEntries) {
                    sw.WriteLine(le);
                }
            }
        }

        public bool CheckErrorStatus() {
            return m_errorStatus.ErrorLogged;
        }

        public ErrorStatus GetErrorStatus() {
            return m_errorStatus;
        }

        public void AddFileHandle(string handle, string filePath) {
            m_fileHandles.Add(handle, filePath);
        }

        // done
        public string ExecuteAboveBelow(object op) {
            AboveBelowOperation abo = (AboveBelowOperation)op;

            string commandOutput = "";

            commandOutput = "Executing AboveBelow command: " + abo.OriginalCommandString + "\n";

            try {
                DataArray d = m_DataArrays[abo.TargetDataArray];

                int rowCount = d.RowCount();
                int colPos = 0;
                int abovePos = 0;
                int belowPos = 0;

                if (d.ColumnPositions.ContainsKey(abo.TargetVar)) {
                    colPos = d.ColumnPositions[abo.TargetVar];
                } else {
                    throw new Exception("Variable to check doesn't exist: " + abo.TargetVar);
                }

                // Above var
                Column col = new Column();
                col.Name = abo.AboveVar;
                col.IsCategorical = false;
                col.Type = ColumnType.Int;
                abovePos = d.AddColumn(col);

                // default flag to 0
                for (int i = 0; i < rowCount; i++) {
                    d.Data[abovePos].Add(0);
                }

                // Below var
                col = new Column();
                col.Name = abo.BelowVar;
                col.IsCategorical = false;
                col.Type = ColumnType.Int;
                belowPos = d.AddColumn(col);

                // default flag to 0
                for (int i = 0; i < rowCount; i++) {
                    d.Data[belowPos].Add(0);
                }

                for (int i = 0; i < rowCount; i++) {
                    if ((double)d.Data[colPos][i] >= 100) {
                        d.Data[abovePos][i] = 100;
                    } else {
                        d.Data[belowPos][i] = 100;
                    }
                }

                commandOutput += "Variable " + abo.TargetVar + " recoded into above/below variables\n";
            } catch (Exception ex) {
                commandOutput += "Exception occurred: " + ex.Message + "\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }

            return commandOutput;
        }

        // done
        public string ExecuteAggregate(object op) {
            AggregateOperation ao = (AggregateOperation)op;

            string commandOutput = "";

            commandOutput = "Executing Aggregate command: " + ao.OriginalCommandString + "\n";

            try {
                DataArray d = m_DataArrays[ao.TargetDataArray];

                DataArray nd = new DataArray();

                // create list of indices of aggregate vars
                List<int> curKeepIdx = new List<int>();
                List<int> curMeanIdx = new List<int>();
                List<int> curCountIdx = new List<int>();

                // keep columns first
                foreach (var kc in ao.KeepColumns) {
                    curKeepIdx.Add(d.ColumnPositions[kc]);
                }

                // now mean columns
                foreach (var mc in ao.MeanColumns) {
                    curMeanIdx.Add(d.ColumnPositions[mc]);
                }

                // now count columns
                foreach (var mc in ao.CountColumns) {
                    curCountIdx.Add(d.ColumnPositions[mc]);
                }

                Dictionary<int, int> c2nLU = new Dictionary<int, int>();

                // create new vars to hold aggregate data & note positions - keep data
                foreach (var kp in curKeepIdx) {
                    c2nLU.Add(kp, nd.AddColumn(Column.Clone(d.Columns[kp], false)));
                }

                // add column to store break totals
                Column col = new Column();
                col.Name = "TotalN";
                col.Type = ColumnType.Int;
                col.IsCategorical = false;

                int brkTotalPos = nd.AddColumn(col);

                // create new vars to hold aggregate data & note positions - mean data
                foreach (var mp in curMeanIdx) {
                    c2nLU.Add(mp, nd.AddColumn(Column.Clone(d.Columns[mp], true)));
                }

                foreach (var cp in curCountIdx) {
                    c2nLU.Add(cp, nd.AddColumn(Column.Clone(d.Columns[cp], false)));
                }

                int brkColPos = d.ColumnPositions[ao.BreakVar];

                int rowCount = d.RowCount();

                // create the dictionary & lists to hold the data to be aggregated
                Dictionary<int, List<object>> meanData = new Dictionary<int, List<object>>();
                Dictionary<int, List<object>> countData = new Dictionary<int, List<object>>();
                Dictionary<int, object> keepData = new Dictionary<int, object>();
                int brkCount = 0;

                foreach (var mc in curMeanIdx) {
                    meanData.Add(mc, new List<object>());
                }

                foreach (var mc in curCountIdx) {
                    countData.Add(mc, new List<object>());
                }

                int prevBrkVal = 0;

                for (int i = 0; i < rowCount; i++) {
                    int curBrkVal = (int)d.Data[brkColPos][i];

                    if (i == 0) {
                        prevBrkVal = curBrkVal;

                        // populate keep data for first break value
                        foreach (var kp in curKeepIdx) {
                            keepData[kp] = d.Data[kp][i];
                        }
                    }

                    if (i > 0 && curBrkVal != prevBrkVal) {
                        // copy all the current values to the new dataarray & clear the temp agg list structures
                        foreach (var kp in curKeepIdx) {
                            nd.Data[c2nLU[kp]].Add(keepData[kp]);
                            keepData[kp] = d.Data[kp][i];
                        }

                        foreach (var mp in curMeanIdx) {
                            nd.Data[c2nLU[mp]].Add(GetMean(meanData[mp], d.Columns[mp].Type));
                            meanData[mp].Clear();
                        }

                        foreach (var cp in curCountIdx) {
                            nd.Data[c2nLU[cp]].Add(GetCount(countData[cp], d.Columns[cp].Type));
                            countData[cp].Clear();
                        }

                        nd.Data[brkTotalPos].Add(brkCount);

                        prevBrkVal = curBrkVal;
                        brkCount = 0;
                    }

                    foreach (var mp in curMeanIdx) {
                        if (d.Columns[mp].Type == ColumnType.Int) {
                            if ((int)d.Data[mp][i] != Int32.MinValue) {
                                meanData[mp].Add((int)d.Data[mp][i]);
                            }
                        } else {
                            if (!Double.IsNaN((double)d.Data[mp][i])) {
                                meanData[mp].Add((double)d.Data[mp][i]);
                            }
                        }
                    }

                    foreach (var cp in curCountIdx) {
                        if (d.Columns[cp].Type == ColumnType.Int) {
                            if ((int)d.Data[cp][i] != Int32.MinValue) {
                                countData[cp].Add((int)d.Data[cp][i]);
                            }
                        }
                    }

                    brkCount++;
                }

                // final copy of last set of aggregate data
                foreach (var kp in curKeepIdx) {
                    nd.Data[c2nLU[kp]].Add(keepData[kp]);
                }

                foreach (var mp in curMeanIdx) {
                    nd.Data[c2nLU[mp]].Add(GetMean(meanData[mp], d.Columns[mp].Type));
                }

                foreach (var cp in curCountIdx) {
                    nd.Data[c2nLU[cp]].Add(GetCount(countData[cp], d.Columns[cp].Type));
                }

                nd.Data[brkTotalPos].Add(brkCount);

                m_DataArrays.Add(ao.NewDataArray, nd);

                commandOutput += "Data from dataset '" + ao.TargetDataArray + "' aggregated to '" + ao.NewDataArray + "'\n";
            } catch (Exception ex) {
                commandOutput += "Exception occurred: " + ex.Message + "\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;

            }

            return commandOutput;
        }

        private double GetMean(List<object> values, ColumnType colType) {
            double total = 0;
            foreach (var v in values) {
                if (colType == ColumnType.Int) {
                    total += (int)v;
                } else {
                    total += (double)v;
                }
            }

            return total / values.Count;
        }

        private int GetCount(List<object> values, ColumnType colType) {
            int total = 0;
            foreach (var v in values) {
                if (colType == ColumnType.Int) {
                    if ((int)v != Int32.MinValue) {
                        total++;
                    }
                } else {
                    if (!Double.IsNaN((double)v)) {
                        total++;
                    }
                }
            }

            return total;
        }

        public string ExecuteAddVariableLabel(object op) {
            AddVariableLabelOperation avl = (AddVariableLabelOperation)op;

            string commandOutput = "";

            commandOutput = "Executing AddVariableLabel command: " + avl.OriginalCommandString + "\n";

            try {
                DataArray d = m_DataArrays[avl.TargetDataArray];

                if (!d.ColumnPositions.ContainsKey(avl.TargetVar)) {
                    throw new Exception("Target variable not found");
                }

                Column col = d.Columns[d.ColumnPositions[avl.TargetVar]];

                col.Description = avl.VariableLabel;

                commandOutput += "Variable label for " + avl.TargetVar + " changed to '" + avl.VariableLabel + "'.\n";
            } catch (Exception ex) {
                commandOutput += "Exception occurred: " + ex.Message + "\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;

            }

            return commandOutput;
        }
        public string ExecuteAddValueLabels(object op) {
            AddValueLabelsOperation avl = (AddValueLabelsOperation)op;

            string commandOutput = "";

            commandOutput = "Executing AddValueLabels command: " + avl.OriginalCommandString + "\n";

            try {
                DataArray d = m_DataArrays[avl.TargetDataArray];

                if (!d.ColumnPositions.ContainsKey(avl.TargetVar)) {
                    throw new Exception("Target variable not found");
                }

                Column col = d.Columns[d.ColumnPositions[avl.TargetVar]];

                foreach (var value in avl.ValueLabels.Keys) {
                    col.AddLabel(value, avl.ValueLabels[value]);
                }

                commandOutput += avl.ValueLabels.Count + " value labels added to " + avl.TargetVar + ".\n";
            } catch (Exception ex) {
                commandOutput += "Exception occurred: " + ex.Message + "\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;

            }

            return commandOutput;
        }

        // done
        public string ExecuteBlankSurvey(object op) {
            BlankSurveyOperation bso = (BlankSurveyOperation)op;

            string commandOutput = "";

            commandOutput = "Executing BlankSurvey command: " + bso.OriginalCommandString + "\n";

            try {
                DataArray d = m_DataArrays[bso.TargetDataArray];

                int rowCount = d.RowCount();
                int colPos = 0;
                
                // New var
                Column col = new Column();
                col.Name = bso.TargetVar;
                col.IsCategorical = false;
                col.Type = ColumnType.Int;
                colPos = d.AddColumn(col);

                // default flag to 0
                for (int i = 0; i < rowCount; i++) {
                    d.Data[colPos].Add(0);
                }

                int blankSurveyCount = 0;

                for (int i = 0; i < rowCount; i++) {
                    int nonBlankCount = 0;

                    foreach (string varname in bso.Variables) { 
                        int j = d.ColumnPositions[varname];

                        if (d.Columns[j].Type == ColumnType.Int) {
                            if ((int)d.Data[j][i] != Int32.MinValue && (int)d.Data[j][i] != 99) {
                                nonBlankCount++;
                            }
                        }
                    }

                    if (nonBlankCount < 4) {
                        d.Data[colPos][i] = 1;
                        blankSurveyCount++;
                    }
                }

                commandOutput += blankSurveyCount + " empty surveys found. Flag var " + bso.TargetVar + " created to indicate records.\n";
            } catch (Exception ex) {
                commandOutput += "Exception occurred: " + ex.Message + "\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;

            }

            return commandOutput;
        }

        // done
        public string ExecuteComputeExpr(object op) {
            ComputeExprOperation ceo = (ComputeExprOperation)op;

            string commandOutput = "";

            commandOutput = "Executing ComputeExpr command: " + ceo.OriginalCommandString + "\n";

            try {
                DataArray d = m_DataArrays[ceo.TargetDataArray];

                /*
                 * Method:
                 * =======
                 * Create new TargetVar column as a Double
                 * Create a new Eval instance
                 * Run through the dataset and evaluate the expression, checking to see if filter applies if applicable
                 */

                int rowCount = d.RowCount();
                int colPos = 0;

                if (d.ColumnPositions.ContainsKey(ceo.TargetVar)) {
                    colPos = d.ColumnPositions[ceo.TargetVar];
                } else {
                    Column col = new Column();
                    col.Name = ceo.TargetVar;
                    col.IsCategorical = false;
                    col.Type = ColumnType.Double;
                    colPos = d.AddColumn(col);

                    // default flag to 0
                    for (int i = 0; i < rowCount; i++) {
                        d.Data[colPos].Add(0.0);
                    }
                }

                int filterCol = 0;

                if (ceo.HasFilter) {
                    filterCol = d.ColumnPositions[ceo.FilterVar];
                }

                Eval eval = new Eval();
                eval.DataArrayName = ceo.TargetDataArray;
                eval.ProcessSymbol += ProcessSymbol;

                for (int i = 0; i < rowCount; i++) {
                    bool okToCheck = true;

                    if (ceo.HasFilter) {
                        okToCheck = (ceo.FilterValues.ContainsKey((int)d.Data[filterCol][i]));
                    }

                    if (okToCheck) {
                        d.Data[colPos][i] = eval.Execute(ceo.Expression, i);
                    }
                }
                commandOutput += "Variable " + ceo.TargetVar + " created with result of expression\n";
            } catch (Exception ex) {
                commandOutput += "Exception occurred: " + ex.Message + "\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }

            return commandOutput;
        }

        // done
        public string ExecuteComputeSum(object op) {
            ComputeSumOperation cso = (ComputeSumOperation)op;
            
            string commandOutput = "";

            commandOutput = "Executing ComputeSum command: " + cso.OriginalCommandString + "\n";

            try {
                DataArray d = m_DataArrays[cso.TargetDataArray];

                /*
                 * NB: Missing values are ignored when summing across variables, and are equivalent to the value being 0
                 */ 

                List<int> columnsToSum = new List<int>();
                bool targetIsDouble = false;

                // check all columns exist
                foreach (var col in cso.VarsToSum) {
                    if (d.ColumnPositions.ContainsKey(col)) {
                        int colPos = d.ColumnPositions[col];
                        columnsToSum.Add(colPos);
                        if (d.Columns[colPos].Type == ColumnType.Double) {
                            targetIsDouble = true;
                        }
                    } else {
                        throw new Exception("Unknown variable in list: " + col);
                    }
                }

                // create new column
                Column targetCol = new Column();

                targetCol.Name = cso.TargetVar;
                targetCol.IsCategorical = false;
                targetCol.Type = targetIsDouble ? ColumnType.Double : ColumnType.Int;

                int targetColPos = d.AddColumn(targetCol);
                int rowCount = d.RowCount();

                for (int i = 0; i < rowCount; i++) {
                    if (targetIsDouble) {
                        double dblRunningTotal = 0;
                        foreach (var cp in columnsToSum) {
                            if (d.Columns[cp].Type == ColumnType.Double) {
                                if (!Double.IsNaN((double)d.Data[cp][i])) {
                                    dblRunningTotal += (double)d.Data[cp][i];
                                }
                            } else if (d.Columns[cp].Type == ColumnType.Int) {
                                if ((int)d.Data[cp][i] != Int32.MinValue) {
                                    dblRunningTotal += (double)d.Data[cp][i];
                                }
                            }
                        }

                        d.Data[targetColPos].Add(dblRunningTotal);
                    } else {
                        int intRunningTotal = 0;
                        foreach (var cp in columnsToSum) {
                            if ((int)d.Data[cp][i] != Int32.MinValue) {
                                intRunningTotal += (int)d.Data[cp][i];
                            }
                        }

                        d.Data[targetColPos].Add(intRunningTotal);
                    }
                }

                commandOutput += "Variable " + cso.TargetVar + " created as the sum of the specified variables\n";
            } catch (Exception ex) {
                commandOutput += "Exception occurred: " + ex.Message + "\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }

            return commandOutput;
        }

        public string ExecuteCreateIntVar(object op) {
            CreateIntVarOperation cv = (CreateIntVarOperation)op;

            string commandOutput = "";

            commandOutput = "Executing CreateIntVar command: " + cv.OriginalCommandString + "\n";

            try {
                DataArray d = m_DataArrays[cv.TargetDataArray];

                int newPos = 0;

                if (!d.ColumnPositions.ContainsKey(cv.NewVarName)) {
                    newPos = d.AddIntColumnWithData(cv.NewVarName, cv.Value);
                }

                commandOutput += "Variable " + cv.NewVarName + " created with value " + cv.Value + "\n";
            } catch (Exception ex) {
                commandOutput += "Exception occurred: " + ex.Message + "\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }

            return commandOutput;
        }

        // done
        public string ExecuteIPCheck(object op) {
            IPCheckOperation ipco = (IPCheckOperation)op;

            string commandOutput = "";

            commandOutput = "Executing IPCheck command: " + ipco.OriginalCommandString + "\n";

            try {
                DataArray d = m_DataArrays[ipco.TargetDataArray];

                /*
                 * Method:
                 * =======
                 * - create a dictionary to hold the IP addresses (k) and a count
                 * - add a new column to contain the flag and keep the col pos
                 * - run through the contents of the IP variable and add it to the dictionary if it doesn't exist
                 * - then run through the keys of the dictionary and strip out any IP that only occurs once
                 * - run through the contents of the IP variable again - if the dictionary contains the key, set flag to 1, otherwise 0
                 */

                int rowCount = d.RowCount();
                int colPos = 0;

                if (d.ColumnPositions.ContainsKey(ipco.TargetVar)) {
                    colPos = d.ColumnPositions[ipco.TargetVar];
                } else {
                    Column col = new Column();
                    col.Name = ipco.TargetVar;
                    col.IsCategorical = false;
                    col.Type = ColumnType.Int;
                    colPos = d.AddColumn(col);

                    // default flag to 0
                    for (int i = 0; i < rowCount; i++) {
                        d.Data[colPos].Add(0);
                    }
                }

                int ipPos = 0;

                if (!d.ColumnPositions.ContainsKey(ipco.IPVar)) {
                    throw new Exception("Variable " + ipco.IPVar + " does not exist.\n");
                } else {
                    ipPos = d.ColumnPositions[ipco.IPVar];
                }

                Dictionary<string, int> ipAddrs = new Dictionary<string, int>();

                // count occurrences of each IP
                for (int i = 0; i < rowCount; i++) {
                    string currentIp = (string)d.Data[ipPos][i];

                    if (ipAddrs.ContainsKey(currentIp)) {
                        ipAddrs[currentIp]++;
                    } else {
                        ipAddrs.Add(currentIp, 1);
                    }
                }

                // remove all the IP addresses that occur only once
                var singleOccurs = from a in ipAddrs where a.Value == 1 select a;
                var keysToRemove = singleOccurs.Select(e => e.Key).ToList();

                foreach (var removal in keysToRemove) {
                    ipAddrs.Remove(removal);
                }

                // flag all remaining IP addresses
                for (int i = 0; i < rowCount; i++) {
                    string currentIp = (string)d.Data[ipPos][i];

                    if (ipAddrs.ContainsKey(currentIp)) {
                        d.Data[colPos][i] = 1;
                    }
                }


                commandOutput += "Variable " + ipco.TargetVar + " created and records with suspicious IP addresses flagged\n";
            } catch (Exception ex) {
                commandOutput += "Exception occurred: " + ex.Message + "\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }

            return commandOutput;
        }

        // done
        public string ExecuteGenderRecode(object op) {
            GenderRecodeOperation gro = (GenderRecodeOperation)op;

            string commandOutput = "";

            commandOutput = "Executing GenderRecode command: " + gro.OriginalCommandString + "\n";

            try {
                DataArray d = m_DataArrays[gro.TargetDataArray];

                /*
                 * Method:
                 * =======
                 * Check if GenderVar has been created already, if not then create
                 * Run through contents of the Gender variable and set to Gender value, checking to see if filter applies if applicable
                 */

                int rowCount = d.RowCount();
                int colPos = 0;

                if (d.ColumnPositions.ContainsKey(gro.TargetVar)) {
                    colPos = d.ColumnPositions[gro.TargetVar];
                } else {
                    Column col = new Column();
                    col.Name = gro.TargetVar;
                    col.IsCategorical = false;
                    col.Type = ColumnType.Int;
                    colPos = d.AddColumn(col);

                    // default flag to 0
                    for (int i = 0; i < rowCount; i++) {
                        d.Data[colPos].Add(0);
                    }
                }

                int genderPos = d.ColumnPositions[gro.GenderVar];

                int filterCol = 0;

                if (gro.HasFilter) {
                    filterCol = d.ColumnPositions[gro.FilterVar];
                }

                int recodedCount = 0;

                for (int i = 0; i < rowCount; i++) {
                    bool okToCheck = true;

                    if (gro.HasFilter) {
                        okToCheck = (gro.FilterValues.ContainsKey((int)d.Data[filterCol][i]));
                    }

                    if (okToCheck) {
                        if ((int)d.Data[genderPos][i] != gro.Gender) {
                            d.Data[genderPos][i] = gro.Gender;
                            d.Data[colPos][i] = 1;
                            recodedCount++;
                        }
                    }
                }

                commandOutput += "Variable " + gro.TargetVar + " created and " + recodedCount + " records where gender has been recoded have been flagged\n";
            } catch (Exception ex) {
                commandOutput += "Exception occurred: " + ex.Message + "\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }

            return commandOutput;
        }

        // done
        public string ExecuteRecode(object op) {
            RecodeOperation ro = (RecodeOperation)op;

            string commandOutput = "";

            commandOutput = "Executing Recode command: " + ro.OriginalCommandString + "\n";

            try {
                DataArray d = m_DataArrays[ro.TargetDataArray];

                int rowCount = d.RowCount();
                int colPos = d.ColumnPositions[ro.RecodeVar];

                if (d.Columns[colPos].Type != ColumnType.Int) {
                    throw new Exception("Target variable is not an integer");
                }

                int filterCol = 0;

                if (ro.HasFilter) {
                    filterCol = d.ColumnPositions[ro.FilterVar];
                }

                for (int i = 0; i < rowCount; i++) {
                    bool okToRecode = true;

                    if (ro.HasFilter) {
                        okToRecode = (ro.FilterValues.ContainsKey((int)d.Data[filterCol][i]));
                    }

                    if (okToRecode) {

                        int curval = (int)d.Data[colPos][i];
                        if (ro.Recodes.ContainsKey(curval)) {
                            d.Data[colPos][i] = ro.Recodes[curval];
                        } else {
                            // is there an "else" value we need to recode to?
                            if (ro.HasElseRecode) {
                                d.Data[colPos][i] = ro.ElseRecode;
                            }
                        }
                    }
                }
                commandOutput += "Variable " + ro.RecodeVar + " recoded\n";
            } catch (Exception ex) {
                commandOutput += "Exception occurred: " + ex.Message + "\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }

            return commandOutput;
        }

        // done
        public string ExecuteRecodeInto(object op) {
            RecodeIntoOperation ro = (RecodeIntoOperation)op;

            string commandOutput = "";

            commandOutput = "Executing RecodeInto command: " + ro.OriginalCommandString + "\n";

            try {
                DataArray d = m_DataArrays[ro.TargetDataArray];

                int rowCount = d.RowCount();
                int rcdPos = d.ColumnPositions[ro.RecodeVar];

                if (d.Columns[rcdPos].Type != ColumnType.Int) {
                    throw new Exception("Recode variable is not an integer");
                }

                // check for existence of target var & create if doesn't exist
                int tgtPos = 0;

                if (!d.ColumnPositions.ContainsKey(ro.TargetVar)) {
                    tgtPos = d.AddIntColumnWithData(ro.TargetVar, 0);
                } else {
                    tgtPos = d.ColumnPositions[ro.TargetVar];
                }

                for (int i = 0; i < rowCount; i++) {
                    int curval = (int)d.Data[rcdPos][i];
                    if (ro.Recodes.ContainsKey(curval)) {
                        d.Data[tgtPos][i] = ro.Recodes[curval];
                    } else {
                        // is there an "else" value we need to recode to?
                        if (ro.HasElseRecode) {
                            d.Data[tgtPos][i] = ro.ElseRecode;
                        }
                    }
                }
                commandOutput += "Variable " + ro.RecodeVar + " recoded into " + ro.TargetVar + "\n";
            } catch (Exception ex) {
                commandOutput += "Exception occurred: " + ex.Message + "\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }

            return commandOutput;
        }

        public string ExecuteRecodeRange(object op) {
            RecodeRangeOperation ro = (RecodeRangeOperation)op;

            string commandOutput = "";

            commandOutput = "Executing RecodeRange command: " + ro.OriginalCommandString + "\n";

            try {
                DataArray d = m_DataArrays[ro.TargetDataArray];

                int rowCount = d.RowCount();
                int rcdPos = d.ColumnPositions[ro.RecodeVar];

                //if (d.Columns[rcdPos].Type != ColumnType.Int) {
                //    throw new Exception("Recode variable is not an integer");
                //}

                // check for existence of target var & create if doesn't exist
                int tgtPos = 0;

                if (!d.ColumnPositions.ContainsKey(ro.TargetVar)) {
                    tgtPos = d.AddIntColumnWithData(ro.TargetVar, 0);
                } else {
                    tgtPos = d.ColumnPositions[ro.TargetVar];
                }

                for (int i = 0; i < rowCount; i++) {
                    double curval = Double.NaN;

                    if (d.Columns[rcdPos].Type == ColumnType.Int) {
                        curval = (int)d.Data[rcdPos][i];
                    } else {
                        curval = (double)d.Data[rcdPos][i];
                    }

                    foreach (var recode in ro.RecodeRangeItems) {
                        if (recode.NoMinBound && curval <= recode.HighBound) {
                            d.Data[tgtPos][i] = recode.RecodeValue;
                            break;
                        } else if (recode.NoMaxBound && curval >= recode.LowBound) {
                            d.Data[tgtPos][i] = recode.RecodeValue;
                            break;
                        } else if (curval >= recode.LowBound && curval <= recode.HighBound) {
                            d.Data[tgtPos][i] = recode.RecodeValue;
                            break;
                        } else if (recode.IsElse) {
                            d.Data[tgtPos][i] = recode.RecodeValue;
                            break;
                        }
                    }
                }

                commandOutput += "Variable " + ro.RecodeVar + " recoded into " + ro.TargetVar + "\n";
            } catch (Exception ex) {
                commandOutput += "Exception occurred: " + ex.Message + "\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }

            return commandOutput;
        }

        // this isn't used yet and isn't properly tested
        public string ExecuteRenameVars(object op) {
            RenameVarsOperation rvo = (RenameVarsOperation)op;

            string commandOutput = "";

            commandOutput = "Executing RenameVars command: " + rvo.OriginalCommandString + "\n";

            try {
                DataArray d = m_DataArrays[rvo.TargetDataArray];

                string renamedVars = "";

                /*
                 * Method:
                 * =======
                 * - Check to see current name exists and target name doesn't exist
                 * - Update the column and the column position dictionary with the new value
                 */

                foreach (var key in rvo.Renames.Keys) {
                    if (d.ColumnPositions.ContainsKey(key) && !d.ColumnPositions.ContainsKey(rvo.Renames[key])) {
                        int colPos = d.ColumnPositions[key];
                        d.Columns[colPos].Name = rvo.Renames[key];
                        d.ColumnPositions.Remove(key);
                        d.ColumnPositions.Add(rvo.Renames[key], colPos);

                        renamedVars += key + " => " + rvo.Renames[key] + ",";
                    }
                }

                renamedVars.TrimEnd(',');

                commandOutput += "The following renames were executed: " + renamedVars + "\n";
            } catch (Exception ex) {
                commandOutput += "Exception occurred: " + ex.Message + "\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }

            return commandOutput;
        }

        public string ExecuteSigTest(object op) {
            SigTestOperation sto = (SigTestOperation)op;

            string commandOutput = "";

            commandOutput = "Executing SigTest command: " + sto.OriginalCommandString + "\n";

            try {
                // should really check the valid school N first and if less than 5 then don't proceed (write out sysmis values to relevant vars)

                DataArray d = m_DataArrays[sto.TargetDataArray];

                int rowCount = d.RowCount();

                int tvalPos = d.AddDoubleColumn(sto.TargetTvalueVar);
                int indPos = d.AddIntColumn(sto.TargetIndicatorVar);

                // set the new columns to sysmis
                // NB the sig test command expects a data set with a single case, hence only setting the data for one case
                d.Data[tvalPos].Add(Double.NaN);
                d.Data[indPos].Add(0);

                // create a dictionary to lookup the column positions for the relevant cat vars
                Dictionary<int, int> colLookup = new Dictionary<int, int>();

                foreach (var cat in sto.Categories) {
                    string varname = sto.VarNameStem + "_" + cat;
                    colLookup.Add(cat, d.ColumnPositions[varname]);
                }

                // transfer across the var labels
                string varLabel = d.Columns[colLookup[sto.Categories[0]]].Description;

                d.Columns[tvalPos].Description = varLabel;
                d.Columns[indPos].Description = varLabel;

                Dictionary<int, double> schoolDataLookup = new Dictionary<int, double>();

                foreach (var cat in sto.Categories) {
                    schoolDataLookup.Add(cat, (double)d.Data[colLookup[cat]][0]);
                }

                // *****************************************************************************
                // check school valid N here & abort if less than 5 (this should also prevent divide by zero issues later on)
                // *****************************************************************************

                int schoolN = (int)d.Data[d.ColumnPositions[sto.SchoolNVar]][0];
                double validSchoolN = 0;

                double validSchSum = 0;

                foreach (var cat in sto.Categories) {
                    validSchSum += schoolDataLookup[cat];
                }

                validSchoolN = schoolN * validSchSum / 100;

                if (validSchoolN < 5) {
                    commandOutput += "Valid school N is not sufficient for sig testing for " + sto.VarNameStem + " (" + validSchoolN + ")\n";
                    return commandOutput;
                }

                // *****************************************************************************
                // calculate school mean score
                // *****************************************************************************

                double schoolMeanScore = 0;
                double nationalMeanScore = 0;

                double sumProduct = 0;
                double sumPct = 0;

                foreach (var cat in sto.Categories) {
                    sumProduct += (schoolDataLookup[cat] * sto.ScoreKeyLookup[cat]);
                    sumPct += schoolDataLookup[cat];
                }

                schoolMeanScore = sumProduct / sumPct;

                // *****************************************************************************
                // calculate national mean score
                // *****************************************************************************

                sumProduct = 0;
                sumPct = 0;

                foreach (var cat in sto.Categories) {
                    sumProduct += (sto.NationalDataLookup[cat] * sto.ScoreKeyLookup[cat]);
                    sumPct += sto.NationalDataLookup[cat];
                }

                nationalMeanScore = sumProduct / sumPct;

                // *****************************************************************************
                // now calculate (max score - mean score)^2 - these need to be stored in dictionaries
                // *****************************************************************************
                
                // school first
                Dictionary<int, double> step2Sch = new Dictionary<int, double>();

                foreach (var cat in sto.Categories) {
                    double scoreDiff = sto.ScoreKeyLookup[cat] - schoolMeanScore;
                    step2Sch.Add(cat, (scoreDiff * scoreDiff));
                }

                // national
                Dictionary<int, double> step2Nat = new Dictionary<int, double>();

                foreach (var cat in sto.Categories) {
                    double scoreDiff = sto.ScoreKeyLookup[cat] - nationalMeanScore;
                    step2Nat.Add(cat, (scoreDiff * scoreDiff));
                }

                // *****************************************************************************
                // now calculate school standard deviation
                // *****************************************************************************
                
                // sqrt((sch N / sch N -1) * sumproduct step 2/sum %)
                sumProduct = 0;
                sumPct = 0;

                foreach (var cat in sto.Categories) {
                    sumProduct += (schoolDataLookup[cat] * step2Sch[cat]);
                    sumPct += schoolDataLookup[cat];
                }

                double schStdDev = Math.Sqrt((validSchoolN / (validSchoolN - 1)) * (sumProduct / sumPct));

                // *****************************************************************************
                // nat std dev (also need to calculate valid national N here
                // *****************************************************************************
                double validNationalN = 0;

                double validNatSum = 0;

                foreach (var cat in sto.Categories) {
                    validNatSum += sto.NationalDataLookup[cat];
                }

                validNationalN = sto.NationalN * validNatSum / 100;

                sumProduct = 0;
                sumPct = 0;

                foreach (var cat in sto.Categories) {
                    sumProduct += (sto.NationalDataLookup[cat] * step2Nat[cat]);
                    sumPct += sto.NationalDataLookup[cat];
                }

                double natStdDev = Math.Sqrt((validNationalN / (validNationalN - 1)) * (sumProduct / sumPct));

                // *****************************************************************************
                // standard error, score difference, initial t-value, degrees of freedom & significance
                // *****************************************************************************

                /*
                 * Tom's original comment from the Excel templates:
                 * Ensure that this is never smaller than 0.288675.
                 * 
                 * 0.288675 is chosen as it is the standard deviation of a uniformly distributed random variable 
                 * between -0.5 and 0.5. This means that if (for example) 100% of respondents say "agree" we treat 
                 * them as if they are evenly distributed between scores of 3.5 and 4.5 rather than treating them 
                 * as if they all had identical opions and that there is zero standard deviation in opinions 
                 * within the school.
                 *
                 * This is particularly important as it avoids overstating statistical significance when we have 
                 * very small numbers of respondents (who may occasionally all say the same thing).
                 */ 

                double schStdErr = Math.Max(schStdDev, sto.StdDevFloor) / Math.Sqrt(validSchoolN);
                double natStdErr = sto.NationalDesignEffect * natStdDev / Math.Sqrt(validNationalN);

                double pooledStdErr = Math.Sqrt((schStdErr * schStdErr) + (natStdErr * natStdErr));

                double scoreDifference = schoolMeanScore - nationalMeanScore;
                double tvalue = Math.Abs(scoreDifference / pooledStdErr);

                double degFreedom = validSchoolN - 1;

                var dist = new StudentDistribution(Math.Round(degFreedom,0));
                double significance = dist.RightProbability(tvalue) * 2;

                // *****************************************************************************
                // preliminary indicator - agree more / disagree more / no sig diff (but doesn't take into account +ve/-ve question
                // *****************************************************************************

                // 1 = agree more, -1 = disagree more, 0 = no sig diff
                int prelimIndicator = 0;

                if (significance < 0.05 && schoolMeanScore > nationalMeanScore) {
                    prelimIndicator = 1;
                } else if (significance < 0.05 && schoolMeanScore < nationalMeanScore) {
                    prelimIndicator = -1;
                } else {
                    prelimIndicator = 0;
                }

                int scoreDifferenceSign = (scoreDifference > 0) ? 1 : -1;

                // *****************************************************************************
                // final values for question
                // *****************************************************************************

                // signed t value has unique value added to avoid ties in ranking
                // get the column position of the first category & use this
                int uniqueValue = colLookup[sto.Categories[0]];

                double signedTValue = scoreDifferenceSign * sto.QuestionIndicator * Math.Round(tvalue, 8) + (0.000000000001 * uniqueValue);
                                
                int finalIndicator = prelimIndicator * sto.QuestionIndicator;

                d.Data[tvalPos][0] = signedTValue;
                d.Data[indPos][0] = finalIndicator;

                /*
                commandOutput += "School mean score = " + schoolMeanScore + "\n";
                commandOutput += "National mean score = " + nationalMeanScore + "\n";
                commandOutput += "School std dev = " + schStdDev + "\n";
                commandOutput += "National std dev = " + natStdDev + "\n";
                commandOutput += "School std err = " + schStdErr + "\n";
                commandOutput += "National std err = " + natStdErr + "\n";
                commandOutput += "Pooled std err = " + pooledStdErr + "\n";
                commandOutput += "Score difference = " + scoreDifference + "\n";
                commandOutput += "T-value = " + tvalue + "\n";
                commandOutput += "Significance = " + significance + "\n";
                
                commandOutput += "Signed T-value = " + signedTValue + "\n";
                commandOutput += "Significance indicator = " + finalIndicator + "\n\n";
                 */
                commandOutput += "Varname,sch mean,nat mean,sch stddev,nat stddev,sch stderr,nat stderr,pooled err,score diff,tvalue,sig,signed t,final ind\n";
                commandOutput += sto.VarNameStem + "," + schoolMeanScore + "," + nationalMeanScore + "," + schStdDev + "," + natStdDev + "," + schStdErr + "," + natStdErr + "," + pooledStdErr + "," + scoreDifference + "," + tvalue + "," + significance + "," + signedTValue + "," + finalIndicator + "\n";
            } catch (Exception ex) {
                commandOutput += "Exception occurred: " + ex.Message + "\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }

            return commandOutput;
        }

        // done
        public string ExecuteSplitVar(object op) {
            SplitVarOperation svo = (SplitVarOperation)op;

            string commandOutput = "";

            commandOutput = "Executing SplitVar command: " + svo.OriginalCommandString + "\n";

            try {
                DataArray d = m_DataArrays[svo.TargetDataArray];

                /* Method:
                 * =======
                 * - Get the column position of target var and check it exists and an int (otherwise throw)
                 * - iterate through the splitvalues and create the columns
                 * - use the returned column position to create a dictionary of splitvalues => column pos
                 * - create the missing value column and add it to the dictionary with int32.minvalue as the key
                 * - iterate over all the rows
                 * - set all the values for the new columns to zero? (by iterating over dictionary
                 * - check the filter var for filter values for each row if exists - i
                 */

                int rowCount = d.RowCount();
                int colPos = d.ColumnPositions[svo.TargetVar];

                if (d.Columns[colPos].Type != ColumnType.Int) {
                    throw new Exception("Target variable is not an integer");
                }

                Dictionary<int, int> svlu = new Dictionary<int,int>();

                Column c;

                string newVarLabel = string.Empty;

                if (!string.IsNullOrEmpty(d.Columns[colPos].Description)) {
                    newVarLabel = d.Columns[colPos].Description;
                }

                foreach (var s in svo.SplitValues) {
                    c = new Column();
                    c.Name = svo.TargetVar + "_" + s;
                    c.Type = ColumnType.Int;
                    c.IsCategorical = false;
                    c.Description = newVarLabel;
                    svlu.Add(s, d.AddColumn(c));
                }

                // add the missing value column
                c = new Column();
                c.Name = svo.TargetVar + "_" + svo.MissingValue;
                c.Type = ColumnType.Int;
                c.IsCategorical = false;
                svlu.Add(Int32.MinValue, d.AddColumn(c));

                int filterCol = 0;

                if (svo.HasFilter) {
                    filterCol = d.ColumnPositions[svo.FilterVar];
                }

                for (int i = 0; i < rowCount; i++) {
                    bool okToSplit = true;

                    if (svo.HasFilter) {
                        okToSplit = (svo.FilterValues.ContainsKey((int)d.Data[filterCol][i]));
                    }

                    if (okToSplit) {
                        // set all the values to zero
                        foreach (var k in svlu.Keys) {
                            d.Data[svlu[k]].Add(0);
                        }

                        int curval = (int)d.Data[colPos][i];

                        // if we have a value that isn't in the lookup, use the missing value instead
                        if (!svlu.ContainsKey(curval)) {
                            d.Data[svlu[Int32.MinValue]][i] = 100;
                        } else {
                            d.Data[svlu[curval]][i] = 100;
                        }
                    } else {
                        foreach (var k in svlu.Keys) {
                            d.Data[svlu[k]].Add(Int32.MinValue);
                        }
                    }
                }

                commandOutput += "Variable " + svo.TargetVar + " has been split into separate variables for each value\n";
            } catch (Exception ex) {
                commandOutput += "Exception occurred: " + ex.Message + "\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }

            return commandOutput;
        }

        // done
        public string ExecuteSort(object op) {
            SortOperation so = (SortOperation)op;

            string commandOutput = "";

            commandOutput = "Executing Sort command: " + so.OriginalCommandString + "\n";

            try {
                DataArray d = m_DataArrays[so.TargetDataArray];

                int rowCount = d.RowCount();
                int colPos = d.ColumnPositions[so.TargetVar];

                int[] sortData = new int[rowCount];
                int[] reorderRef = new int[rowCount];

                for (int i = 0; i < rowCount; i++) {
                    sortData[i] = (int)d.Data[colPos][i];
                    reorderRef[i] = i;
                }

                Array.Sort(sortData, reorderRef);

                for (int j = 0; j < d.Columns.Count; j++) {
                    List<object> currentData = d.Data[j];
                    List<object> sortedData = new List<object>();

                    foreach (var k in reorderRef) {
                        sortedData.Add(currentData[k]);
                    }

                    d.Data[j] = sortedData;
                }

                commandOutput += "Dataset sorted by " + so.TargetVar + "\n";
            } catch (Exception ex) {
                commandOutput += "Exception occurred: " + ex.Message + "\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }

            return commandOutput;
        }

        // Implement expression symbols for ExecuteComputeExpr
        protected void ProcessSymbol(object sender, SymbolEventArgs e) {
            DataArray d = m_DataArrays[e.DataArrayName];

            if (d.ColumnPositions.ContainsKey(e.Name)) {
                int colPos = d.ColumnPositions[e.Name];
                e.Result = (int)d.Data[colPos][e.Row];
            } else {
                e.Status = SymbolStatus.UndefinedSymbol;
            }
        }

        // done
        public string ExecuteSetRemoveFlag(object op) {
            SetRemoveFlagOperation srfo = (SetRemoveFlagOperation)op;

            string commandOutput = "";

            commandOutput = "Executing SetRemoveFlag command: " + srfo.OriginalCommandString + "\n";

            try {
                DataArray d = m_DataArrays[srfo.TargetDataArray];

                int rowCount = d.RowCount();
                int colPos = 0;

                if (d.ColumnPositions.ContainsKey(srfo.TargetVar)) {
                    colPos = d.ColumnPositions[srfo.TargetVar];
                } else {
                    Column col = new Column();
                    col.Name = srfo.TargetVar;
                    col.IsCategorical = false;
                    col.Type = ColumnType.Int;
                    colPos = d.AddColumn(col);

                    // default flag to 0
                    for (int i = 0; i < rowCount; i++) {
                        d.Data[colPos].Add(0);
                    }
                }

                foreach (var ri in srfo.RecordsToRemove) {
                    d.Data[colPos][ri] = 1;
                }

                commandOutput += "Variable " + srfo.TargetVar + " created and records to be removed have been flagged\n";
            } catch (Exception ex) {
                commandOutput += "Exception occurred: " + ex.Message + "\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }

            return commandOutput;
        }

        // done
        public string ExecuteRemoveRecords(object op) {
            RemoveRecordsOperation rro = (RemoveRecordsOperation)op;

            string commandOutput = "";

            commandOutput = "Executing RemoveRecords command: " + rro.OriginalCommandString + "\n";

            try {
                DataArray d = m_DataArrays[rro.TargetDataArray];

                int rowCount = d.RowCount();
                if (d.ColumnPositions.ContainsKey(rro.TargetVar)) {
                    int colPos = d.ColumnPositions[rro.TargetVar];
                    int recordsRemoved = 0;

                    // iterate over rows backwards otherwise removing rows whilst iterating causes loop counter to go out of sync
                    for (int i = rowCount - 1; i >= 0; i--) {
                        if ((int)d.Data[colPos][i] == 1) {
                            for (int j = 0; j < d.Columns.Count; j++) {
                                d.Data[j].RemoveAt(i);
                            }
                            recordsRemoved++;
                        }
                    }

                    commandOutput += recordsRemoved.ToString() + " records flaggged in " + rro.TargetVar + " have been removed\n";
                } else {
                    commandOutput += rro.TargetVar + " does not exist, no records have been removed\n";
                }
            } catch (Exception ex) {
                commandOutput += "Exception occurred: " + ex.Message + "\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }

            return commandOutput;
        }

        // done
        public string ExecuteKeepVars(object op) {
            KeepVarsOperation kvo = (KeepVarsOperation)op;

            string commandOutput = "";

            commandOutput = "Executing KeepVars command: " + kvo.OriginalCommandString + "\n";

            try {
                DataArray d = m_DataArrays[kvo.TargetDataArray];

                /*
                 * Method:
                 * =======
                 * - determine the current indices of the vars to be kept in keepColIdx (list<int>)
                 * - create a new Column list, ColumnPosition dictionary & Data list
                 * - run through keepColIdx and use the indices to populate the new objects
                 * - replace the existing objects with the new ones
                 */

                // if kvo.CreateNewArray == true then we should perform the keep vars operation on a new dataarray

                List<int> keepColIdx = new List<int>();

                foreach (var cn in kvo.VarsToKeep) {
                    keepColIdx.Add(d.ColumnPositions[cn]);
                }

                List<Column> newColumns = new List<Column>();
                Dictionary<string, int> newColumnPositions = new Dictionary<string, int>();
                List<List<object>> newData = new List<List<object>>();

                foreach (var idx in keepColIdx) {
                    newColumns.Add(d.Columns[idx]);
                    newColumnPositions.Add(d.Columns[idx].Name, newColumns.Count - 1);
                    newData.Add(d.Data[idx]);
                }

                // are we applying the results to a new data array or an existing one?
                if (kvo.CreateNewArray) {
                    DataArray newDA = new DataArray();

                    newDA.Columns = newColumns;
                    newDA.ColumnPositions = newColumnPositions;
                    newDA.Data = newData;

                    m_DataArrays.Add(kvo.NewDataArray, newDA);
                } else {
                    d.Columns = newColumns;
                    d.ColumnPositions = newColumnPositions;
                    d.Data = newData;
                }

                commandOutput += "Dataset variables reordered.\n";
            } catch (Exception ex) {
                commandOutput += "Exception occurred: " + ex.Message + "\n";
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

        // done
        public string ExecuteExport(object op) {
            ExportOperation eo = (ExportOperation)op;

            string commandOutput = "";

            commandOutput = "Executing Export command: " + eo.OriginalCommandString + "\n";

            try {
                DataArray d = m_DataArrays[eo.TargetDataArray];

                string filePath;

                if (eo.Filename.StartsWith("!")) {
                    filePath = m_fileHandles[eo.Filename];
                } else {
                    filePath = eo.Filename;
                }

                if (eo.Mode == ExportMode.New) {
                    if (eo.FileType == ExportType.Csv) {
                        d.Export(filePath);
                    } else {
                        d.ExportToSpss(filePath);
                    }
                } else {
                    if (eo.FileCopyReqd) {
                        string fileToCopy;
                        if (eo.FileToCopy.StartsWith("!")) {
                            fileToCopy = m_fileHandles[eo.FileToCopy];
                        } else {
                            fileToCopy = eo.FileToCopy;
                        }
                        File.Copy(fileToCopy, filePath, true);
                    }
                    d.AppendToCsv(filePath);
                }

                string exportMode = (eo.Mode == ExportMode.New) ? "exported" : "appended";

                commandOutput += "Dataset " + eo.TargetDataArray + " " + exportMode + " to " + filePath + "\n";
            } catch (Exception ex) {
                commandOutput += "Exception occurred: " + ex.Message + "\n";
                m_errorStatus.ErrorLogged = true;
                m_errorStatus.OffendingCommand = commandOutput;
                m_errorStatus.CaughtException = ex;
            }

            return commandOutput;
        }
    }
}
