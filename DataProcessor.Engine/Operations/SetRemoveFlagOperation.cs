using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataProcessor.Engine.Operations {
    public class SetRemoveFlagOperation : Operation {
        public string OriginalCommandString { get; set; }

        public string TargetDataArray { get; set; }
        public string TargetVar { get; set; }
        public List<int> RecordsToRemove { get; set; }

        public SetRemoveFlagOperation(string arguments) {
            OriginalCommandString = arguments;
            string[] argElements = arguments.Split('|');

            TargetDataArray = argElements[0];
            TargetVar = argElements[1];

            string[] recordsArray = argElements[2].Split(',');

            RecordsToRemove = new List<int>();

            foreach (var r in recordsArray) {
                RecordsToRemove.Add(Int32.Parse(r));
            }
        }

        public override string GetExecuteMethod() {
            return "ExecuteSetRemoveFlag";
        }
    }
}
