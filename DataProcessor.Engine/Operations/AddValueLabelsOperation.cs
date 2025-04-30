using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataProcessor.Engine.Operations {
    public class AddValueLabelsOperation : Operation {
        public string OriginalCommandString { get; set; }

        public string TargetDataArray { get; set; }

        public string TargetVar { get; set; }
        public Dictionary<int, string> ValueLabels { get; set; }
        
        public AddValueLabelsOperation(string arguments) {
            OriginalCommandString = arguments;
            string[] argElements = arguments.Split('|');

            TargetDataArray = argElements[0];
            
            TargetVar = argElements[1];

            ValueLabels = new Dictionary<int,string>();

            string[] labels = argElements[2].Split(';');

            foreach (var label in labels) {
                string[] parts = label.Split('=');

                ValueLabels.Add(Int32.Parse(parts[0]), parts[1]);
            }
        }

        public override string GetExecuteMethod() {
            return "ExecuteAddValueLabels";
        }
    }
}
