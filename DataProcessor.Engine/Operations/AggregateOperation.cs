using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataProcessor.Engine.Operations {
    public class AggregateOperation : Operation {
        public string OriginalCommandString { get; set; }

        public string TargetDataArray { get; set; }
        public string NewDataArray { get; set; }

        public string BreakVar { get; set; }

        public List<string> KeepColumns { get; set; }
        public List<string> MeanColumns { get; set; }
        public List<string> CountColumns { get; set; }

        // not implemented
        public List<string> TotalColumns { get; set; }

        public AggregateOperation(string arguments) {
            OriginalCommandString = arguments;
            string[] argElements = arguments.Split('|');

            TargetDataArray = argElements[0];
            NewDataArray = argElements[1];
            BreakVar = argElements[2];

            string[] keepColArray = argElements[3].Split(',');
            string[] meanColArray = argElements[4].Split(',');

            KeepColumns = new List<string>(keepColArray);
            MeanColumns = new List<string>(meanColArray);

            if (argElements.Length > 5) {
                string[] countColArray = argElements[5].Split(',');
                CountColumns = new List<string>(countColArray);
            } else {
                // just create an empty list
                CountColumns = new List<string>();
            }
        }

        public override string GetExecuteMethod() {
            return "ExecuteAggregate";
        }
    }
}
