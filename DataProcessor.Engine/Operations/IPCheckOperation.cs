using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataProcessor.Engine.Operations {
    public class IPCheckOperation : Operation {
        public string OriginalCommandString { get; set; }

        public string TargetDataArray { get; set; }

        // flag variable
        public string TargetVar { get; set; }
        
        // variable containing IP address
        public string IPVar { get; set; }
        
        public IPCheckOperation(string arguments) {
            OriginalCommandString = arguments;
            string[] argElements = arguments.Split('|');

            TargetDataArray = argElements[0];
            TargetVar = argElements[1];
            IPVar = argElements[2];
        }

        public override string GetExecuteMethod() {
            return "ExecuteIPCheck";
        }
    }
}
