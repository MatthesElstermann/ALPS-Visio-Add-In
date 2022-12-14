using System;
using System.Windows.Forms;

namespace VisioAddIn
{
    public partial class OWLImportDialog : Form
    {
        public OWLImportDialog()
        {
            InitializeComponent();
            ComboBox comboBoxModel = new ComboBox();
            string[] modeltest = new string[] { "model1", "model2", "model3" };
            comboBoxModel.Items.AddRange(modeltest);
            //hier noch getMethode aufrufen, die modellnamen übergibt
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Import_Click(object sender, EventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            String model = (string)comboBox.SelectedItem;
            //hier Parser mit entsprechendem Model aufrufen
            Close();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
