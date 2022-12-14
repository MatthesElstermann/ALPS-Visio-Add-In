using System;
using System.Windows.Forms;

namespace VisioAddIn
{
    public partial class TemporaryModelExplorer : Form
    {
        ThisAddIn addin;
        public TemporaryModelExplorer(ThisAddIn addin)
        {
            InitializeComponent();
            this.addin = addin;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            addin.updateClicked();
        }
    }
}
