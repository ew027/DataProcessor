namespace DataProcessor
{
    partial class DataGridForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.dataGridView = new System.Windows.Forms.DataGridView();
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.addColumnButton = new System.Windows.Forms.ToolStripButton();
            this.addRowButton = new System.Windows.Forms.ToolStripButton();
            this.removeRowButton = new System.Windows.Forms.ToolStripButton();
            this.removeColumnButton = new System.Windows.Forms.ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).BeginInit();
            this.toolStrip.SuspendLayout();
            this.SuspendLayout();

            // toolStrip
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.addColumnButton,
                this.addRowButton,
                this.removeRowButton,
                this.removeColumnButton});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(584, 25);
            this.toolStrip.TabIndex = 0;
            this.toolStrip.Text = "toolStrip";

            // addColumnButton
            this.addColumnButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.addColumnButton.Name = "addColumnButton";
            this.addColumnButton.Size = new System.Drawing.Size(83, 22);
            this.addColumnButton.Text = "Add Column";
            this.addColumnButton.Click += new System.EventHandler(this.addColumnButton_Click);

            // addRowButton
            this.addRowButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.addRowButton.Name = "addRowButton";
            this.addRowButton.Size = new System.Drawing.Size(62, 22);
            this.addRowButton.Text = "Add Row";
            this.addRowButton.Click += new System.EventHandler(this.addRowButton_Click);

            // removeRowButton
            this.removeRowButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.removeRowButton.Name = "removeRowButton";
            this.removeRowButton.Size = new System.Drawing.Size(83, 22);
            this.removeRowButton.Text = "Remove Row";
            this.removeRowButton.Click += new System.EventHandler(this.removeRowButton_Click);

            // removeColumnButton
            this.removeColumnButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.removeColumnButton.Name = "removeColumnButton";
            this.removeColumnButton.Size = new System.Drawing.Size(104, 22);
            this.removeColumnButton.Text = "Remove Column";
            this.removeColumnButton.Click += new System.EventHandler(this.removeColumnButton_Click);

            // dataGridView
            this.dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView.Location = new System.Drawing.Point(0, 25);
            this.dataGridView.Name = "dataGridView";
            this.dataGridView.Size = new System.Drawing.Size(584, 336);
            this.dataGridView.TabIndex = 1;

            // DataGridForm
            this.ClientSize = new System.Drawing.Size(584, 361);
            this.Controls.Add(this.dataGridView);
            this.Controls.Add(this.toolStrip);
            this.Name = "DataGridForm";
            this.Text = "Data Grid";
            this.Load += new System.EventHandler(this.DataGridForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).EndInit();
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private DataGridView dataGridView;
        private ToolStrip toolStrip;
        private ToolStripButton addColumnButton;
        private ToolStripButton addRowButton;
        private ToolStripButton removeRowButton;
        private ToolStripButton removeColumnButton;
        #endregion
    }
}