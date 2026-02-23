using System;
using System.IO;
using System.Windows.Forms;

namespace DataProcessor
{
    public partial class CodeEditorForm : Form
    {
        public CodeEditorForm()
        {
            InitializeComponent();
            WireToolStripButtons();
        }

        public string CodeText
        {
            get { return codeTextBox.Text; }
            set { codeTextBox.Text = value; }
        }

        private void WireToolStripButtons()
        {
            foreach (ToolStripItem item in toolStrip.Items)
            {
                switch (item.Name)
                {
                    case "Save": item.Click += SaveButton_Click; break;
                    case "Run":  item.Click += RunButton_Click;  break;
                    case "Load": item.Click += LoadButton_Click; break;
                }
            }
        }

        private void RunButton_Click(object? sender, EventArgs e)
        {
            string script = codeTextBox.Text;
            string scriptName = EngineContext.CurrentScript;

            EngineContext.Engine.ClearOperations(scriptName);

            try
            {
                bool isPipeFormat = script.Contains('|');
                if (isPipeFormat)
                {
                    // Pipe-delimited format: one operation per line
                    foreach (var line in script.Split('\n'))
                    {
                        string trimmed = line.Trim();
                        if (!string.IsNullOrEmpty(trimmed))
                            EngineContext.Engine.AddOperation(trimmed, scriptName);
                    }
                }
                else
                {
                    // SPSS-like format
                    EngineContext.Engine.LoadScriptSpss(script, scriptName);
                }

                EngineContext.Engine.Execute(scriptName);
            }
            catch (Exception ex)
            {
                EngineContext.Engine.GetLogEntries().Add($"Fatal error during execution: {ex.Message}");
            }

            EngineContext.RaiseExecutionCompleted(EngineContext.Engine.GetLogEntries());
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            using var dlg = new SaveFileDialog
            {
                Filter = "Script files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = "txt"
            };
            if (dlg.ShowDialog() == DialogResult.OK)
                File.WriteAllText(dlg.FileName, codeTextBox.Text);
        }

        private void LoadButton_Click(object? sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Filter = "Script files (*.txt)|*.txt|All files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == DialogResult.OK)
                codeTextBox.Text = File.ReadAllText(dlg.FileName);
        }
    }
}
