using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataProcessor.Engine.Operations {
    public class BlankSurveyOperation : Operation {
        public string OriginalCommandString { get; set; }

        public string TargetDataArray { get; set; }

        // variable to hold blank record flag
        public string TargetVar { get; set; }

        // start & end vars to check
        public List<string> Variables { get; set; }
        
        public BlankSurveyOperation(string arguments) {
            OriginalCommandString = arguments;
            string[] argElements = arguments.Split('|');

            TargetDataArray = argElements[0];
            TargetVar = argElements[1];

            string[] vars = argElements[2].Split(',');

            Variables = new List<string>();

            foreach (string varname in vars) {
                Variables.Add(varname);
            }
        }

        public override string GetExecuteMethod() {
            return "ExecuteBlankSurvey";
        }
    }
}
