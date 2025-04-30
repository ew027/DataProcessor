using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataProcessor.Engine.Operations {
    public class ComputeExprOperation : Operation {
        public string OriginalCommandString { get; set; }

        public string TargetDataArray { get; set; }
        public string TargetVar { get; set; }
        public string Expression { get; set; }

        public bool HasFilter { get; set; }
        public string FilterVar { get; set; }
        public Dictionary<int, int> FilterValues { get; set; }
        
        public ComputeExprOperation(string arguments) {
            OriginalCommandString = arguments;
            string[] argElements = arguments.Split('|');

            TargetDataArray = argElements[0];
            TargetVar = argElements[1];
            Expression = argElements[2];

            if (argElements.Length > 3) {
                HasFilter = true;
                string[] filterArray = argElements[3].Split('=');
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
            return "ExecuteComputeExpr";
        }
    }
}
