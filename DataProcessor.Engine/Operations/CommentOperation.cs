using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataProcessor.Engine.Operations {
    public class CommentOperation : Operation {
        public string CommentText { get; set; }

        public CommentOperation(string arguments) {
            CommentText = arguments;
        }

        public override string GetExecuteMethod() {
            return "ExecuteComment";
        }
    }
}
