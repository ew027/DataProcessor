using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            // Navigate to default URL when form loads
            outputPanel.Text = "<html><body><h1>Welcome to WebView</h1></body></html>";
        }
    }
}
