namespace VisioAddIn
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
        /// <param Name="disposing">"true", wenn verwaltete Ressourcen gelöscht werden sollen, andernfalls "false".</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Komponenten-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für Designerunterstützung -
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.tab1 = this.Factory.CreateRibbonTab();
            this.group1 = this.Factory.CreateRibbonGroup();
            this.openShapes = this.Factory.CreateRibbonButton();
            this.group2 = this.Factory.CreateRibbonGroup();
            this.showDirectory = this.Factory.CreateRibbonButton();
            this.group3 = this.Factory.CreateRibbonGroup();
            this.buttonCreateFromFile = this.Factory.CreateRibbonButton();
            this.tab1.SuspendLayout();
            this.group1.SuspendLayout();
            this.group2.SuspendLayout();
            this.group3.SuspendLayout();
            this.SuspendLayout();
            // 
            // tab1
            // 
            this.tab1.ControlId.ControlIdType = Microsoft.Office.Tools.Ribbon.RibbonControlIdType.Office;
            this.tab1.Groups.Add(this.group1);
            this.tab1.Groups.Add(this.group2);
            this.tab1.Groups.Add(this.group3);
            this.tab1.Label = "ALPS/PASS ADDIN";
            this.tab1.Name = "tab1";
            // 
            // group1
            // 
            this.group1.Items.Add(this.openShapes);
            this.group1.Name = "group1";
            // 
            // openShapes
            // 
            this.openShapes.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.openShapes.Label = "Open ALPS/PASS Stencils";
            this.openShapes.Name = "openShapes";
            this.openShapes.ShowImage = true;
            this.openShapes.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.openShapes_Click);
            // 
            // group2
            // 
            this.group2.Items.Add(this.showDirectory);
            this.group2.Name = "group2";
            // 
            // showDirectory
            // 
            this.showDirectory.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.showDirectory.ImageName = "LayersMenu";
            this.showDirectory.Label = "Show layer Explorer";
            this.showDirectory.Name = "showDirectory";
            this.showDirectory.OfficeImageId = "LayersMenu";
            this.showDirectory.ShowImage = true;
            this.showDirectory.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.showDirectoryClicked);
            // 
            // group3
            // 
            this.group3.Items.Add(this.buttonCreateFromFile);
            this.group3.Name = "group3";
            // 
            // buttonCreateFromFile
            // 
            this.buttonCreateFromFile.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.buttonCreateFromFile.Image = global::VisioAddIn.Properties.Resources.owlIcon2;
            this.buttonCreateFromFile.Label = "Import OWL";
            this.buttonCreateFromFile.Name = "buttonCreateFromFile";
            this.buttonCreateFromFile.ShowImage = true;
            this.buttonCreateFromFile.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.buttonCreateFromFile_Click);
            // 
            // ALPSRibbon
            // 
            this.Name = "ALPSRibbon";
            this.RibbonType = "Microsoft.Visio.Drawing";
            this.Tabs.Add(this.tab1);
            this.Load += new Microsoft.Office.Tools.Ribbon.RibbonUIEventHandler(this.Ribbon1_Load);
            this.tab1.ResumeLayout(false);
            this.tab1.PerformLayout();
            this.group1.ResumeLayout(false);
            this.group1.PerformLayout();
            this.group2.ResumeLayout(false);
            this.group2.PerformLayout();
            this.group3.ResumeLayout(false);
            this.group3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        internal Microsoft.Office.Tools.Ribbon.RibbonTab tab1;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup group1;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup group2;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton openShapes;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton showDirectory;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup group3;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton buttonCreateFromFile;
    }

    partial class ThisRibbonCollection
    {
        internal ALPSRibbon Ribbon1
        {
            get { return this.GetRibbon<ALPSRibbon>(); }
        }
    }
}
