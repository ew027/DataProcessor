using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataProcessor.Engine.Operations {
    public class RenameVarsOperation : Operation {
        public string OriginalCommandString { get; set; }

        public string TargetDataArray { get; set; }
        
        public Dictionary<string, string> Renames { get; set; }

        public RenameVarsOperation(string arguments) {
            OriginalCommandString = arguments;
            string[] argElements = arguments.Split('|');

            Renames = new Dictionary<string, string>();

            TargetDataArray = argElements[0];
            
            string renameOps = argElements[1];

            string[] renameOpsArray = renameOps.Split(';');

            foreach (var ro in renameOpsArray) {
                string[] vals = ro.Split('=');
                Renames.Add(vals[0], vals[1]);
            }
        }

        public override string GetExecuteMethod() {
            return "ExecuteRenameVars";
        }
    }
}
