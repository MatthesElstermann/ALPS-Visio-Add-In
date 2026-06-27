using Microsoft.Office.Tools.Ribbon;
using Microsoft.Office.Core;
using System.Windows.Forms;

namespace ALPS_Visio_AddIn_rewrite
{
    partial class ALPSRibbon : RibbonBase
    {
        /// <summary>
        /// Ribbon containing ALPS Menu
        /// </summary>
        public ALPSRibbon() : base(Globals.Factory.GetRibbonFactory())
        {
            this.RibbonType = "Microsoft.Visio.Drawing";

            RibbonTab alpsTab = this.Factory.CreateRibbonTab();
            alpsTab.Label = "ALPS/PASS ADDIN";
            this.Tabs.Add(alpsTab);

            RibbonGroup owlGroup = this.Factory.CreateRibbonGroup();
            owlGroup.Label = "ALPS Tools";
            alpsTab.Groups.Add(owlGroup);

            RibbonButton owlImporterButton = this.Factory.CreateRibbonButton();
            owlImporterButton.Name = "owlImporterButton";
            owlImporterButton.Label = "Import OWL";
            owlImporterButton.SuperTip = "Use this tool to import PASS and ALPS Process Models from OWL Files based on the standard pass ontology";
            owlImporterButton.Image = Properties.Resources.owlIcon2;
            owlImporterButton.ShowImage = true;
            owlImporterButton.ControlSize = RibbonControlSize.RibbonControlSizeLarge;
            owlImporterButton.Click += new RibbonControlEventHandler(this.LoadOWLFile);
            owlGroup.Items.Add(owlImporterButton);

            RibbonButton openStencilsButton = this.Factory.CreateRibbonButton();
            openStencilsButton.Name = "openStencilsButton";
            openStencilsButton.Label = "Open ALPS/PASS Stencils";
            openStencilsButton.SuperTip = "Tries to open the (necessary) ALPS Visio stencils if they are available on the system.";
            openStencilsButton.Image = Properties.Resources.document_open_7;
            openStencilsButton.ShowImage = true;
            openStencilsButton.ControlSize = RibbonControlSize.RibbonControlSizeLarge;
            openStencilsButton.Click += new RibbonControlEventHandler(this.OpenStencils);
            owlGroup.Items.Add(openStencilsButton);

            RibbonButton layerExplorerButton = this.Factory.CreateRibbonButton();
            layerExplorerButton.Name = "layerExplorerButton";
            layerExplorerButton.Label = "Show layer Explorer";
            layerExplorerButton.SuperTip = "Open a the layer explorer, a tool for advanced multi-layered ALPS (Abstract Layered PASS editing)";
            layerExplorerButton.Image = Properties.Resources.pageSetup;
            layerExplorerButton.ShowImage = true;
            layerExplorerButton.ControlSize = RibbonControlSize.RibbonControlSizeLarge;
            layerExplorerButton.Click += new RibbonControlEventHandler(this.ShowLayerExplorer);
            owlGroup.Items.Add(layerExplorerButton);

            RibbonButton arrangeTopDownButton = this.Factory.CreateRibbonButton();
            arrangeTopDownButton.Name = "arrangeTopDownButton";
            arrangeTopDownButton.Label = "Arrange Top-Down";
            arrangeTopDownButton.SuperTip = "Re-arranges the active SID or SBD page from its shapes, flowing top to bottom: states fall into layers downward, subjects line up in a column.";
            arrangeTopDownButton.Image = Properties.Resources.pageSetup;
            arrangeTopDownButton.ShowImage = true;
            arrangeTopDownButton.ControlSize = RibbonControlSize.RibbonControlSizeLarge;
            arrangeTopDownButton.Click += new RibbonControlEventHandler(this.ArrangeTopDown);
            owlGroup.Items.Add(arrangeTopDownButton);

            RibbonButton arrangeLeftRightButton = this.Factory.CreateRibbonButton();
            arrangeLeftRightButton.Name = "arrangeLeftRightButton";
            arrangeLeftRightButton.Label = "Arrange Left-Right";
            arrangeLeftRightButton.SuperTip = "Re-arranges the active SID or SBD page from its shapes, flowing left to right: states fall into layers rightward, subjects line up in a row.";
            arrangeLeftRightButton.Image = Properties.Resources.pageSetup;
            arrangeLeftRightButton.ShowImage = true;
            arrangeLeftRightButton.ControlSize = RibbonControlSize.RibbonControlSizeLarge;
            arrangeLeftRightButton.Click += new RibbonControlEventHandler(this.ArrangeLeftRight);
            owlGroup.Items.Add(arrangeLeftRightButton);

            // FEAT: ALPS verification tool
            // FEAT: PASS natural language checker
            // FEAT: PASS BPMN converter
        }

        /// <summary>
        /// Open file dialog and import OWL file.
        /// </summary>
        private void LoadOWLFile(object sender, RibbonControlEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "Ontology Files (.owl)|*.owl|RDF Files (*.rdf)|*.rdf"
            };

            if (dialog.ShowDialog() == DialogResult.OK) OWLImporter.Instance.Parse(dialog.FileName);
        }

        /// <summary>
        /// Open ALPS stencils.
        /// </summary>
        private void OpenStencils(object sender, RibbonControlEventArgs e)
        {
            VisioHelper.openStencil(VisioHelper.VisioStencils.SID_STENCIL);
        }

        /// <summary>
        /// Show the layer explorer.
        /// </summary>
        private void ShowLayerExplorer(object sender, RibbonControlEventArgs e)
        {
            Globals.ThisAddIn.showDirectoryClicked();
        }

        /// <summary>
        /// Re-arrange the active page from its shapes, flowing top to bottom.
        /// </summary>
        private void ArrangeTopDown(object sender, RibbonControlEventArgs e)
        {
            AutoArranger.ArrangeActivePage(Globals.ThisAddIn.Application, AutoArranger.LayoutDirection.TopToBottom);
        }

        /// <summary>
        /// Re-arrange the active page from its shapes, flowing left to right.
        /// </summary>
        private void ArrangeLeftRight(object sender, RibbonControlEventArgs e)
        {
            AutoArranger.ArrangeActivePage(Globals.ThisAddIn.Application, AutoArranger.LayoutDirection.LeftToRight);
        }
    }
}
