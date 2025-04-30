using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataProcessor.Engine.Operations {
    public class RecodeIntoOperation : Operation {
        public string OriginalCommandString { get; set; }

        public string TargetDataArray { get; set; }
        public string TargetVar { get; set; }
        public string RecodeVar { get; set; }
        
        public Dictionary<int, int> Recodes { get; set; }

        public bool HasElseRecode { get; set; }
        public int ElseRecode { get; set; }

        public RecodeIntoOperation(string arguments) {
            OriginalCommandString = arguments;
            string[] argElements = arguments.Split('|');

            Recodes = new Dictionary<int, int>();

            TargetDataArray = argElements[0];
            TargetVar = argElements[1];
            RecodeVar = argElements[2];

            string recodeOps = argElements[3];

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
                } else {
                    Recodes.Add(Int32.Parse(vals[0]), recodeValue);
                }
            }
        }

        public override string GetExecuteMethod() {
            return "ExecuteRecodeInto";
        }
    }
}
