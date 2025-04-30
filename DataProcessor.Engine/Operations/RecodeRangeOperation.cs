using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataProcessor.Engine.Operations {
    public class RecodeRangeOperation : Operation {
        public string OriginalCommandString { get; set; }

        public string TargetDataArray { get; set; }
        public string TargetVar { get; set; }
        public string RecodeVar { get; set; }
        
        public List<RecodeRangeItem> RecodeRangeItems { get; set; }

        //public bool HasElseRecode { get; set; }
        //public int ElseRecode { get; set; }

        public RecodeRangeOperation(string arguments) {
            OriginalCommandString = arguments;
            string[] argElements = arguments.Split('|');

            RecodeRangeItems = new List<RecodeRangeItem>();

            TargetDataArray = argElements[0];
            TargetVar = argElements[1];
            RecodeVar = argElements[2];

            string recodeOps = argElements[3];

            string[] recodeOpsArray = recodeOps.Split(';');

            foreach (var ro in recodeOpsArray) {
                RecodeRangeItem rangeItem = new RecodeRangeItem();

                string[] vals = ro.Split('=');
                int recodeValue;
                if (vals[1] == "sysmis") {
                    recodeValue = Int32.MinValue;
                } else {
                    recodeValue = Int32.Parse(vals[1]);
                }

                rangeItem.RecodeValue = recodeValue;

                if (vals[0] == "else") {
                    rangeItem.IsElse = true;
                } else {
                    // now split on "~"
                    // min / max can be used instead of acutal min / max values
                    string[] ranges = vals[0].Split('~');
                    if (ranges[0] == "min") {
                        rangeItem.NoMinBound = true;
                    } else {
                        rangeItem.LowBound = Double.Parse(ranges[0]);
                    }

                    if (ranges[1] == "max") {
                        rangeItem.NoMaxBound = true;
                    } else {
                        rangeItem.HighBound = Double.Parse(ranges[1]);
                    }
                }

                RecodeRangeItems.Add(rangeItem);
            }
        }

        public override string GetExecuteMethod() {
            return "ExecuteRecodeRange";
        }
    }
}
