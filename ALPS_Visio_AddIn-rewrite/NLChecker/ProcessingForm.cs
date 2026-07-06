using System.Drawing;
using System.Windows.Forms;

namespace ALPS_Visio_AddIn_rewrite.NLChecker
{
    /// <summary>Small progress window shown while the NL check iterates the document's shapes.</summary>
    public class ProcessingForm : Form
    {
        private Label lblProgress;
        private ProgressBar progressBar;

        public ProcessingForm()
        {
            InitializeComponent();
        }

        public void UpdateProgress(int processed, int total)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(() => UpdateProgress(processed, total)));
                return;
            }

            lblProgress.Text = $"Processing {processed} of {total} shapes...";
            progressBar.Maximum = total < 1 ? 1 : total;
            progressBar.Value = processed > progressBar.Maximum ? progressBar.Maximum : processed;
            Application.DoEvents(); // Force UI update
        }

        private void InitializeComponent()
        {
            this.lblProgress = new Label();
            this.progressBar = new ProgressBar();
            this.SuspendLayout();

            this.lblProgress.AutoSize = true;
            this.lblProgress.Location = new Point(10, 10);
            this.lblProgress.Text = "Starting processing...";

            this.progressBar.Location = new Point(10, 40);
            this.progressBar.Size = new Size(300, 20);

            this.ClientSize = new Size(320, 70);
            this.Controls.Add(this.lblProgress);
            this.Controls.Add(this.progressBar);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "PASS NL Checker";
            this.ResumeLayout(false);
        }
    }
}
