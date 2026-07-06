using System.Drawing;
using System.Windows.Forms;

namespace ALPS_Visio_AddIn_rewrite.NLChecker
{
    /// <summary>Read-only window that shows the NL-check report for the active document.</summary>
    public class ValidityCheckResultsForm : Form
    {
        private RichTextBox richTextBox;

        public ValidityCheckResultsForm(string analysisResult)
        {
            InitializeComponent();
            richTextBox.Text = analysisResult;
        }

        private void InitializeComponent()
        {
            this.richTextBox = new RichTextBox();
            this.SuspendLayout();

            this.richTextBox.Dock = DockStyle.Fill;
            this.richTextBox.Font = new Font("Consolas", 10);
            this.richTextBox.ReadOnly = true;
            this.richTextBox.WordWrap = false;

            this.ClientSize = new Size(800, 600);
            this.Controls.Add(this.richTextBox);
            this.Text = "PASS NL Checker – Model Integrity Check";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ResumeLayout(false);
        }
    }
}
