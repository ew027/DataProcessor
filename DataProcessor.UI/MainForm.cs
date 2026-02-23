using System;
using System.IO;
using System.Windows.Forms;
using DataProcessor.Engine;

namespace DataProcessor
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void newCodeEditorMenuItem_Click(object sender, EventArgs e)
        {
            CodeEditorForm codeEditor = new CodeEditorForm();
            codeEditor.MdiParent = this;
            codeEditor.Text = "Code Editor " + MdiChildren.Length;
            codeEditor.Show();
            statusLabel.Text = "Code editor window created";
        }

        private void newWebViewMenuItem_Click(object sender, EventArgs e)
        {
            WebViewForm webView = new WebViewForm();
            webView.MdiParent = this;
            webView.Text = "Web View " + MdiChildren.Length;
            webView.Show();
            statusLabel.Text = "Web view window created";
        }

        private void newDataGridMenuItem_Click(object sender, EventArgs e)
        {
            DataGridForm dataGrid = new DataGridForm();
            dataGrid.MdiParent = this;
            dataGrid.Text = "Data Grid " + MdiChildren.Length;
            dataGrid.Show();
            statusLabel.Text = "Data grid window created";
        }

        private void openScriptMenuItem_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Filter = "Script files (*.txt)|*.txt|All files (*.*)|*.*",
                Title = "Open Script"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            string text = File.ReadAllText(dlg.FileName);

            // Open or reuse a CodeEditorForm
            CodeEditorForm? editor = null;
            foreach (Form child in MdiChildren)
                if (child is CodeEditorForm ce) { editor = ce; break; }

            if (editor == null)
            {
                editor = new CodeEditorForm { MdiParent = this };
                editor.Show();
            }

            editor.CodeText = text;
            editor.Activate();
            statusLabel.Text = $"Script loaded: {Path.GetFileName(dlg.FileName)}";
        }

        private void loadDataMenuItem_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Load Data (CSV)"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                DataStore ds = CsvImporter.FromCsv(dlg.FileName);
                string name = Path.GetFileNameWithoutExtension(dlg.FileName);
                EngineContext.Engine.AddDataStore(ds, name);

                // Update DataGrid if one is open
                foreach (Form child in MdiChildren)
                {
                    if (child is DataGridForm dgf) { dgf.LoadData(ds); break; }
                }

                statusLabel.Text = $"Loaded {ds.RowCount} rows from {Path.GetFileName(dlg.FileName)} as '{name}'";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load CSV: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void exitMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void cascadeMenuItem_Click(object sender, EventArgs e) =>
            this.LayoutMdi(MdiLayout.Cascade);

        private void tileHorizontalMenuItem_Click(object sender, EventArgs e) =>
            this.LayoutMdi(MdiLayout.TileHorizontal);

        private void tileVerticalMenuItem_Click(object sender, EventArgs e) =>
            this.LayoutMdi(MdiLayout.TileVertical);

        private void arrangeIconsMenuItem_Click(object sender, EventArgs e) =>
            this.LayoutMdi(MdiLayout.ArrangeIcons);

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
