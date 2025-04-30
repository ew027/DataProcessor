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
            // Create a new code editor window
            CodeEditorForm codeEditor = new CodeEditorForm();
            codeEditor.MdiParent = this;
            codeEditor.Text = "Code Editor " + MdiChildren.Count(c => c is CodeEditorForm);
            codeEditor.Show();
            statusLabel.Text = "Code editor window created";
        }

        private void newWebViewMenuItem_Click(object sender, EventArgs e)
        {
            // Create a new web view window
            WebViewForm webView = new WebViewForm();
            webView.MdiParent = this;
            webView.Text = "Web View " + MdiChildren.Count(c => c is WebViewForm);
            webView.Show();
            statusLabel.Text = "Web view window created";
        }

        private void newDataGridMenuItem_Click(object sender, EventArgs e)
        {
            // Create a new data grid window
            DataGridForm dataGrid = new DataGridForm();
            dataGrid.MdiParent = this;
            dataGrid.Text = "Data Grid " + MdiChildren.Count(c => c is DataGridForm);
            dataGrid.Show();
            statusLabel.Text = "Data grid window created";
        }

        private void exitMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void cascadeMenuItem_Click(object sender, EventArgs e)
        {
            this.LayoutMdi(MdiLayout.Cascade);
        }

        private void tileHorizontalMenuItem_Click(object sender, EventArgs e)
        {
            this.LayoutMdi(MdiLayout.TileHorizontal);
        }

        private void tileVerticalMenuItem_Click(object sender, EventArgs e)
        {
            this.LayoutMdi(MdiLayout.TileVertical);
        }

        private void arrangeIconsMenuItem_Click(object sender, EventArgs e)
        {
            this.LayoutMdi(MdiLayout.ArrangeIcons);
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
