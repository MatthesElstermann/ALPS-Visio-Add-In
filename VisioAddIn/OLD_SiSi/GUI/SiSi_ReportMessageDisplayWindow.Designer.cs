
namespace VisioAddIn.SiSi.GUI
{
    partial class SiSi_ReportMessageDisplayWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

       

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.bt_refresh = new System.Windows.Forms.Button();
            this.listView = new System.Windows.Forms.ListView();
            this.SuspendLayout();
            // 
            // bt_refresh
            // 
            this.bt_refresh.Dock = System.Windows.Forms.DockStyle.Top;
            this.bt_refresh.Location = new System.Drawing.Point(0, 0);
            this.bt_refresh.Name = "bt_refresh";
            this.bt_refresh.Size = new System.Drawing.Size(1082, 23);
            this.bt_refresh.TabIndex = 1;
            this.bt_refresh.Text = "Refresh ReportList";
            this.bt_refresh.UseVisualStyleBackColor = true;
            this.bt_refresh.Click += new System.EventHandler(this.bt_refresh_Click);
            // 
            // listView
            // 
            this.listView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listView.FullRowSelect = true;
            this.listView.GridLines = true;
            this.listView.Location = new System.Drawing.Point(0, 23);
            this.listView.Name = "listView";
            this.listView.Size = new System.Drawing.Size(1082, 280);
            this.listView.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.listView.TabIndex = 2;
            this.listView.UseCompatibleStateImageBehavior = false;
            this.listView.View = System.Windows.Forms.View.Details;
            this.listView.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.listView_ColumnClick);
            // 
            // SiSi_ReportMessageDisplayWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(1082, 303);
            this.Controls.Add(this.listView);
            this.Controls.Add(this.bt_refresh);
            this.MinimumSize = new System.Drawing.Size(650, 350);
            this.Name = "SiSi_ReportMessageDisplayWindow";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.Text = "SiSi_ReportMessageDisplayWindow";
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button bt_refresh;
        private System.Windows.Forms.ListView listView;
    }
}