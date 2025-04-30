using Westermo.HtmlRenderer.WinForms;

namespace DataProcessor
{
    partial class WebViewForm
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
            this.outputPanel = new HtmlPanel();
            this.SuspendLayout();

            
            // webBrowser
            this.outputPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.outputPanel.Location = new System.Drawing.Point(0, 0);
            this.outputPanel.MinimumSize = new System.Drawing.Size(20, 20);
            this.outputPanel.Name = "outputPanel";
            this.outputPanel.Size = new System.Drawing.Size(584, 361);
            this.outputPanel.TabIndex = 1;
            this.outputPanel.AutoScroll = true;

            // WebViewForm
            this.ClientSize = new System.Drawing.Size(584, 361);
            this.Controls.Add(this.outputPanel);
            this.Name = "WebViewForm";
            this.Text = "Output";
            this.Load += new System.EventHandler(this.WebViewForm_Load);
            this.ResumeLayout(false);
        }

        private HtmlPanel outputPanel;
        #endregion
    }
}