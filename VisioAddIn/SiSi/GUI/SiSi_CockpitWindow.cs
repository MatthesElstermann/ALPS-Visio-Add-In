using System;
using System.Windows.Forms;

namespace VisioAddIn.SiSi.GUI
{
    public partial class SiSi_CockpitWindow : Form
    {
        SiSi_CockpitController myController;

        internal SiSi_CockpitWindow(SiSi_CockpitController controller)
        {
            InitializeComponent();
            myController = controller;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            myController.startSimulation_Clicked();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            myController.showErrorLog_Clicked();
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            myController.comboBoxSelectionChanged();
        }


        internal string getParameterRecursionDepth()
        {
            return textBox1.Text;
        }

        internal string getParameterSigma()
        {
            return textBox2.Text;
        }

        internal bool getParameterAbsolutMinMax()
        {
            return checkBoxDetermineAbsolutMinMax.Checked;
        }

        internal bool getParameterWriteWaitingTimes()
        {
            return checkBoxWriteWaitingTimesIntoModel.Checked;
        }



        internal bool getOutputReportResponseObjects()
        {
            return checkBox1.Checked;
        }

        internal bool getOutputReportFirstSendObjects()
        {
            return checkBox2.Checked;
        }
        //Textbox Output
        internal string getText()
        {
            return textBox3.Text;
        }

        internal void setText(string text)
        {
            textBox3.Text = text;
        }

        internal void addText(string text)
        {
            textBox3.Text += text;
        }

        internal void addTextLine(string text)
        {
            textBox3.Text += Environment.NewLine + text;
        }

        internal string getComboBoxSelection()
        {
            return comboBox2.SelectedItem.ToString();
        }

        internal void addComboBoxItem(string itemName)
        {
            comboBox2.Items.Add(itemName);
        }

        internal void setComboBoxEnabeld(bool enabeld)
        {
            comboBox2.Enabled = enabeld;
        }

        internal void setComboxBoxSelection(string selection)
        {
            comboBox2.SelectedItem = selection;
        }

    }
}
