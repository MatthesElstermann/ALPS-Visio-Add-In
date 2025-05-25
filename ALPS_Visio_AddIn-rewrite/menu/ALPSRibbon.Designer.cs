using System.Diagnostics;

namespace ALPS_Visio_AddIn_rewrite
{
    partial class ALPSRibbon : Microsoft.Office.Tools.Ribbon.RibbonBase
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        public ALPSRibbon()
            : base(Globals.Factory.GetRibbonFactory())
        {
            InitializeComponent();
        }

        /// <summary> 
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">"true", wenn verwaltete Ressourcen gelöscht werden sollen, andernfalls "false".</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        internal Microsoft.Office.Tools.Ribbon.RibbonTab tab1;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup group1;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton loadOWLFile;

        #region Vom Komponenten-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            // init
            this.tab1 = this.Factory.CreateRibbonTab();
            this.tab1.SuspendLayout();
            this.group1 = this.Factory.CreateRibbonGroup();
            this.group1.SuspendLayout();
            this.loadOWLFile = this.Factory.CreateRibbonButton();
            this.SuspendLayout();
            // 
            // tab1
            // 
            this.tab1.ControlId.ControlIdType = Microsoft.Office.Tools.Ribbon.RibbonControlIdType.Office;
            this.tab1.Groups.Add(this.group1);
            this.tab1.Label = "ALPS/PASS ADDIN";
            this.tab1.Name = "tab1";
            // 
            // group1
            // 
            this.group1.Items.Add(this.loadOWLFile);
            this.group1.Label = "OWL PASS Tools";
            this.group1.Name = "group1";
            //
            // loadOWLFile
            //
            this.loadOWLFile.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.loadOWLFile.Image = global::ALPS_Visio_AddIn_rewrite.Properties.Resources.owlIcon2;
            this.loadOWLFile.Label = "Import OWL";
            this.loadOWLFile.Name = "loadOWLFile";
            this.loadOWLFile.ShowImage = true;
            this.loadOWLFile.SuperTip = "Use this tool to import PASS and ALPS Process Models from OWL Files based on the standard pass ontology";
            this.loadOWLFile.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.loadOWLFile_Click);
            // 
            // ALPSRibbon
            // 
            this.Name = "Ribbon1";
            this.RibbonType = "Microsoft.Visio.Drawing";
            this.Tabs.Add(this.tab1);
            this.Load += new Microsoft.Office.Tools.Ribbon.RibbonUIEventHandler(this.ALPSRibbon_Load);
            this.tab1.ResumeLayout(false);
            this.tab1.PerformLayout();
            this.group1.ResumeLayout(false);
            this.group1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
    }

    partial class ThisRibbonCollection
    {
        internal ALPSRibbon ALPSRibbon
        {
            get { return this.GetRibbon<ALPSRibbon>(); }
        }
    }
}
