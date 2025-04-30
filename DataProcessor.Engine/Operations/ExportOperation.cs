using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataProcessor.Engine.Operations {
    public class ExportOperation : Operation {
        public string OriginalCommandString { get; set; }

        public string TargetDataArray { get; set; }

        public ExportMode Mode { get; set; }
        public ExportType FileType { get; set; }

        // Filename to export to (should include path)
        public string Filename { get; set; }

        public string FileToCopy { get; set; }

        public bool FileCopyReqd { get; set; }
        
        public ExportOperation(string arguments) {
            OriginalCommandString = arguments;
            string[] argElements = arguments.Split('|');

            TargetDataArray = argElements[0];

            Mode = (argElements[1] == "Append") ? ExportMode.Append : ExportMode.New;
            FileType = (argElements[2] == "csv") ? ExportType.Csv : ExportType.Spss;

            Filename = argElements[3];

            if (Mode == ExportMode.Append && argElements.Length > 4) {
                FileToCopy = argElements[4];
                FileCopyReqd = true;
            } else {
                FileCopyReqd = false;
            }
        }

        public override string GetExecuteMethod() {
            return "ExecuteExport";
        }
    }

    public enum ExportMode {
        New,
        Append
    }

    public enum ExportType {
        Csv,
        Spss
    }
}
