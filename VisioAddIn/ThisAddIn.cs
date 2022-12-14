using Microsoft.Office.Interop.Visio;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using VisioAddIn.Snapping;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisioAddIn
{

    public interface IAddInCallback
    {

    }

    /// <summary>
    /// main class of the Add-In.
    /// It initializes all the controllers and starts the Add-In.
    /// </summary>
    public partial class ThisAddIn
    {
        /// <summary>
        /// The active Visio document this Add-In operates in
        /// </summary>
        private Visio.Document activeDoc;


        /// <summary>
        /// reference to Directory where TreeView etc is displayed.
        /// </summary>
        private WindowDirectory layerExplorer;


        /// <summary>
        /// reference to the ModelManager where the data is maintained 
        /// </summary>
        private ModelController modelManager;

        private static ThisAddIn currentInstance;

        public static ThisAddIn getInstance()
        {
            return currentInstance;
        }

        /// <summary>
        /// Startup method of the Add-In
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ThisAddIn_Startup(object sender, System.EventArgs e)
        {
            currentInstance = this;

            // Creates the ribbon (displaying functionality buttons), reference not needed furthermore
            ALPSRibbon alpsRibbon = new ALPSRibbon();


            modelManager = new ModelController(this);

            // Add triggers for methods to be called when an updateWholeController in visio occurs
            Application.DocumentCreated += Application_DocumentCreated;
            Application.PageAdded += Application_PageAdded;
            Application.WindowActivated += Application_WindowActivated;
            Application.DocumentOpened += Application_DocumentOpened;

            // Set the current active document
            activeDoc = Application.ActiveDocument;

            SiSi_SimpleSim.setAddin(this);
        }


        /* ---------------------------------------------------------------------
         *                 Methods triggered by changes in Visio 
         * --------------------------------------------------------------------- */

        /// <summary>
        /// Called when the active window in the document changes.
        /// Checks whether the active document is still the same or not.
        /// </summary>
        /// <param name="window">The active window, not used by this function</param>
        private void Application_WindowActivated(Window window)
        {
            // If no window change, return
            if (activeDoc.FullName.Equals(Application.ActiveDocument.FullName)) return;
            activeDoc = Application.ActiveDocument;
            reset();
        }

        /// <summary>
        /// Called when a Page was added. Determines to which model the Page belongs to 
        /// </summary>
        private void Application_PageAdded(Page page)
        {
            //let the model manager determine to what model the new Page belongs to
            modelManager.pageAdded(page);

            refreshLayerExplorerTreeView();
        }

        private void Application_DocumentOpened(IVDocument doc)
        {
            activeDoc = Application.ActiveDocument;
            reset();
        }

        private void Application_DocumentCreated(IVDocument doc)
        {
            activeDoc = Application.ActiveDocument;
            reset();
        }

        // ---------------------------------------------------------------------


        /// <summary>
        /// Opens the latest SID and SBD shapes which are found in the shapes folder specified by the visio user.
        /// </summary>
        internal void openShapesClicked()
        {
            if (Application.Documents.Count < 1)
            {
                Application.Documents.Add("");
            }

            // Stencils laden automatisch die zugehörige Datei nach (SID->SBD), also zweiter Aufruf nicht benötigt.
            VisioHelper.openStencil(VisioHelper.VisioStencils.SBD_STENCIL);
            VisioHelper.openStencil(VisioHelper.VisioStencils.SID_STENCIL);

        }



        /// <summary>
        /// Refreshes the tree view in the layer Explorer
        /// </summary>
        public void refreshLayerExplorerTreeView()
        {
            layerExplorer?.displayTreeView(modelManager.getTreeView());
        }

        // Called by ribbon, button does not exist anymore -> method is not used
        internal void versionButtonClicked()
        {

            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            MessageBox.Show("Version: " + version, "Version");
        }

        /// <summary>
        /// called when the priority of a page was changed through the directory
        /// </summary>
        /// <param name="newProperty">new priority</param>
        /// <param name="changedPage">Page where priority was changed</param>
        internal void priorityChanged(string newProperty, SIDPage changedPage)
        {
            modelManager.updateWholeController(Application.ActiveDocument.Pages);
            layerExplorer.displayTreeView(modelManager.getTreeView());
        }

        /// <summary>
        /// called when the Extends property of a Page was changed
        /// </summary>
        /// <param name="extends">new extended Page</param>
        /// <param name="changedPage">Page where prop was changed</param>
        internal void extendsChanged(SIDPage extends, SIDPage changedPage)
        {
            modelManager.updateWholeController(Application.ActiveDocument.Pages);
            //if (changedPage.)
            modelManager.updateBackground(extends, changedPage);
            layerExplorer.displayTreeView(modelManager.getTreeView());
        }

        /// <summary>
        /// updates data and displays it
        /// </summary>
        internal void updateClicked()
        {
            modelManager.updateWholeController(Application.ActiveDocument.Pages);
            layerExplorer.displayTreeView(modelManager.getTreeView());
        }

        

        /// <summary>
        /// called by ribbon.
        /// opens a user dialog to choose the file that should be loaded
        /// and creates the visio diagram out of it.
        /// </summary>
        public void createGraphFromOwlClicked()
        {

            //Possible to design new window
            //DialogResult result = MessageBox.Show("OWL will be importet to a new document (approve with 'yes')." +
            //                                      "If you like to import the owl into the current document click 'No'." + Environment.NewLine +
            //    "Please note: for a correct import result, the \"ALPS/S-BPM\"-macro shouldn't be running. If the macro is already running, please use the \"Stop Makros\"-Button of the macro." +
            //    "If the macro isn't running, select \"deacitvate\" in the case of the macro-dialog."
            //    , "OWL-Import", MessageBoxButtons.YesNoCancel);

            DialogResult result = MessageBox.Show("Import OWL to new document? (Press No to use the current)"
                , "OWL - Import", MessageBoxButtons.YesNoCancel);

            string fileName;
            IList<string> path = new List<string>();

            // Show file picker, save selected filename in fileName
            if (result == DialogResult.Cancel || "".Equals(fileName = showFileDialog())) return;
            // Open in another Document selected
            if (DialogResult.Yes == result)
            {
                Document newDoc = Application.Documents.Add("");
            }

            // Create new importer with file
            OWLImporter parser = new OWLImporter(fileName);
            VisioHelper.openStencil(VisioHelper.VisioStencils.SID_STENCIL); ;

            //Parse the first page in the document
            if (Application.ActiveDocument.Pages.Count > 0)
                parser.parse(VisioHelper.getPageInPages(Application.ActiveDocument.Pages, 0), activeDoc);

            // Delete pages not containing PASS content
            foreach (var page in Application.ActiveDocument.Pages.Cast<Page>()
                         .Where(page => page.PageSheet.CellExistsU["Prop." + ALPSConstants.alpsPropertieTypePageType, 1] == 0))
            {
                page.Delete(0);
            }

            //doc.SaveAs(docName);

            /*Visio.Page page = parser.CreateSIDPage("SID_5_foo", "SBD_37_bar", "http://foo.bar", "", "", "");
            Visio.Shape shape = parser.PlaceStandardActor(page, "standardActor", "Subject 1234", "SubjektNull", "2");
            parser.CreateSBDPage(page, "SBD_23_unique", "SBD_23_unique", shape);
            page.LayoutIncremental(Visio.VisLayoutIncrementalType.visLayoutIncrAlign,
                Visio.VisLayoutHorzAlignType.visLayoutHorzAlignDefault,
                Visio.VisLayoutVertAlignType.visLayoutVertAlignDefault, 
                1.0, 1.0, Visio.VisUnitCodes.visMillimeters);*/
        }

        // Testmethod - which purpose?
        public void testButtonClicked()
        {
            Master masterShape = VisioHelper.openStencil(VisioHelper.VisioStencils.SID_STENCIL).Masters.get_ItemU(ALPSConstants.alpsSIDMasterMessageBox);
            Page page = Application.ActivePage;
            Shape messagebox = page.Drop(masterShape, 4.25, 4.25);

            MessageBox.Show("Continue?", "Continue?", MessageBoxButtons.OKCancel);

            masterShape = VisioHelper.openStencil(VisioHelper.VisioStencils.SID_STENCIL).Masters.get_ItemU(ALPSConstants.alpsSIDMasterMessage);
            Shape message = page.Drop(masterShape, 2, 2);
            //message.CellsU["Prop.lable"].Formula = "Message" + count;

            MessageBox.Show("Wait");

            messagebox.ContainerProperties.InsertListMember(message, 1);

            messagebox.CellsU["PinX"].Formula = 5.ToString();

        }

        /// <summary>
        /// Shows a simple file dialog to create a new file in a directory
        /// </summary>
        /// <returns>The name of the new file</returns>
        private static string showFileDialog()
        {
            string fileName = "";

            // Create an instance of the open file dialog box.
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            // Set filter options and filter index.
            openFileDialog1.Filter = "Ontology Files (.owl)|*.owl|RDF Files (*.rdf)|*.rdf";
            openFileDialog1.FilterIndex = 1;

            openFileDialog1.Multiselect = true;

            // Call the ShowDialog method to show the dialog box.
            DialogResult userClickedOk = openFileDialog1.ShowDialog();

            // Process input if the user clicked OK.
            if (userClickedOk == DialogResult.OK)
            {
                fileName = openFileDialog1.FileName;
            }
            return fileName;
        }

        /// <summary>
        /// Saves a file with a given default filename using a dialog
        /// </summary>
        /// <param name="fileName">The default name of the file</param>
        public void openSaveFileDialog(string fileName)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = fileName;
            saveFileDialog.ShowDialog();
            string saveFileName = saveFileDialog.FileName;
            Application.ActiveDocument.SaveAs(saveFileName);
        }


        /// <summary>
        /// called when the button for showing Directory was clicked.
        /// </summary>
        internal void showDirectoryClicked()
        {
            modelManager.updateWholeController(Application.ActiveDocument.Pages);

            //Methods are not used due to a problem with the setParent-Method regarding the anchor-bar
            AnchorBarsUsage ancBar = new AnchorBarsUsage(this, modelManager);
            layerExplorer = ancBar.CreateAnchorBar(Application);

            // TemporaryModelExplorerController controller = new TemporaryModelExplorerController(this, ModelManager);
            // Directory = controller.getDirectory();
            layerExplorer.displayTreeView(modelManager.getTreeView());
        }



        internal void changeModel(SIDPage changed, IVisioProcessModel newModel, IVisioProcessModel oldModel)
        {
            modelManager.changeModelForSidPage(changed, newModel, oldModel);
        }

        /// <summary>
        /// resets the ModelManager. 
        /// Should be called if the active document changed.
        /// Updates the internal ModelManager
        /// </summary>
        private void reset()
        {
            this.modelManager = new ModelController(this);
            modelManager.updateWholeController(Application.ActiveDocument.Pages);
            layerExplorer?.displayTreeView(modelManager.getTreeView());
        }

        private void ThisAddIn_Shutdown(object sender, System.EventArgs e)
        {
            //TODO ?
        }


        public ModelController getModelController()
        {
            return modelManager;
        }

        #region Von VSTO generierter Code


        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InternalStartup()
        {
            this.Startup += new System.EventHandler(ThisAddIn_Startup);
            this.Shutdown += new System.EventHandler(ThisAddIn_Shutdown);
        }

        #endregion
    }
}
