using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataProcessor.Engine.Operations {
    public class AddVariableLabelOperation : Operation {
        public string OriginalCommandString { get; set; }

        public string TargetDataArray { get; set; }

        public string TargetVar { get; set; }
        public string VariableLabel { get; set; }

        public AddVariableLabelOperation(string arguments) {
            OriginalCommandString = arguments;
            string[] argElements = arguments.Split('|');

            TargetDataArray = argElements[0];
            
            TargetVar = argElements[1];
            VariableLabel = argElements[2];
        }

        public override string GetExecuteMethod() {
            return "ExecuteAddVariableLabel";
        }
    }
}
