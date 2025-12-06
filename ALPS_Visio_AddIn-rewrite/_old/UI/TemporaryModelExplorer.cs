using System;
using System.Windows.Forms;

namespace VisioAddIn
{
    public partial class TemporaryModelExplorer : Form
    {
        ALPS_Visio_AddIn_rewrite.ThisAddIn addin;
        public TemporaryModelExplorer(ALPS_Visio_AddIn_rewrite.ThisAddIn addin)
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
