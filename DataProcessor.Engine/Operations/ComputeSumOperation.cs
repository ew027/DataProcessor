using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataProcessor.Engine.Operations {
    public class ComputeSumOperation : Operation {
        public string OriginalCommandString { get; set; }

        public string TargetDataArray { get; set; }
        public string TargetVar { get; set; }
        public List<string> VarsToSum { get; set; }

        public ComputeSumOperation(string arguments) {
            OriginalCommandString = arguments;

            string[] argElements = arguments.Split('|');

            TargetDataArray = argElements[0];
            TargetVar = argElements[1];

            string varsToSumString = argElements[2];
            string[] varsToSumArray = varsToSumString.Split(',');

            VarsToSum = new List<string>(varsToSumArray);
        }

        public override string GetExecuteMethod() {
            return "ExecuteComputeSum";
        }
    }
}
