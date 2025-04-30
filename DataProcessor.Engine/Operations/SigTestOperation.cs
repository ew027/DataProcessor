using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataProcessor.Engine.Operations {
    public class SigTestOperation : Operation {
        public string OriginalCommandString { get; set; }

        public string TargetDataArray { get; set; } // 0

        // Example command: SigTest|Dataset|
        public string TargetTvalueVar { get; set; } // 1
        public string TargetIndicatorVar { get; set; } // 2

        public string VarNameStem { get; set; } // 3

        public List<int> Categories { get; set; }

        public Dictionary<int, int> ScoreKeyLookup { get; set; } // 4
        public Dictionary<int, double> NationalDataLookup { get; set; } // 5

        public int NationalN { get; set; } // 6

        public string SchoolNVar { get; set; } // 7

        public int QuestionIndicator { get; set; } // 8 +/1 depending on whether positive/negative question

        public int NationalDesignEffect { get; set; } // 9

        public double StdDevFloor { get; set; } // 10

        
        public SigTestOperation(string arguments) {
            OriginalCommandString = arguments;
            string[] argElements = arguments.Split('|');

            Categories = new List<int>();
            ScoreKeyLookup = new Dictionary<int, int>();
            NationalDataLookup = new Dictionary<int, double>();

            TargetDataArray = argElements[0];
            TargetTvalueVar = argElements[1];
            TargetIndicatorVar = argElements[2];

            VarNameStem = argElements[3];

            string[] scoreKeyArray = argElements[4].Split(',');

            foreach (var item in scoreKeyArray) {
                string[] parts = item.Split('=');
                ScoreKeyLookup.Add(Int32.Parse(parts[0]), Int32.Parse(parts[1]));
                Categories.Add(Int32.Parse(parts[0]));
            }

            string[] natDataArray = argElements[5].Split(',');

            foreach (var item in natDataArray) {
                string[] parts = item.Split('=');
                NationalDataLookup.Add(Int32.Parse(parts[0]), Double.Parse(parts[1]));
            }

            NationalN = Int32.Parse(argElements[6]);

            // name of variable containing N (usually TotalN if data created by aggregate command)
            SchoolNVar = argElements[7];

            // 1 = positive question (good to agree), -1 = negative (bad to agree)
            QuestionIndicator = Int32.Parse(argElements[8]);

            // default is 3
            NationalDesignEffect = Int32.Parse(argElements[9]);

            // default is 0.288675 - see comment in execute method
            StdDevFloor = Double.Parse(argElements[10]);

        }

        public override string GetExecuteMethod() {
            return "ExecuteSigTest";
        }
    }
}
