using System.Drawing;
using System.Windows.Forms;

namespace ALPS_Visio_AddIn_rewrite.Verification
{
    /// <summary>Read-only window that shows the raw ALPS Verification report.</summary>
    public class VerificationResultsForm : Form
    {
        private RichTextBox richTextBox;

        public VerificationResultsForm(string report)
        {
            InitializeComponent();
            richTextBox.Text = report;
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
            this.Text = "ALPS Verification – Ergebnis";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ResumeLayout(false);
        }
    }
}
