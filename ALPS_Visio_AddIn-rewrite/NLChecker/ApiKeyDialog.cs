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

            // DPI-festes Layout: keine festen Pixelpositionen, sondern Layout-Container mit
            // AutoSize — bei 125/150 % Bildschirmskalierung wuchsen sonst die Schriften,
            // aber nicht der Dialog (abgeschnittene Buttons, "Canc...").
            //
            // lblApiKey
            //
            this.lblApiKey.AutoSize = true;
            this.lblApiKey.Anchor = AnchorStyles.Left;
            this.lblApiKey.Margin = new Padding(0, 0, 8, 0);
            this.lblApiKey.Name = "lblApiKey";
            this.lblApiKey.TabIndex = 0;
            this.lblApiKey.Text = "API Key:";
            //
            // txtApiKey
            //
            this.txtApiKey.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            this.txtApiKey.MinimumSize = new System.Drawing.Size(280, 0);
            this.txtApiKey.Name = "txtApiKey";
            this.txtApiKey.TabIndex = 1;
            //
            // btnOK
            //
            this.btnOK.AutoSize = true;
            this.btnOK.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.btnOK.MinimumSize = new System.Drawing.Size(88, 28);
            this.btnOK.Padding = new Padding(10, 2, 10, 2);
            this.btnOK.Name = "btnOK";
            this.btnOK.TabIndex = 2;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new EventHandler(this.btnOK_Click);
            //
            // btnCancel
            //
            this.btnCancel.AutoSize = true;
            this.btnCancel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.btnCancel.MinimumSize = new System.Drawing.Size(88, 28);
            this.btnCancel.Padding = new Padding(10, 2, 10, 2);
            this.btnCancel.DialogResult = DialogResult.Cancel;
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new EventHandler(this.btnCancel_Click);
            //
            // Button-Zeile (rechtsbuendig; RightToLeft: zuerst hinzugefuegt = ganz rechts)
            //
            FlowLayoutPanel buttonRow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0, 12, 0, 0),
                WrapContents = false,
            };
            buttonRow.Controls.Add(this.btnCancel);
            buttonRow.Controls.Add(this.btnOK);
            //
            // Tabellen-Layout: Zeile 1 = Label + Eingabe, Zeile 2 = Buttons rechts
            //
            TableLayoutPanel layout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 2,
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Controls.Add(this.lblApiKey, 0, 0);
            layout.Controls.Add(this.txtApiKey, 1, 0);
            layout.Controls.Add(buttonRow, 1, 1);
            //
            // ApiKeyDialog
            //
            this.AutoScaleMode = AutoScaleMode.Font;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.AcceptButton = this.btnOK;
            this.CancelButton = this.btnCancel;
            this.Controls.Add(layout);
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
