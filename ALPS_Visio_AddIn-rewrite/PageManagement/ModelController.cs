using Microsoft.Office.Interop.Visio;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace ALPS_Visio_AddIn_rewrite
{
    /// <summary>
    /// The entry class for the snapping module.
    /// Keeps overview of the managed SID and SBD pages.
    /// </summary>
    public class ModelController
    {
        private readonly ThisAddIn addIn;

        private ISet<IVisioProcessModel> models;

        private IDictionary<IVisioProcessModel, ISet<SIDPageController>> modelToSidController;
        private IDictionary<SIDPage, ISet<SBDPageController>> sidPageToSbdController;
        private IDictionary<int, Page> possibleSidOrSbdPages = new Dictionary<int, Page>();

        public ModelController(ThisAddIn addIn)
        {
            Debug.Print("creating ModelController");
            this.addIn = addIn;
            this.models = new HashSet<IVisioProcessModel>();
            this.modelToSidController = new Dictionary<IVisioProcessModel, ISet<SIDPageController>>();
            this.sidPageToSbdController = new Dictionary<SIDPage, ISet<SBDPageController>>();
        }

        private static readonly string[] sidPageCompleteIfCellsExists = {
            "Prop." + Constants.Properties.PageModelURI,
            "Prop." + Constants.Properties.PageType,
            "Prop." + Constants.Properties.PageLayer,
            "Prop." + Constants.Properties.PageModelVersion,
            "Prop." + Constants.Properties.PriorityOrderNumber,
        };

        /// <summary>
        /// Called by Visio when a Page is added to the current document
        /// </summary>
        internal void pageAdded(Page page)
        {
            if (isSid(page))
            {
                // Erst aus der Warteliste austragen, dann registrieren: die Registrierung schreibt
                // selbst Zellen (PriorityOrderNumber) — mit noch aktivem Abo lief pageAdded
                // re-entrant ein zweites Mal und legte einen doppelten Controller an.
                stopWatching(page);
                registerNewSidPage(page);
            }
            else if (isSbd(page))
            {
                // Eine SBD-Seite ist erst registrierbar, wenn ihr pageLayer gesetzt ist. Bis dahin
                // in der Warteliste lassen, sonst ginge sie bis zum naechsten Reset verloren.
                stopWatching(page);
                if (!registerNewSbdPage(page))
                    startWatching(page);
            }
            else
            {
                startWatching(page);
            }
        }

        private void startWatching(Page page)
        {
            if (possibleSidOrSbdPages.ContainsKey(page.ID)) return;
            possibleSidOrSbdPages.Add(page.ID, page);
            page.CellChanged += onCellChangedOnPossibleSidOrSbdPage;
        }

        private void stopWatching(Page page)
        {
            if (!possibleSidOrSbdPages.ContainsKey(page.ID)) return;
            possibleSidOrSbdPages.Remove(page.ID);
            page.CellChanged -= onCellChangedOnPossibleSidOrSbdPage;
        }

        /// <summary>
        /// Checks if a visio page is a SID page, meaning all the relevant cells exist in the PageSheet
        /// and the type cell contains a value stating that it is a SID page
        /// </summary>
        private static bool isSid(Page page)
        {
            if (sidPageCompleteIfCellsExists.Any(cell => page.PageSheet.CellExistsU[cell, 1] == 0))
                return false;
            string pageType = page.PageSheet.CellsU["Prop." + Constants.Properties.PageType].Formula;
            return pageType.Contains("SubjectInteraction");
        }

        /// <summary>
        /// Checks if a visio page is a SBD page, meaning all the relevant cells exist in the PageSheet
        /// and the type cell contains a value stating that it is a SBD page
        /// </summary>
        private static bool isSbd(Page page)
        {
            return page.PageSheet.CellExistsU["Prop." + Constants.Properties.SBDLinkedSubjectID, 1] != 0;
        }

        private void onCellChangedOnPossibleSidOrSbdPage(Cell cell)
        {
            // COM-Event-Handler: Ausnahmen nicht nach Visio durchreichen.
            try
            {
                if (possibleSidOrSbdPages.TryGetValue(cell.ContainingPageID, out Page page))
                    pageAdded(page);
            }
            catch (System.Exception e)
            {
                Debug.WriteLine("[ModelController] page registration failed: " + e);
            }
        }

        private void registerNewSidPage(Page page)
        {
            Cell cell = page.PageSheet.CellsU["Prop." + Constants.Properties.PageModelURI];
            IVisioProcessModel model = getOrCreateModel(cell.Formula);

            if (model.containsSidPage(page.NameU)) return;

            SIDPageController pageController = SIDPageController.getController(addIn, this, cell.Formula, page);
            SIDPage sidPageWrapper = pageController.getSidPage();

            model.addSidPage(sidPageWrapper);
            modelToSidController[model].Add(pageController);
            sidPageToSbdController.Add(sidPageWrapper, new HashSet<SBDPageController>());
        }

        /// <returns>false, solange die Seite (noch) keinen pageLayer traegt.</returns>
        private bool registerNewSbdPage(Page sbdPage)
        {
            if (sbdPage.PageSheet.CellExistsU["Prop." + Constants.Properties.PageLayer, 1] == 0) return false;
            string pageLayer = sbdPage.PageSheet.CellsU["Prop." + Constants.Properties.PageLayer].Formula;
            if (string.IsNullOrWhiteSpace(pageLayer.Trim('"'))) return false;

            foreach (IVisioProcessModel model in models)
            {
                foreach (SIDPage sidPage in model.getSidPages())
                {
                    if (!sidPage.getLayer().Equals(pageLayer)) continue;

                    SIDPageController sidController = getSidPageController(sidPage);
                    if (sidController != null && sidController.addSbdPageAndCreateNewController(sbdPage, out SBDPageController sbdController))
                        sidPageToSbdController[sidPage].Add(sbdController);
                }
            }
            return true;
        }

        public void updateExtends(SIDPageController modifiedC, SIDPage modifiedP, string userInput)
        {
            SIDPage extending = getSidPage(userInput) ?? registerSidPageWithLayer(modifiedC.getPage().Document, userInput);
            SIDPage oldExtends = modifiedC.getExtends();

            if (extending != null || string.IsNullOrWhiteSpace(userInput))
            {
                SIDPageController extendingC = getSidPageController(extending);
                extendingC?.setExtended(modifiedP);
                modifiedC.setExtends(extending);

                if (oldExtends == null) return;
                SIDPageController oldExtendsC = getSidPageController(oldExtends);
                if (oldExtendsC != null && (extendingC == null || !oldExtendsC.getNameU().Equals(extendingC.getNameU())))
                    oldExtendsC.setNotExtended();
            }
            else
            {
                ALPS_Visio_AddIn_rewrite.UI.ResultDialog.ShowWarning("Eingabe nicht gefunden",
                    "„" + userInput + "“ konnte nicht aufgelöst werden.",
                    "Ort der fehlerhaften Eingabe: „" + modifiedC.getNameU() + "“");
            }
        }

        /// <summary>
        /// Sucht im Dokument eine noch nicht registrierte SID-Seite mit dem angegebenen pageLayer und
        /// registriert sie nach (z. B. eine Seite, die die Schablone erst nachtraeglich zur SID-Seite
        /// gemacht hat). Liefert die registrierte Seite oder null.
        /// </summary>
        private SIDPage registerSidPageWithLayer(Document document, string layerName)
        {
            if (document == null || string.IsNullOrWhiteSpace(layerName)) return null;
            string wanted = layerName.Trim('\\', '"');
            foreach (Page page in document.Pages)
            {
                if (!isSid(page)) continue;
                string layer = page.PageSheet.CellsU["Prop." + Constants.Properties.PageLayer].ResultStr[""];
                if (!wanted.Equals(layer)) continue;
                stopWatching(page);
                registerNewSidPage(page);
                return getSidPage(layerName);
            }
            return null;
        }

        public SIDPage getSidPage(string layerName)
        {
            return models.SelectMany(model => model.getSidPages())
                .FirstOrDefault(sidPage => sidPage.getLayerForUser().Equals(layerName));
        }

        public SIDPageController getSidPageController(DiagramPage searched)
        {
            if (searched == null) return null;
            return modelToSidController.Keys.SelectMany(model => modelToSidController[model])
                .FirstOrDefault(c => c.getSidPage().getNameU().Equals(searched.getNameU()));
        }

        public void backgroundShapeMoved(Shape shape, SIDPage foreground)
        {
            getSidPageController(foreground)?.backgroundShapeMoved(shape);
        }

        public void backgroundShapeMoved(Shape shape, SBDPage foreground)
        {
            getSbdPageController(foreground)?.backgroundShapeMoved(shape);
        }

        public SBDPage getSbdPage(string nameU)
        {
            return (from model in models from sidPage in model.getSidPages() from sbdPage in sidPage.getSbdPages() select sbdPage)
                .FirstOrDefault(sbdPage => sbdPage.getNameU().Equals(nameU));
        }

        public SBDPageController getSbdPageController(DiagramPage toFind)
        {
            if (toFind == null) return null;
            return sidPageToSbdController.Keys.SelectMany(sidPage => sidPageToSbdController[sidPage])
                .FirstOrDefault(c => c.getNameU().Equals(toFind.getNameU()));
        }

        public IList<SIDPage> getExtendableSidPages(SIDPage givenPage)
        {
            string givenLayer = givenPage.getLayer();
            return (from model in models from sidPage in model.getSidPages()
                    where !sidPage.getLayer().Equals(givenLayer) select sidPage).ToList();
        }

        public void setActivePage(SIDPage sidPage)
        {
            SIDPageController sidPageC = getSidPageController(sidPage);
            if (sidPageC != null) showPage(sidPageC.getPage());
        }

        public void setActivePage(SBDPage sbdPage)
        {
            SBDPageController sbdPageC = getSbdPageController(sbdPage);
            if (sbdPageC != null) showPage(sbdPageC.getPage());
        }

        /// <summary>
        /// Zeigt die Seite im aktiven Fenster — nur, wenn sie zu dessen Dokument gehoert
        /// (eine Seite eines anderen Dokuments loest sonst eine COMException im Klick-Handler aus).
        /// </summary>
        private void showPage(Page page)
        {
            try
            {
                Microsoft.Office.Interop.Visio.Window window = addIn.Application.ActiveWindow;
                if (window?.Document == null || page.Document.ID != window.Document.ID) return;
                window.Page = page;
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                Debug.WriteLine("[ModelController] could not activate page: " + e.Message);
            }
        }

        public void changeLayerName(SIDPage changed, string newName)
        {
            SIDPageController pageController = getSidPageController(changed);
            if (pageController == null) return;
            pageController.setLayerName(newName);
            if (changed.getForeground() != null)
            {
                getSidPageController(changed.getForeground())?.setExtendsCell(changed.getLayer());
            }
            addIn.refreshLayerExplorerTreeView();
        }

        internal void changeModelForSidPage(SIDPage changed, IVisioProcessModel newModel, IVisioProcessModel oldModel)
        {
            oldModel.removePage(changed);
            newModel.addSidPage(changed);
            getSidPageController(changed)?.setModelUri(newModel.getModelUri());
        }

        public void moveSidPageToNewModel(SIDPageController pageController, string newModelUri)
        {
            SIDPage sidPage = pageController.getSidPage();

            IVisioProcessModel newModel = getOrCreateModel(newModelUri);
            IVisioProcessModel oldModel = getOrCreateModel(sidPage.getModelUri());

            oldModel.removePage(sidPage);
            modelToSidController[oldModel].Remove(pageController);

            pageController.setModelUri(newModelUri);

            if (!newModel.containsSidPage(sidPage.getNameU()))
                newModel.addSidPage(sidPage);
            modelToSidController[newModel].Add(pageController);

            addIn.refreshLayerExplorerTreeView();
        }

        /// <summary>
        /// resets the data and builds the data structure completely new.
        /// </summary>
        internal void updateWholeController(Pages pages)
        {
            detachSbdControllers();
            SIDPageController.pruneClosedControllers();
            models = new HashSet<IVisioProcessModel>();
            this.modelToSidController = new Dictionary<IVisioProcessModel, ISet<SIDPageController>>();
            this.sidPageToSbdController = new Dictionary<SIDPage, ISet<SBDPageController>>();

            foreach (var page in pages.Cast<Page>().Where(isSid))
                registerNewSidPage(page);
            foreach (var page in pages.Cast<Page>().Where(isSbd))
            {
                if (!registerNewSbdPage(page)) startWatching(page);
            }
            // Auch schon vorhandene, (noch) leere Seiten beobachten: die Schablonen-VBA wandelt z. B.
            // die Startseite einer neuen Zeichnung nachtraeglich in eine SID-Seite um. Ohne Abo
            // wurde diese nie registriert — extends-Verweise darauf liefen ins Leere ("SID_1
            // konnte nicht aufgeloest werden") und das Snapping fiel aus.
            foreach (var page in pages.Cast<Page>().Where(page => !isSid(page) && !isSbd(page)))
                startWatching(page);
            foreach (var sidPageC in modelToSidController.SelectMany(pair => pair.Value))
                sidPageC.updateExtends();

            // Hintergrund der SBD/GBD-Seiten aus den (jetzt bekannten) SID-Ebenen ableiten.
            // Frueher geschah das nur bei einer Shape-Aenderung auf der Seite — nach einem Import
            // blieb die GBD ohne Hintergrund, bis der Nutzer etwas verschob.
            foreach (SBDPageController sbdPageC in sidPageToSbdController.Values.SelectMany(set => set).ToList())
            {
                try { sbdPageC.tryDeriveExtends(); }
                catch (System.Exception e) { Debug.WriteLine("[ModelController] deriving SBD background failed: " + e.Message); }
            }
        }

        private void detachSbdControllers()
        {
            foreach (SBDPageController controller in sidPageToSbdController.Values.SelectMany(set => set))
                controller.detach();
        }

        /// <summary>
        /// Meldet alle Visio-Events dieses Controllers ab, bevor ThisAddIn.reset() ihn ersetzt.
        /// </summary>
        internal void detach()
        {
            detachSbdControllers();
            foreach (Page page in possibleSidOrSbdPages.Values)
            {
                try { page.CellChanged -= onCellChangedOnPossibleSidOrSbdPage; }
                catch (System.Runtime.InteropServices.COMException) { /* Seite bereits geschlossen */ }
            }
            possibleSidOrSbdPages.Clear();
        }

        public int getCurrentPriority(string modelUri)
        {
            return getOrCreateModel(modelUri).getCurrentPriority();
        }

        private IVisioProcessModel getOrCreateModel(string modelUri)
        {
            IVisioProcessModel fittingModel = models.FirstOrDefault(model => modelUri.Equals(model.getModelUri()));
            if (fittingModel != null) return fittingModel;

            fittingModel = new VisioProcessModel(modelUri);
            models.Add(fittingModel);
            modelToSidController.Add(fittingModel, new HashSet<SIDPageController>());
            return fittingModel;
        }

        internal void updatePagePriority(string newProperty, SIDPageController changed)
        {
            changed.setPriorityOrder(newProperty);
        }

        public void updatePagePriority(int newProperty, SIDPage changed)
        {
            getSidPageController(changed)?.setPriorityOrder(newProperty);
        }

        internal void updateBackground(SIDPage newProperty, SIDPage changedPage)
        {
            SIDPageController newPropC = getSidPageController(newProperty);
            SIDPageController changedC = getSidPageController(changedPage);

            if (!newProperty.getLayer().Equals(changedPage.getLayer()))
            {
                SIDPage oldExtends = changedC.getExtends();
                if ((oldExtends == null || oldExtends.getLayer().Equals(newProperty.getLayer())) && oldExtends != null) return;
                newPropC.setExtended(changedPage);
                changedC.setExtends(newProperty);
                if (oldExtends == null) return;
                getSidPageController(oldExtends).setNotExtended();
            }
            else
            {
                SIDPage oldExtends = changedC.getExtends();
                changedC.setExtends(null);
                if (oldExtends == null) return;
                getSidPageController(oldExtends).setNotExtended();
            }
        }

        internal IDictionary<IVisioProcessModel, IDictionary<SIDPage, IList<SBDPage>>> getTreeView()
        {
            IDictionary<IVisioProcessModel, IDictionary<SIDPage, IList<SBDPage>>> treeView =
                new Dictionary<IVisioProcessModel, IDictionary<SIDPage, IList<SBDPage>>>();
            foreach (IVisioProcessModel model in models)
                treeView.Add(model, model.getTreeView());
            return treeView;
        }
    }
}
