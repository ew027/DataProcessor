using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataProcessor.Engine.Operations {
    public class AboveBelowOperation : Operation {
        public string OriginalCommandString { get; set; }

        public string TargetDataArray { get; set; }

        // variable containing value to check
        public string TargetVar { get; set; }

        // new vars to hold above/below indicators
        public string AboveVar { get; set; }
        public string BelowVar { get; set; }
        
        public AboveBelowOperation(string arguments) {
            OriginalCommandString = arguments;
            string[] argElements = arguments.Split('|');

            TargetDataArray = argElements[0];
            TargetVar = argElements[1];
            AboveVar = argElements[2];
            BelowVar = argElements[3];
        }

        public override string GetExecuteMethod() {
            return "ExecuteAboveBelow";
        }
    }
}
