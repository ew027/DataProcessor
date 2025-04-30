using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataProcessor.Engine.Operations {
    public class KeepVarsOperation : Operation {
        public string OriginalCommandString { get; set; }

        public string TargetDataArray { get; set; }
        public List<string> VarsToKeep { get; set; }

        public string NewDataArray { get; set; }
        public bool CreateNewArray { get; set; }
        
        public KeepVarsOperation(string arguments) {
            OriginalCommandString = arguments;

            string[] argElements = arguments.Split('|');

            TargetDataArray = argElements[0];
            
            string varsToKeepString = argElements[1];
            string[] varsToKeepArray = varsToKeepString.Split(',');

            VarsToKeep = new List<string>(varsToKeepArray);

            if (argElements.Length > 2) {
                NewDataArray = argElements[2];
                CreateNewArray = true;
            } else {
                NewDataArray = string.Empty;
                CreateNewArray = false;
            }
        }

        public override string GetExecuteMethod() {
            return "ExecuteKeepVars";
        }
    }
}
