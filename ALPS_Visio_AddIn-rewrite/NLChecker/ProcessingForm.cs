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

            // DPI-festes Layout wie beim ApiKeyDialog: AutoSize-Container statt fester
            // Pixelpositionen, sonst schneidet hohe Bildschirmskalierung den Text ab.
            this.lblProgress.AutoSize = true;
            this.lblProgress.Margin = new Padding(0, 0, 0, 8);
            this.lblProgress.Text = "Starting processing...";

            this.progressBar.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            this.progressBar.MinimumSize = new Size(300, 20);

            TableLayoutPanel layout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 2,
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Controls.Add(this.lblProgress, 0, 0);
            layout.Controls.Add(this.progressBar, 0, 1);

            this.AutoScaleMode = AutoScaleMode.Font;
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.Controls.Add(layout);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "PASS NL Checker";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
