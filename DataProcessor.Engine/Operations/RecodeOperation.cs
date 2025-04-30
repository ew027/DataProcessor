using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataProcessor.Engine.Operations {
    public class RecodeOperation : Operation {
        public string OriginalCommandString { get; set; }

        public string TargetDataArray { get; set; }
        public string RecodeVar { get; set; }

        public Dictionary<int, int> Recodes { get; set; }

        public bool HasElseRecode { get; set; }
        public int ElseRecode { get; set; }

        public bool HasFilter { get; set; }
        public string FilterVar { get; set; }
        public Dictionary<int, int> FilterValues { get; set; }

        public RecodeOperation(string arguments) {
            OriginalCommandString = arguments;
            string[] argElements = arguments.Split('|');

            Recodes = new Dictionary<int, int>();

            TargetDataArray = argElements[0];
            RecodeVar = argElements[1];

            string recodeOps = argElements[2];

            string[] recodeOpsArray = recodeOps.Split(';');

            // assume no "else" recode
            HasElseRecode = false;

            foreach (var ro in recodeOpsArray) {
                string[] vals = ro.Split('=');
                int recodeValue;
                if (vals[1] == "sysmis") {
                    recodeValue = Int32.MinValue;
                } else {
                    recodeValue = Int32.Parse(vals[1]);
                }

                if (vals[0] == "else") {
                    HasElseRecode = true;
                    ElseRecode = recodeValue;
                } else if (vals[0] == "sysmis") {
                    Recodes.Add(Int32.MinValue, recodeValue);
                } else {
                    Recodes.Add(Int32.Parse(vals[0]), recodeValue);
                }
            }

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
            return "ExecuteRecode";
        }
    }
}
