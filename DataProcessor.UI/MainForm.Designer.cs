namespace DataProcessor
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.fileMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.newMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.newCodeEditorMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newWebViewMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newDataGridMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openScriptMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadDataMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.windowMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.cascadeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tileHorizontalMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tileVerticalMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.arrangeIconsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.menuStrip.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();

            // menuStrip
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.fileMenu,
                this.windowMenu});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.MdiWindowListItem = this.windowMenu;
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(800, 24);
            this.menuStrip.TabIndex = 0;
            this.menuStrip.Text = "MenuStrip";

            // fileMenu
            this.fileMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.newMenu,
                this.openScriptMenuItem,
                this.loadDataMenuItem,
                this.exitMenuItem});
            this.fileMenu.Name = "fileMenu";
            this.fileMenu.Size = new System.Drawing.Size(37, 20);
            this.fileMenu.Text = "&File";

            // newMenu
            this.newMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.newCodeEditorMenuItem,
                this.newWebViewMenuItem,
                this.newDataGridMenuItem});
            this.newMenu.Name = "newMenu";
            this.newMenu.Size = new System.Drawing.Size(180, 22);
            this.newMenu.Text = "&New";

            // newCodeEditorMenuItem
            this.newCodeEditorMenuItem.Name = "newCodeEditorMenuItem";
            this.newCodeEditorMenuItem.Size = new System.Drawing.Size(180, 22);
            this.newCodeEditorMenuItem.Text = "Code &Editor";
            this.newCodeEditorMenuItem.Click += new System.EventHandler(this.newCodeEditorMenuItem_Click);

            // newWebViewMenuItem
            this.newWebViewMenuItem.Name = "newWebViewMenuItem";
            this.newWebViewMenuItem.Size = new System.Drawing.Size(180, 22);
            this.newWebViewMenuItem.Text = "&Web View";
            this.newWebViewMenuItem.Click += new System.EventHandler(this.newWebViewMenuItem_Click);

            // newDataGridMenuItem
            this.newDataGridMenuItem.Name = "newDataGridMenuItem";
            this.newDataGridMenuItem.Size = new System.Drawing.Size(180, 22);
            this.newDataGridMenuItem.Text = "&Data Grid";
            this.newDataGridMenuItem.Click += new System.EventHandler(this.newDataGridMenuItem_Click);

            // openScriptMenuItem
            this.openScriptMenuItem.Name = "openScriptMenuItem";
            this.openScriptMenuItem.Size = new System.Drawing.Size(180, 22);
            this.openScriptMenuItem.Text = "&Open Script...";
            this.openScriptMenuItem.Click += new System.EventHandler(this.openScriptMenuItem_Click);

            // loadDataMenuItem
            this.loadDataMenuItem.Name = "loadDataMenuItem";
            this.loadDataMenuItem.Size = new System.Drawing.Size(180, 22);
            this.loadDataMenuItem.Text = "Load &Data (CSV)...";
            this.loadDataMenuItem.Click += new System.EventHandler(this.loadDataMenuItem_Click);

            // exitMenuItem
            this.exitMenuItem.Name = "exitMenuItem";
            this.exitMenuItem.Size = new System.Drawing.Size(180, 22);
            this.exitMenuItem.Text = "E&xit";
            this.exitMenuItem.Click += new System.EventHandler(this.exitMenuItem_Click);

            // windowMenu
            this.windowMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.cascadeMenuItem,
                this.tileHorizontalMenuItem,
                this.tileVerticalMenuItem,
                this.arrangeIconsMenuItem});
            this.windowMenu.Name = "windowMenu";
            this.windowMenu.Size = new System.Drawing.Size(63, 20);
            this.windowMenu.Text = "&Window";

            // cascadeMenuItem
            this.cascadeMenuItem.Name = "cascadeMenuItem";
            this.cascadeMenuItem.Size = new System.Drawing.Size(180, 22);
            this.cascadeMenuItem.Text = "&Cascade";
            this.cascadeMenuItem.Click += new System.EventHandler(this.cascadeMenuItem_Click);

            // tileHorizontalMenuItem
            this.tileHorizontalMenuItem.Name = "tileHorizontalMenuItem";
            this.tileHorizontalMenuItem.Size = new System.Drawing.Size(180, 22);
            this.tileHorizontalMenuItem.Text = "Tile &Horizontal";
            this.tileHorizontalMenuItem.Click += new System.EventHandler(this.tileHorizontalMenuItem_Click);

            // tileVerticalMenuItem
            this.tileVerticalMenuItem.Name = "tileVerticalMenuItem";
            this.tileVerticalMenuItem.Size = new System.Drawing.Size(180, 22);
            this.tileVerticalMenuItem.Text = "Tile &Vertical";
            this.tileVerticalMenuItem.Click += new System.EventHandler(this.tileVerticalMenuItem_Click);

            // arrangeIconsMenuItem
            this.arrangeIconsMenuItem.Name = "arrangeIconsMenuItem";
            this.arrangeIconsMenuItem.Size = new System.Drawing.Size(180, 22);
            this.arrangeIconsMenuItem.Text = "&Arrange Icons";
            this.arrangeIconsMenuItem.Click += new System.EventHandler(this.arrangeIconsMenuItem_Click);

            // statusStrip
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.statusLabel});
            this.statusStrip.Location = new System.Drawing.Point(0, 428);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(800, 22);
            this.statusStrip.TabIndex = 1;
            this.statusStrip.Text = "StatusStrip";

            // statusLabel
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(39, 17);
            this.statusLabel.Text = "Ready";

            // MainForm
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.menuStrip);
            this.IsMdiContainer = true;
            this.MainMenuStrip = this.menuStrip;
            this.Name = "MainForm";
            this.Text = "Data Processor";
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private MenuStrip menuStrip;
        private ToolStripMenuItem fileMenu;
        private ToolStripMenuItem newMenu;
        private ToolStripMenuItem newCodeEditorMenuItem;
        private ToolStripMenuItem newWebViewMenuItem;
        private ToolStripMenuItem newDataGridMenuItem;
        private ToolStripMenuItem openScriptMenuItem;
        private ToolStripMenuItem loadDataMenuItem;
        private ToolStripMenuItem exitMenuItem;
        private ToolStripMenuItem windowMenu;
        private ToolStripMenuItem cascadeMenuItem;
        private ToolStripMenuItem tileHorizontalMenuItem;
        private ToolStripMenuItem tileVerticalMenuItem;
        private ToolStripMenuItem arrangeIconsMenuItem;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        #endregion
    }
}
