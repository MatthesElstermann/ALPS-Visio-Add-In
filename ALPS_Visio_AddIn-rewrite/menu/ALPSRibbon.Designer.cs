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
            this.tab1 = this.Factory.CreateRibbonTab();
            this.tab1.SuspendLayout();
            this.group1 = this.Factory.CreateRibbonGroup();
            this.group1.SuspendLayout();
            this.loadOWLFile = this.Factory.CreateRibbonButton();
            this.SuspendLayout();

            this.tab1.Label = "ALPS/PASS ADDIN";
            this.tab1.Groups.Add(this.group1);

            this.group1.Label = "OWL PASS Tools";
            this.group1.Items.Add(this.loadOWLFile);

            this.loadOWLFile.Name = "loadOWLFile";
            this.loadOWLFile.Label = "Import OWL";
            this.loadOWLFile.SuperTip = "Use this tool to import PASS and ALPS Process Models from OWL Files based on the standard pass ontology";
            this.loadOWLFile.Image = global::ALPS_Visio_AddIn_rewrite.Properties.Resources.owlIcon2;
            this.loadOWLFile.ShowImage = true;
            this.loadOWLFile.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.loadOWLFile.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.loadOWLFile_Click);

            this.RibbonType = "Microsoft.Visio.Drawing";
            this.Tabs.Add(this.tab1);

            this.tab1.ResumeLayout(true);
            this.group1.ResumeLayout(true);
            this.ResumeLayout(true);
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
