using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataProcessor.Engine.Operations {
    public class CreateIntVarOperation : Operation {
        public string OriginalCommandString { get; set; }

        public string TargetDataArray { get; set; }

        public string NewVarName { get; set; }
        public int Value { get; set; }
        
        public CreateIntVarOperation(string arguments) {
            OriginalCommandString = arguments;
            string[] argElements = arguments.Split('|');

            TargetDataArray = argElements[0];
            
            NewVarName = argElements[1];

            Value = Int32.Parse(argElements[2]);

        }

        public override string GetExecuteMethod() {
            return "ExecuteCreateIntVar";
        }
    }
}
