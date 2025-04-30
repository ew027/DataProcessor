using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataProcessor.Engine.Operations {
    public class GenderRecodeOperation : Operation {
        public string OriginalCommandString { get; set; }

        public string TargetDataArray { get; set; }

        // variable where flag will be set if gender has been recoded - needs to be created already
        public string TargetVar { get; set; }

        // variable containing the gender data
        public string GenderVar { get; set; }
        
        // gender value to recode to
        public int Gender { get; set; }

        // yeargroup filter
        public bool HasFilter { get; set; }
        public string FilterVar { get; set; }
        public Dictionary<int, int> FilterValues { get; set; }

        public GenderRecodeOperation(string arguments) {
            OriginalCommandString = arguments;
            string[] argElements = arguments.Split('|');

            TargetDataArray = argElements[0];
            TargetVar = argElements[1];
            GenderVar = argElements[2];
            Gender = Int32.Parse(argElements[3]);

            if (argElements.Length > 4) {
                HasFilter = true;
                string[] filterArray = argElements[4].Split('=');
                FilterVar = filterArray[0];

                string[] filterValsArray = filterArray[1].Split(',');

                FilterValues = new Dictionary<int, int>();

                foreach (var f in filterValsArray) {
                    FilterValues.Add(Int32.Parse(f), 1);
                }
            } else {
                HasFilter = false;
            }

        }

        public override string GetExecuteMethod() {
            return "ExecuteGenderRecode";
        }
    }
}
