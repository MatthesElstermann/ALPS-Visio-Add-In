using Microsoft.Office.Interop.Visio;
using System.Windows.Forms;
using VisioAddIn;
using VisioAddIn.Snapping;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite
{
    public partial class ThisAddIn
    {
        /// <summary>
        /// Entrypoint of this AddIn.
        /// </summary>
        private void ThisAddIn_Startup(object sender, System.EventArgs e)
        {
            prepStuff();
        }

        #region Von VSTO generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InternalStartup()
        {
            this.Startup += new System.EventHandler(ThisAddIn_Startup);
        }

        #endregion

        #region Code from old Project (i did not refactor this)

        private void prepStuff()
        {
            modelManager = new ModelController(this);

            // Add triggers for methods to be called when an updateWholeController in visio occurs
            Application.DocumentCreated += Application_DocumentCreated;
            Application.PageAdded += Application_PageAdded;
            Application.WindowActivated += Application_WindowActivated;
            Application.DocumentOpened += Application_DocumentOpened;

            // Set the current active document
            activeDoc = Application.ActiveDocument;
        }

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

        internal void updateClicked()
        {
            modelManager.updateWholeController(Application.ActiveDocument.Pages);
            layerExplorer.displayTreeView(modelManager.getTreeView());
        }

        internal ModelController getModelController()
        {
            return modelManager;
        }

        internal void extendsChanged(SIDPage extends, SIDPage changedPage)
        {
            modelManager.updateWholeController(Application.ActiveDocument.Pages);
            //if (changedPage.)
            modelManager.updateBackground(extends, changedPage);
            layerExplorer.displayTreeView(modelManager.getTreeView());
        }

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
        private void reset()
        {
            this.modelManager = new ModelController(this);
            modelManager.updateWholeController(Application.ActiveDocument.Pages);
            layerExplorer?.displayTreeView(modelManager.getTreeView());
        }

        public void refreshLayerExplorerTreeView()
        {
            layerExplorer?.displayTreeView(modelManager.getTreeView());
        }

        #endregion
    }
}
