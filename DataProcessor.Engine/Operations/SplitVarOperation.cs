using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataProcessor.Engine.Operations {
    public class SplitVarOperation : Operation {
        public string OriginalCommandString { get; set; }

        public string TargetDataArray { get; set; }
        public string TargetVar { get; set; }
        public List<int> SplitValues { get; set; }
        public int MissingValue { get; set; }
        public bool HasFilter { get; set; }
        public string FilterVar { get; set; }
        public Dictionary<int, int> FilterValues { get; set; }
        
        public SplitVarOperation(string arguments) {
            OriginalCommandString = arguments;
            string[] argElements = arguments.Split('|');

            TargetDataArray = argElements[0];
            TargetVar = argElements[1];

            string[] valuesArray = argElements[2].Split(',');

            SplitValues = new List<int>();

            foreach (var r in valuesArray) {
                SplitValues.Add(Int32.Parse(r));
            }

            MissingValue = Int32.Parse(argElements[3]);

            if (argElements.Length > 4) {
                HasFilter = true;
                string[] filterArray = argElements[4].Split('=');
                FilterVar = filterArray[0];

                string[] filterValsArray = filterArray[1].Split(',');

                FilterValues = new Dictionary<int, int>();

                foreach (var f in filterValsArray) {
                    FilterValues.Add(Int32.Parse(f), 1);
                }
            } else {
                HasFilter = false;
            }
        }

        public override string GetExecuteMethod() {
            return "ExecuteSplitVar";
        }
    }
}
