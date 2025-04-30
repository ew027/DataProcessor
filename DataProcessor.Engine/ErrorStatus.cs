using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataProcessor.Engine {
    public class ErrorStatus {
        public string OffendingCommand { get; set; }
        public Exception CaughtException { get; set; }
        public bool ErrorLogged { get; set; }
    }
}
