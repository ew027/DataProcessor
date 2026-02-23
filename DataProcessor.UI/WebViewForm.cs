using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace DataProcessor
{
    public partial class WebViewForm : Form
    {
        public WebViewForm()
        {
            InitializeComponent();
        }

        private void WebViewForm_Load(object sender, EventArgs e)
        {
            outputPanel.Text = "<html><body><h1>Data Processor</h1><p>Run a script to see results here.</p></body></html>";
            EngineContext.ExecutionCompleted += OnExecutionCompleted;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            EngineContext.ExecutionCompleted -= OnExecutionCompleted;
            base.OnFormClosed(e);
        }

        private void OnExecutionCompleted(object? sender, ExecutionCompletedEventArgs e)
        {
            if (InvokeRequired) { Invoke(new Action(() => OnExecutionCompleted(sender, e))); return; }
            outputPanel.Text = BuildHtmlLog(e.LogEntries);
        }

        private static string BuildHtmlLog(List<string> entries)
        {
            var sb = new StringBuilder();
            sb.Append("<html><body style='font-family:monospace;font-size:13px'>");
            foreach (var entry in entries)
            {
                string escaped = System.Net.WebUtility.HtmlEncode(entry);
                bool isError = entry.Contains("Exception") || entry.Contains("Error");
                string style = isError ? "color:red" : "";

                // Bold the "Executing …" lines
                if (entry.StartsWith("Executing") || entry.StartsWith("Starting"))
                    sb.Append($"<p><strong style='{style}'>{escaped}</strong></p>");
                else if (isError)
                    sb.Append($"<p style='{style}'>{escaped}</p>");
                else
                    sb.Append($"<p>{escaped}</p>");
            }
            sb.Append("</body></html>");
            return sb.ToString();
        }
    }
}
