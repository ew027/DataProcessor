namespace DataProcessor
{
    partial class CodeEditorForm
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
            this.codeTextBox = new System.Windows.Forms.TextBox();
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.SuspendLayout();

            // toolStrip
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                new ToolStripButton("Save", null, null, "Save"),
                new ToolStripButton("Run", null, null, "Run")
            });
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(584, 25);
            this.toolStrip.TabIndex = 0;
            this.toolStrip.Text = "ToolStrip";

            // codeTextBox
            this.codeTextBox.AcceptsReturn = true;
            this.codeTextBox.AcceptsTab = true;
            this.codeTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.codeTextBox.Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.codeTextBox.Location = new System.Drawing.Point(0, 25);
            this.codeTextBox.Multiline = true;
            this.codeTextBox.Name = "codeTextBox";
            this.codeTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.codeTextBox.Size = new System.Drawing.Size(584, 336);
            this.codeTextBox.TabIndex = 1;
            this.codeTextBox.WordWrap = false;

            // CodeEditorForm
            this.ClientSize = new System.Drawing.Size(584, 361);
            this.Controls.Add(this.codeTextBox);
            this.Controls.Add(this.toolStrip);
            this.Name = "CodeEditorForm";
            this.Text = "Code Editor";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private TextBox codeTextBox;
        private ToolStrip toolStrip;
        #endregion
    }
}