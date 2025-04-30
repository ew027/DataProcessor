using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataProcessor.Engine.Operations {
    public class RecodeRangeItem {
        public double LowBound { get; set; }
        public double HighBound { get; set; }
        public bool NoMinBound { get; set; }
        public bool NoMaxBound { get; set; }
        public int RecodeValue { get; set; }
        public bool IsElse { get; set; }

        public RecodeRangeItem() {
            LowBound = 0;
            HighBound = 0;
            NoMinBound = false;
            NoMaxBound = false;
            IsElse = false;
        }
    }
}
