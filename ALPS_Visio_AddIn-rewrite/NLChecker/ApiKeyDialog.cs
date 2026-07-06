using System;
using System.Windows.Forms;

namespace ALPS_Visio_AddIn_rewrite.NLChecker
{
    /// <summary>Simple dialog to enter/replace the LLM API key.</summary>
    public class ApiKeyDialog : Form
    {
        private TextBox txtApiKey;
        private Button btnOK;
        private Button btnCancel;
        private Label lblApiKey;

        public string ApiKey => txtApiKey.Text;

        public ApiKeyDialog(string currentKey = null)
        {
            InitializeComponent();
            if (!string.IsNullOrEmpty(currentKey))
            {
                txtApiKey.Text = currentKey;
                txtApiKey.SelectAll();
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void InitializeComponent()
        {
            this.txtApiKey = new TextBox();
            this.btnOK = new Button();
            this.btnCancel = new Button();
            this.lblApiKey = new Label();
            this.SuspendLayout();
            //
            // txtApiKey
            //
            this.txtApiKey.Location = new System.Drawing.Point(70, 12);
            this.txtApiKey.Name = "txtApiKey";
            this.txtApiKey.Size = new System.Drawing.Size(200, 26);
            this.txtApiKey.TabIndex = 1;
            //
            // btnOK
            //
            this.btnOK.Location = new System.Drawing.Point(114, 45);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 2;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new EventHandler(this.btnOK_Click);
            //
            // btnCancel
            //
            this.btnCancel.DialogResult = DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(195, 45);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new EventHandler(this.btnCancel_Click);
            //
            // lblApiKey
            //
            this.lblApiKey.AutoSize = true;
            this.lblApiKey.Location = new System.Drawing.Point(12, 15);
            this.lblApiKey.Name = "lblApiKey";
            this.lblApiKey.Size = new System.Drawing.Size(69, 20);
            this.lblApiKey.TabIndex = 0;
            this.lblApiKey.Text = "API Key:";
            //
            // ApiKeyDialog
            //
            this.AcceptButton = this.btnOK;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(390, 90);
            this.Controls.Add(this.lblApiKey);
            this.Controls.Add(this.txtApiKey);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ApiKeyDialog";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Enter API Key";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
