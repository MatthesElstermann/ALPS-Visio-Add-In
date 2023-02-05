using Microsoft.Office.Interop.Visio;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using VisioAddIn;

namespace VisioAddIn.Snapping
{

    /// <summary>
    /// The entry class for the snapping module
    /// Keeps overview of the managed SID and SBD pages
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
            ALPSConstants.cellValuePropertyPageModelURI,
            ALPSConstants.cellValuePropertyPageType,
            ALPSConstants.cellValuePropertyPageLayer,
            ALPSConstants.cellValuePropertyPageModelVersion,
            ALPSConstants.cellValuePropertyPriorityOrderNumber
        };

        /// <summary>
        /// Called by Visio when a Page is added to the current document
        /// </summary>
        /// <param name="page">added Page</param>
        internal void pageAdded(Page page)
        {
            // Check if page is a fully functional SID page (all cells created correctly)
            if (isSid(page))
            {
                registerNewSidPage(page);
                if (!possibleSidOrSbdPages.ContainsKey(page.ID)) return;
                possibleSidOrSbdPages.Remove(page.ID);
                page.CellChanged -= onCellChangedOnPossibleSidOrSbdPage;
            }
            // Check if page is a fully functional SBD page (all cells created correctly)
            else if (isSbd(page))
            {
                registerNewSbdPage(page);
                if (!possibleSidOrSbdPages.ContainsKey(page.ID)) return;
                possibleSidOrSbdPages.Remove(page.ID);
                page.CellChanged -= onCellChangedOnPossibleSidOrSbdPage;
            }
            else
            {
                // Page might still be in creation process
                // Save the page for later and register a CellChanged listener
                if (possibleSidOrSbdPages.ContainsKey(page.ID)) return;
                possibleSidOrSbdPages.Add(page.ID, page);
                page.CellChanged += onCellChangedOnPossibleSidOrSbdPage;
            }
        }

        /// <summary>
        /// Checks if a visio page is a SID page, meaning all the relevant cells exist in the PageSheet
        /// and the type cell contains a value stating that it is a SID page
        /// </summary>
        /// <returns></returns>
        private static bool isSid(Page page)
        {
            // If one of the specified cells does not exist, return false
            if (sidPageCompleteIfCellsExists.Any(cell => page.PageSheet.CellExistsU[cell, 1] == 0))
            {
                return false;
            }
            string pageType = page.PageSheet.CellsU[ALPSConstants.cellValuePropertyPageType].Formula;
            
            return pageType.Contains("SubjectInteraction");
        }

        /// <summary>
        /// Checks if a visio page is a SBD page, meaning all the relevant cells exist in the PageSheet
        /// and the type cell contains a value stating that it is a SBD page
        /// </summary>
        /// <returns></returns>
        private static bool isSbd(Page page)
        {
            return page.PageSheet.CellExistsU[ALPSConstants.cellValuePropertySBDLinkedSubjectID, 1] != 0;
        }

        /// <summary>
        /// Pages that might not be fully initialized and are therefor currently no valid SID or SBD pages
        /// trigger this method on cell change.
        /// This method then checks if the intitialization of the page is complete
        /// </summary>
        /// <param name="cell"></param>
        private void onCellChangedOnPossibleSidOrSbdPage(Cell cell)
        {
            if (possibleSidOrSbdPages.ContainsKey(cell.ContainingPageID))
            {
                pageAdded(possibleSidOrSbdPages[cell.ContainingPageID]);
            }

        }

        /// <summary>
        /// If the page is a SID page, this method fetches the IVisioProcessModel the page belongs to and adds the page to the model.
        /// </summary>
        /// <param name="page">Page to be checked</param>
        private void registerNewSidPage(Page page)
        {
            // Get the URI of the model that is defined in the pagesheet of the page
            Cell cell = page.PageSheet.CellsU[ALPSConstants.cellValuePropertyPageModelURI];
            // Fetch a fitting model (if none exists, a new one is created by the called method)
            IVisioProcessModel model = getOrCreateModel(cell.Formula);

            if (model.containsSidPage(page.NameU)) return;

            // Create new SID page controller and generate SIDPage wrapper for the visio page object
            SIDPageController pageController = SIDPageController.getController(addIn, this, cell.Formula, page);
            SIDPage sidPageWrapper = pageController.getSidPage();

            // Add the wrapepr to the model
            model.addSidPage(sidPageWrapper);
            modelToSidController[model].Add(pageController);
            sidPageToSbdController.Add(sidPageWrapper, new HashSet<SBDPageController>());
        }

        /// <summary>
        /// updateWholeController SBD pages.
        /// </summary>
        /// <param name="sbdPage">Page to be checked if it's a sbd Page.</param>
        private void registerNewSbdPage(Page sbdPage)
        {
            string pageLayer = sbdPage.PageSheet.CellsU[ALPSConstants.cellValuePropertyPageLayer].Formula;
            if (string.IsNullOrWhiteSpace(pageLayer)) return;

            foreach (IVisioProcessModel model in models)
            {
                foreach (SIDPage sidPage in model.getSidPages())
                {
                    if (!sidPage.getLayer().Equals(pageLayer)) continue;

                    // Get SBD pages of SID page and check if SBD page is already in there.
                    SIDPageController sidController = getSidPageController(sidPage);
                    if (sidController.addSbdPageAndCreateNewController(sbdPage, out SBDPageController sbdController))
                    {
                        sidPageToSbdController[sidPage].Add(sbdController);
                    }
                }
            }
        }


        /// <summary>
        /// called when the user modified the extends property manually.
        /// has to check if userInput is * nullOrWhiteSpace * is an existing sid page
        /// </summary>
        /// <param name="modifiedC"></param>
        /// <param name="userInput"></param>
        public void updateExtends(SIDPageController modifiedC, SIDPage modifiedP, string userInput)
        {
            SIDPage extending = getSidPage(userInput);

            SIDPage oldExtends = modifiedC.getExtends();
            if (extending != null || string.IsNullOrWhiteSpace(userInput))
            {
                SIDPageController extendingC = getSidPageController(extending);

                // Set only if extendingC is not null
                extendingC?.setExtended(modifiedP);

                modifiedC.setExtends(extending);

                //tell the (evtl) old page that it is not a bg anymore.
                if (oldExtends == null) return;
                SIDPageController oldExtendsC = getSidPageController(oldExtends);
                if (extendingC == null || !oldExtendsC.getNameU().Equals(extendingC.getNameU()))
                {
                    oldExtendsC.setNotExtended();
                }
            }
            else
            {
                //page does not exist. show userinputNotFound.
                MessageBox.Show(string.Format(ALPSConstants.InputNotFound, userInput, modifiedC.getNameU()), "Error", MessageBoxButton.OK);
                //UserInputNotFound notFound = UserInputNotFound.GetInstance(this, userInput, modifiedC.getNameU());
                //notFound.Show();
            }
        }

        /// <summary>
        /// searches after a sidPage specified by its layerName
        /// </summary>
        /// <param name="layerName">name of sidPage</param>
        /// <returns>sidPage if found, null otherwise</returns>
        public SIDPage getSidPage(string layerName)
        {
            return models.SelectMany(model => model.getSidPages()).FirstOrDefault(sidPage => sidPage.getLayerForUser().Equals(layerName));
        }


        /// <summary>
        /// searches for the controller of a specified sidPage.
        /// </summary>
        /// <param name="searched"></param>
        /// <returns></returns>
        public SIDPageController getSidPageController(DiagramPage searched)
        {
            if (searched == null) return null;
            return modelToSidController.Keys.SelectMany(model => modelToSidController[model]).FirstOrDefault
                (sidPageController => sidPageController.getSidPage().getNameU().Equals(searched.getNameU()));
        }

        /// <summary>
        /// a shape on the background page of the given page has moved. 
        /// Let's check in the controller of the foreground page,
        /// if there is a snapped shape that should move now.
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="foreground"></param>
        public void backgroundShapeMoved(Shape shape, SIDPage foreground)
        {
            SIDPageController foregroundC = getSidPageController(foreground);
            foregroundC.backgroundShapeMoved(shape);
        }

        /// <summary>
        /// a shape on the background page of the given page has moved.
        /// Let's check in the controller of the foreground page,
        /// if there is a snapped shape that should move now.
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="foreground"></param>
        public void backgroundShapeMoved(Shape shape, SBDPage foreground)
        {
            SBDPageController foregroundC = getSbdPageController(foreground);
            foregroundC?.backgroundShapeMoved(shape);
        }

        /// <summary>
        /// searches after a sbdPage specified by its layerName
        /// </summary>
        /// <param name="nameU">name of sbdPage</param>
        /// <returns>sbdPage if found, null otherwise</returns>
        public SBDPage getSbdPage(string nameU)
        {
            return (from model in models from sidPage in model.getSidPages() from sbdPage in sidPage.getSbdPages() select sbdPage)
                .FirstOrDefault(sbdPage => sbdPage.getNameU().Equals(nameU));
        }

        public SBDPageController getSbdPageController(DiagramPage toFind)
        {
            if (toFind == null) return null;
            return sidPageToSbdController.Keys.SelectMany(sidPage => sidPageToSbdController[sidPage])
                .FirstOrDefault(sbdPageC => sbdPageC.getNameU().Equals(toFind.getNameU()));
        }

        /// <summary>
        /// lists all SIDPages that can be extended by the one given
        /// </summary>
        /// <param name="givenPage">the given SIDPage</param>
        /// <returns>List of extendable SIDPages</returns>
        public IList<SIDPage> getExtendableSidPages(SIDPage givenPage)
        {
            string givenLayer = givenPage.getLayer();

            return (from model in models from sidPage in model.getSidPages() where !sidPage.getLayer().Equals(givenLayer) select sidPage).ToList();
        }

        /// <summary>
        /// Sets the sid page as active page in the current document
        /// Used by the LayerEditor
        /// </summary>
        /// <param name="sidPage"></param>
        public void setActivePage(SIDPage sidPage)
        {
            SIDPageController sidPageC = getSidPageController(sidPage);
            if (sidPageC != null) addIn.Application.ActiveWindow.Page = sidPageC.getPage();

        }

        /// <summary>
        /// Sets the sbd page as active page in the current document
        /// Used by the LayerEditor
        /// </summary>
        /// <param name="sbdPage"></param>
        public void setActivePage(SBDPage sbdPage)
        {
            SBDPageController sbdPageC = getSbdPageController(sbdPage);
            if (sbdPageC != null) addIn.Application.ActiveWindow.Page = sbdPageC.getPage();

        }

        /// <summary>
        /// Change the layer name of a sid page to a new name.
        /// Used by the LayerEditor
        /// </summary>
        /// <param name="changed"></param>
        /// <param name="newName"></param>
        public void changeLayerName(SIDPage changed, string newName)
        {
            SIDPageController pageController = getSidPageController(changed);
            pageController.setLayerName(newName);
            if (changed.getForeground() != null)
            {
                SIDPageController foregroundController = getSidPageController(changed.getForeground());
                foregroundController.setExtendsCell(changed.getLayer());
            }
            addIn.refreshLayerExplorerTreeView();
        }

        internal void changeModelForSidPage(SIDPage changed, IVisioProcessModel newModel, IVisioProcessModel oldModel)
        {
            oldModel.removePage(changed);
            newModel.addSidPage(changed);
            //let the page itself know about it
            SIDPageController changedC = getSidPageController(changed);
            changedC?.setModelUri(newModel.getModelUri());
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
            {
                newModel.addSidPage(sidPage);
            }
            modelToSidController[newModel].Add(pageController);

            addIn.refreshLayerExplorerTreeView();
        }


        /// <summary>
        /// resets the data and builds the data structure completely new.
        /// </summary>
        /// <param name="pages"></param>
        internal void updateWholeController(Pages pages)
        {
            models = new HashSet<IVisioProcessModel>();

            this.modelToSidController = new Dictionary<IVisioProcessModel, ISet<SIDPageController>>();
            this.sidPageToSbdController = new Dictionary<SIDPage, ISet<SBDPageController>>();

            foreach (var page in pages.Cast<Page>().Where(isSid))
            {
                registerNewSidPage(page);
            }
            foreach (var page in pages.Cast<Page>().Where(isSbd))
            {
                registerNewSbdPage(page);
            }
            foreach (var sidPageC in modelToSidController.SelectMany(pair => pair.Value))
            {
                sidPageC.updateExtends();
            }
        }





        public int getCurrentPriority(string modelUri)
        {
            return getOrCreateModel(modelUri).getCurrentPriority();
        }

        /// <summary>
        /// Searches after model by its modelURI.
        /// If no such model exists, a new one is created.
        /// </summary>
        /// <param name="modelUri">The model URI of the IVisioProcessModel</param>
        /// <returns></returns>
        private IVisioProcessModel getOrCreateModel(string modelUri)
        {
            IVisioProcessModel fittingModel = models.FirstOrDefault(model => modelUri.Equals(model.getModelUri()));

            if (fittingModel != null) return fittingModel;

            // Create new model if no model exists
            fittingModel = new VisioProcessModel(modelUri);
            models.Add(fittingModel);
            modelToSidController.Add(fittingModel, new HashSet<SIDPageController>());
            return fittingModel;
        }

        /// <summary>
        /// updates the priority of the Page to the new property given.
        /// </summary>
        /// <param name="newProperty">new priority of the Page</param>
        /// <param name="changed">Page whose priority should be changed</param>
        internal void updatePagePriority(string newProperty, SIDPageController changed)
        {
            changed.setPriorityOrder(newProperty);
        }

        public void updatePagePriority(int newProperty, SIDPage changed)
        {
            SIDPageController changedC = getSidPageController(changed);
            changedC.setPriorityOrder(newProperty);
        }

        /// <summary>
        /// updates the background property of pages.
        /// If newProperty is the same as changedPage it means that changedPage is extending nothing at the moment. 
        /// </summary>
        /// <param name="newProperty">the new Page that is extended</param>
        /// <param name="changedPage">the extending Page which properties were changed</param>
        internal void updateBackground(SIDPage newProperty, SIDPage changedPage)
        {
            SIDPageController newPropC = getSidPageController(newProperty);
            SIDPageController changedC = getSidPageController(changedPage);

            if (!newProperty.getLayer().Equals(changedPage.getLayer()))
            {
                SIDPage oldExtends = changedC.getExtends();
                if ((oldExtends == null
                     || oldExtends.getLayer().Equals(newProperty.getLayer()))
                    && oldExtends != null) return;
                newPropC.setExtended(changedPage);
                changedC.setExtends(newProperty);
                if (oldExtends == null) return;
                SIDPageController oldExtended = getSidPageController(oldExtends);
                oldExtended.setNotExtended();
            }
            else
            {
                //unextend.
                SIDPage oldExtends = changedC.getExtends();

                changedC.setExtends(null);

                if (oldExtends == null) return;
                SIDPageController oldExtended = getSidPageController(oldExtends);
                oldExtended.setNotExtended();
            }
        }



        /// <summary>
        /// creates the right structure for presenting the data in a tree view
        /// </summary>
        /// <returns>a dictionary from model names to a Page dictionary.</returns>
        internal IDictionary<IVisioProcessModel, IDictionary<SIDPage, IList<SBDPage>>> getTreeView()
        {
            IDictionary<IVisioProcessModel, IDictionary<SIDPage, IList<SBDPage>>> treeView =
                new Dictionary<IVisioProcessModel, IDictionary<SIDPage, IList<SBDPage>>>();
            foreach (IVisioProcessModel model in models)
            {
                treeView.Add(model, model.getTreeView());
            }
            return treeView;
        }


    }
}
