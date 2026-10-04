using Microsoft.Office.Interop.Visio;
using System.Windows.Forms;
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
            // Kein Dokument offen (z. B. nach dem Schliessen des letzten) — nichts zu tun.
            Visio.Document current = Application.ActiveDocument;
            if (current == null) return;

            // activeDoc kann null sein (Visio ohne Dokument gestartet) oder auf ein bereits
            // geschlossenes Dokument zeigen — dann wirft FullName eine COMException.
            string previous;
            try { previous = activeDoc?.FullName; }
            catch (System.Runtime.InteropServices.COMException) { previous = null; }

            // If no window change, return
            if (current.FullName.Equals(previous)) return;
            activeDoc = current;
            try { reset(); }
            catch (System.Exception e)
            {
                System.Diagnostics.Debug.WriteLine("[ThisAddIn] reset on window change failed: " + e);
            }
        }

        /// <summary>
        /// Called when a Page was added. Determines to which model the Page belongs to 
        /// </summary>
        private void Application_PageAdded(Page page)
        {
            // Seiten anderer Dokumente (z. B. Hintergrunddokumente, Schablonen) gehoeren nicht
            // zum Modell des aktiven Dokuments.
            if (!IsActiveDrawing(page.Document)) return;
            try
            {
                //let the model manager determine to what model the new Page belongs to
                modelManager.pageAdded(page);
                refreshLayerExplorerTreeView();
            }
            catch (System.Exception e)
            {
                // Ausnahmen aus COM-Events wuerden sonst still verschluckt bzw. den Handler kappen.
                System.Diagnostics.Debug.WriteLine("[ThisAddIn] PageAdded failed: " + e);
            }
        }

        private void Application_DocumentOpened(IVDocument doc)
        {
            onDrawingOpenedOrCreated(doc);
        }

        private void Application_DocumentCreated(IVDocument doc)
        {
            onDrawingOpenedOrCreated(doc);
        }

        /// <summary>
        /// DocumentOpened/-Created feuern auch fuer Schablonen — u. a. fuer die SID-/SBD-Schablone,
        /// die der Import selbst oeffnet. Ein reset() an dieser Stelle ersetzte den ModelController
        /// mitten in einem laufenden Import/setExtends. Nur echte Zeichnungen loesen den Reset aus.
        /// </summary>
        private void onDrawingOpenedOrCreated(IVDocument doc)
        {
            if (doc == null || doc.Type != VisDocumentTypes.visTypeDrawing) return;
            try
            {
                // Dokumente aus einem frueheren Import tragen die Steuerzelle evtl. noch auf "aus"
                // (wurde mit gespeichert) — dann lief u. a. das Umschalten des Shape-Sets nie mehr.
                if (doc is Visio.Document drawing && VisioHelper.AreVBAListenersOff(drawing))
                    VisioHelper.ReenableVBAListenersWhenIdle(drawing);

                activeDoc = Application.ActiveDocument;
                reset();
            }
            catch (System.Exception e)
            {
                System.Diagnostics.Debug.WriteLine("[ThisAddIn] reset after open/create failed: " + e);
            }
        }

        private bool IsActiveDrawing(Visio.Document doc)
        {
            Visio.Document active = Application.ActiveDocument;
            if (doc == null || active == null) return false;
            try { return doc.ID == active.ID && doc.Type == VisDocumentTypes.visTypeDrawing; }
            catch (System.Runtime.InteropServices.COMException) { return false; }
        }

        internal void updateClicked()
        {
            if (Application.ActiveDocument == null) return;
            modelManager.updateWholeController(Application.ActiveDocument.Pages);
            layerExplorer?.displayTreeView(modelManager.getTreeView());
        }

        internal ModelController getModelController()
        {
            return modelManager;
        }

        internal void extendsChanged(SIDPage extends, SIDPage changedPage)
        {
            if (Application.ActiveDocument == null) return;
            modelManager.updateWholeController(Application.ActiveDocument.Pages);
            //if (changedPage.)
            modelManager.updateBackground(extends, changedPage);
            layerExplorer?.displayTreeView(modelManager.getTreeView());
        }

        /// <summary>
        /// Visio-Fenster der aktuell angezeigten Anchor Bar (Layer Explorer), um sie bei einem
        /// erneuten Klick wieder einzublenden statt jedes Mal eine weitere anzulegen.
        /// </summary>
        private Visio.Window layerExplorerWindow;
        private int layerExplorerHostWindowId;

        internal void showDirectoryClicked()
        {
            if (Application.ActiveDocument == null) return;
            modelManager.updateWholeController(Application.ActiveDocument.Pages);

            if (!TryReshowLayerExplorer())
            {
                //Methods are not used due to a problem with the setParent-Method regarding the anchor-bar
                AnchorBarsUsage ancBar = new AnchorBarsUsage(this, modelManager);
                layerExplorer = ancBar.CreateAnchorBar(Application);
                layerExplorerWindow = ancBar.AnchorWindow;
                layerExplorerHostWindowId = Application.ActiveWindow.ID;
            }

            layerExplorer.displayTreeView(modelManager.getTreeView());
        }

        /// <summary>
        /// Blendet die vorhandene Anchor Bar wieder ein, sofern sie noch existiert und zum
        /// aktiven Zeichnungsfenster gehoert. Sonst false (dann wird eine neue angelegt).
        /// </summary>
        private bool TryReshowLayerExplorer()
        {
            if (layerExplorer == null || layerExplorerWindow == null) return false;
            try
            {
                if (layerExplorerHostWindowId == Application.ActiveWindow.ID)
                {
                    // Nur wiederverwenden, wenn die Anchor Bar noch zum Zeichnungsfenster gehoert
                    // (vom Nutzer geschlossene Anchor Bars verschwinden aus dessen Windows-Liste).
                    int explorerId = layerExplorerWindow.ID;
                    foreach (Visio.Window child in Application.ActiveWindow.Windows)
                    {
                        if (child.ID != explorerId) continue;
                        child.Visible = true;
                        return true;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // Anchor Bar wurde geschlossen bzw. ihr Zeichnungsfenster existiert nicht mehr.
            }
            layerExplorerWindow = null;
            return false;
        }
        private void reset()
        {
            if (Application.ActiveDocument == null) return;
            this.modelManager?.detach();
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
