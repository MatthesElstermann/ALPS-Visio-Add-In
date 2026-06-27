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

            // Group order and labels mirror the original add-in (upstream/main).

            // --- Group 1: Standard Functions ---
            RibbonGroup standardGroup = this.Factory.CreateRibbonGroup();
            standardGroup.Label = "Standard Functions";
            alpsTab.Groups.Add(standardGroup);

            RibbonButton openStencilsButton = this.Factory.CreateRibbonButton();
            openStencilsButton.Name = "openStencilsButton";
            openStencilsButton.Label = "Open ALPS/PASS Stencils";
            openStencilsButton.SuperTip = "Tries to open the (necessary) ALPS Visio stencils if they are available on the system.";
            openStencilsButton.Image = Properties.Resources.document_open_7;
            openStencilsButton.ShowImage = true;
            openStencilsButton.ControlSize = RibbonControlSize.RibbonControlSizeLarge;
            openStencilsButton.Click += new RibbonControlEventHandler(this.OpenStencils);
            standardGroup.Items.Add(openStencilsButton);

            // --- Group 2: ALPS Layer Editing ---
            RibbonGroup layerGroup = this.Factory.CreateRibbonGroup();
            layerGroup.Label = "ALPS Layer Editing";
            alpsTab.Groups.Add(layerGroup);

            RibbonButton layerExplorerButton = this.Factory.CreateRibbonButton();
            layerExplorerButton.Name = "layerExplorerButton";
            layerExplorerButton.Label = "Show layer Explorer";
            layerExplorerButton.SuperTip = "Open a the layer explorer, a tool for advanced multi-layered ALPS (Abstract Layered PASS editing)";
            layerExplorerButton.OfficeImageId = "LayersMenu";
            layerExplorerButton.ShowImage = true;
            layerExplorerButton.ControlSize = RibbonControlSize.RibbonControlSizeLarge;
            layerExplorerButton.Click += new RibbonControlEventHandler(this.ShowLayerExplorer);
            layerGroup.Items.Add(layerExplorerButton);

            // --- Group 3: OWL PASS Tools ---
            RibbonGroup owlGroup = this.Factory.CreateRibbonGroup();
            owlGroup.Label = "OWL PASS Tools";
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

            // Carried over from the original add-in; not implemented yet (stub).
            RibbonButton verificationButton = this.Factory.CreateRibbonButton();
            verificationButton.Name = "verificationButton";
            verificationButton.Label = "ALPS Verification";
            verificationButton.SuperTip = "Open the verification tool to check if a given model adheres to a given specification (abstract) model.";
            verificationButton.OfficeImageId = "AdpDiagramArrangeTables";
            verificationButton.ShowImage = true;
            verificationButton.ControlSize = RibbonControlSize.RibbonControlSizeLarge;
            verificationButton.Click += new RibbonControlEventHandler(this.AlpsVerification);
            owlGroup.Items.Add(verificationButton);

            // Carried over from the original add-in; not implemented yet (stub).
            RibbonButton naturalLanguageButton = this.Factory.CreateRibbonButton();
            naturalLanguageButton.Name = "naturalLanguageButton";
            naturalLanguageButton.Label = "PASS NL Checker";
            naturalLanguageButton.SuperTip = "Check a PASS model against a natural-language description.";
            naturalLanguageButton.Image = Properties.Resources.pageSetup;
            naturalLanguageButton.ShowImage = true;
            naturalLanguageButton.ControlSize = RibbonControlSize.RibbonControlSizeLarge;
            naturalLanguageButton.Click += new RibbonControlEventHandler(this.NotImplemented);
            owlGroup.Items.Add(naturalLanguageButton);

            // Carried over from the original add-in; not implemented yet (stub).
            RibbonButton bpmnButton = this.Factory.CreateRibbonButton();
            bpmnButton.Name = "bpmnButton";
            bpmnButton.Label = "PASS BPMN Converter";
            bpmnButton.SuperTip = "Convert between PASS and BPMN process models.";
            bpmnButton.Image = Properties.Resources.pageSetup;
            bpmnButton.ShowImage = true;
            bpmnButton.ControlSize = RibbonControlSize.RibbonControlSizeLarge;
            bpmnButton.Click += new RibbonControlEventHandler(this.NotImplemented);
            owlGroup.Items.Add(bpmnButton);

            RibbonMenu arrangeMenu = this.Factory.CreateRibbonMenu();
            arrangeMenu.Name = "arrangeMenu";
            arrangeMenu.Label = "Auto Arrange";
            arrangeMenu.SuperTip = "Re-arranges the active SID or SBD page from its shapes. Pick the direction the layout flows.";
            arrangeMenu.Image = Properties.Resources.pageSetup;
            arrangeMenu.ShowImage = true;
            arrangeMenu.ControlSize = RibbonControlSize.RibbonControlSizeLarge;

            RibbonButton arrangeTopDownItem = this.Factory.CreateRibbonButton();
            arrangeTopDownItem.Name = "arrangeTopDownItem";
            arrangeTopDownItem.Label = "Top-Down";
            arrangeTopDownItem.SuperTip = "States fall into layers downward; subjects line up in a column.";
            arrangeTopDownItem.Click += new RibbonControlEventHandler(this.ArrangeTopDown);
            arrangeMenu.Items.Add(arrangeTopDownItem);

            RibbonButton arrangeLeftRightItem = this.Factory.CreateRibbonButton();
            arrangeLeftRightItem.Name = "arrangeLeftRightItem";
            arrangeLeftRightItem.Label = "Left-Right";
            arrangeLeftRightItem.SuperTip = "States fall into layers rightward; subjects line up in a row.";
            arrangeLeftRightItem.Click += new RibbonControlEventHandler(this.ArrangeLeftRight);
            arrangeMenu.Items.Add(arrangeLeftRightItem);

            owlGroup.Items.Add(arrangeMenu);
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
        /// ALPS verification tool — placeholder, not implemented yet.
        /// </summary>
        private void AlpsVerification(object sender, RibbonControlEventArgs e)
        {
            NotImplemented(sender, e);
        }

        /// <summary>
        /// Shared placeholder for ribbon buttons whose feature is not implemented yet.
        /// </summary>
        private void NotImplemented(object sender, RibbonControlEventArgs e)
        {
            MessageBox.Show("Diese Funktion ist noch nicht implementiert.", "ALPS/PASS Add-In",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
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
