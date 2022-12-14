using Microsoft.Office.Interop.Visio;
using System;
using System.Collections.Generic;
using System.Linq;
using VisioAddIn;


namespace VisioAddIn.Snapping
{
    public class SIDPageController : DiagramPageController
    {
        /// <summary>
        /// reference to main class for different calls
        /// </summary>
        private readonly ThisAddIn addIn;

        private static readonly IList<SIDPageController> controllers = new List<SIDPageController>();

        private ModelController modelController;
        private string modelURri;

        /// <summary>
        /// To save the current x-coordinate of the moved shape
        /// </summary>
        private string xCoordinate = "";

        /// <summary>
        /// Controlled controlledSidPage
        /// </summary>
        private SIDPage controlledSidPage;

        private SidSnapHandler snapHandler;

        private SIDPageController(ThisAddIn addIn, ModelController modelController, string modelUri, Page page) : base(page)
        {
            this.addIn = addIn;
            modelURri = modelUri;

            refresh(modelController);
        }

        private void refresh(ModelController controller)
        {
            this.modelController = controller;

            createSidPage();

            snapHandler = new SidSnapHandler(controller, controlledSidPage);

            visioPage.CellChanged += onCellChanged;
        }


        /// <summary>
        /// Factory method to obtain a controller for a SID page.
        /// Only creates a new controller if no controller exists for the requested page.
        /// </summary>
        /// <param name="addIn">The instance of the current addIn</param>
        /// <param name="modelController">the model controller</param>
        /// <param name="modelUri">the model uri the page belongs to</param>
        /// <param name="page">the visio page itself</param>
        /// <returns></returns>
        public static SIDPageController getController(ThisAddIn addIn, ModelController modelController, string modelUri, Page page)
        {
            // Check if a controller for the page exists
            foreach (SIDPageController controller in controllers)
            {
                if (!controller.modelURri.Equals(modelUri) || !controller.visioPage.Equals(page)) continue;
                // TODO why refresh here?
                controller.refresh(modelController);
                return controller;
            }

            // If not, create a new one
            SIDPageController newController = new SIDPageController(addIn, modelController, modelUri, page);
            controllers.Add(newController);
            return newController;
        }

        /// <summary>
        /// creates the actual sid page in the model for the visio page.
        /// </summary>
        private void createSidPage()
        {
            string layer = visioPage.PageSheet.CellsU[ALPSConstants.cellValuePropertyPageLayer].Formula;
            string nameU = visioPage.NameU;

            int priority = readOutPriority();
            if (priority == -1)
            {
                priority = modelController.getCurrentPriority(modelURri);
                Cell cell = visioPage.PageSheet.CellsU[ALPSConstants.cellValuePropertyPriorityOrderNumber];
                cell.Formula = priority.ToString();
            }

            controlledSidPage = new SIDPage(layer, nameU, modelURri, priority);
        }


        /// <summary>
        /// event listener for the cell changed event; active as soon as the page has a background
        /// every time a shape is moved, event is fired 2 times: for x and y axis.
        /// shape accessible via cell.Shape
        /// </summary>
        /// <param name="cell">changed cell</param>
        private void onCellChanged(Cell cell)
        {
            SIDPage extends = controlledSidPage.getExtends();
            switch (cell.Name)
            {
                // If the link to the extended subject was changed
                case ALPSConstants.cellHyperlinkCategoryPrefix +
                     ALPSConstants.alpsHyperlinksExtendedSubject:
                {
                        if (extends == null) break;
                        var parts = cell.Formula.Split('/');
                        string subjectName = parts.Length > 1 ? parts[1] : parts[0];
                        snapHandler.snap(cell.Shape, subjectName);
                    break;
                }
                // If the extends cell was changed (by user or shapes)
                case ALPSConstants.cellPropertyCategoryPrefix + ALPSConstants.alpsPropertieTypeExtends:
                    uUpdateExtends(visioPage.PageSheet.CellsU[ALPSConstants.cellValuePropertyExtends].Formula);
                    break;
                case ALPSConstants.shapeCellShapeTransformPinX:
                {
                    // x and y always get updated, even if the value did not change.
                    // Only call method once -> remember initial x value and do not call if the value stays the same.

                    string newXCoordinate = cell.Formula.Replace("\"", "");
                    if (!newXCoordinate.Equals(xCoordinate))
                    {
                        xCoordinate = newXCoordinate;
                        if (extends != null)
                        {
                            snapHandler.checkForSnapping(cell.Shape);
                        }
                        //not else, bc a page could be in the middle (is extending and is extended)
                        if (controlledSidPage.getForeground() != null)
                        {
                            //if the page is a background page, listen if subjects are moved
                            //(and snapped extensions should move, too)
                            modelController.backgroundShapeMoved(cell.Shape, controlledSidPage.getForeground());
                        }
                    }

                    break;
                }
                case ALPSConstants.shapeCellShapeTransformPinY:
                    xCoordinate = "";
                    break;
                case ALPSConstants.cellPropertyCategoryPrefix + ALPSConstants.alpsPropertieTypePageModelURI:
                {
                    string newModelURI = visioPage.PageSheet.CellsU[ALPSConstants.cellValuePropertyPageModelURI].Formula;
                    string trimmed = newModelURI.Trim('\\', '"');
                    if (string.IsNullOrWhiteSpace(trimmed))
                    {
                        setModelUri(modelURri);
                    }
                    else
                    {
                        modelController.moveSidPageToNewModel(this, newModelURI);
                    }

                    break;
                }
                case ALPSConstants.cellPropertyCategoryPrefix + ALPSConstants.alpsPropertieTypePriorityOrderNumber:
                    setPriorityOrder(visioPage.PageSheet.CellsU[ALPSConstants.cellValuePropertyPriorityOrderNumber].Formula);
                    addIn.refreshLayerExplorerTreeView();
                    break;
                case ALPSConstants.pageCellPagePropertiesPageWidth:
                case ALPSConstants.pageCellPagePropertiesPageHeight:
                {
                    if (extends != null)
                    {
                        // TODO macht es hier überhaupt sinn die referenz auf die sid page zu übergeben?
                        rearrangeBackRectangle(this.getSidPage());
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// sets only the extends cell in PageSheet.
        /// </summary>
        /// <param name="newProperty"></param>
        public void setExtendsCell(string newProperty)
        {
            visioPage.PageSheet.CellsU[ALPSConstants.cellValuePropertyExtends].Formula = newProperty;
        }

        public void setLayerName(string newName)
        {
            newName = "\"" + newName + "\"";

            visioPage.PageSheet.CellsU[ALPSConstants.cellValuePropertyPageLayer].Formula = newName;
            controlledSidPage.setLayer(newName);
        }

        public SIDPage getSidPage()
        {
            return controlledSidPage;
        }

        public void sbdBackgroundShapeMoved(Shape shape, SBDPage foreground)
        {
            modelController.backgroundShapeMoved(shape, foreground);
        }

        /// <summary>
        /// gets called from model controller
        /// when a shape in the background of this page has moved.
        /// </summary>
        /// <param name="shape"></param>
        public void backgroundShapeMoved(Shape shape)
        {
            snapHandler.notifyBackgroundShapeMoved(shape);
        }

        /// <summary>
        /// is called when the page is extended
        /// </summary>
        /// <param name="extended">new extending page (foreground)</param>
        public void setExtended(SIDPage extended)
        {
            visioPage.Background = -1;
            controlledSidPage.setForeground(extended);
        }

        /// <summary>
        /// sets the extends property to the given page, 
        /// places the rectangle-layer for visualization purposes
        /// and resets the snap handler.
        /// </summary>
        /// <param name="extendedPage">page that should be extended</param>
        internal void setExtends(SIDPage extendedPage)
        {
            setBackgroundForThis("");

            // Something is extended -> update references and background
            if (extendedPage != null)
            {
                visioPage.PageSheet.CellsU[ALPSConstants.cellValuePropertyExtends].Formula = extendedPage.getLayer();
                setBackgroundForThis(extendedPage.getNameU());
                if (!backLayerExists()) placeBackRectangle(extendedPage);
            }

            // Nothing is extended -> Remove reference to previous extends and clear background
            else
            {
                visioPage.PageSheet.CellsU[ALPSConstants.cellValuePropertyExtends].Formula = "";
                deleteBackRectangle();
            }

            snapHandler.setBackgroundPage(extendedPage);
            controlledSidPage.setExtends(extendedPage);

            foreach (Shape shape in visioPage.Shapes)
            {
                // either CheckForSnapping or Snap
                // Find a shape that extends another subject shape
                if (shape.CellExistsU[ALPSConstants.cellSubAdressHyperlinkExtendedSubject, 0] == 0) continue;
                string formula = shape.CellsU[ALPSConstants.cellSubAdressHyperlinkExtendedSubject].Formula;
                string subjectName = (formula.Contains('/')) ? formula.Split('/')[1] : formula;
                snapHandler.snap(shape, subjectName);
            }
        }

        public override DiagramPageController getController(DiagramPage background)
        {
            return modelController.getSidPageController(background);
        }

        /// <summary>
        /// updateWholeController extends property by content in shapeSheet
        /// </summary>
        public void updateExtends()
        {
            string userInput = visioPage.PageSheet.CellsU[ALPSConstants.cellValuePropertyExtends].Formula;
            uUpdateExtends(userInput);
        }

        /// <summary>
        /// updateWholeController extends property by name of extending page.
        /// Just assuring that the input is not null and that the input is not the
        /// same as the current extends-Property
        /// </summary>
        /// <param name="userInput">new extends-Property</param>
        private void uUpdateExtends(string userInput)
        {
            userInput = userInput.Trim('\\', '"');
            SIDPage extending = controlledSidPage.getExtends();
            bool isNull = string.IsNullOrWhiteSpace(userInput);
            if ((!isNull && extending != null && !extending.getLayerForUser().Equals(userInput))
                || !isNull && extending == null
                || isNull && extending != null)
            {
                modelController.updateExtends(this, controlledSidPage, userInput);
            }
        }

        /// <summary>
        /// Called from controller on notification of a new sbd page.
        /// Checks if the page is already registered to this controlledSidPage.
        /// If the page is not registered, a new controller is created and passed as output.
        /// </summary>
        /// <param name="page">Visio SBDPage</param>
        /// <param name="controller">The freshly created controller. Null if the sbd page was registered before</param>
        /// <returns>True if the sbd page was not registered before and a new controller was created,
        /// false if the page was registered before and no new controller was created</returns>
        public bool addSbdPageAndCreateNewController(Page page, out SBDPageController controller)
        {
            controller = null;
            if (containsSbdPage(page)) return false;
            SBDPageController sbdPageC = new SBDPageController(modelController, this, page);
            controlledSidPage.addSbdPage(sbdPageC.getSbdPage());
            controller = sbdPageC;
            return true;
        }

        private bool containsSbdPage(IVPage page)
        {
            return controlledSidPage.getSbdPages().Any(sbdPage => sbdPage.getNameU().Equals(page.NameU));
        }

        /// <summary>
        /// Searches after a specified shape by nameU.
        /// </summary>
        /// <param name="shapeNameU">nameU of searched shape</param>
        /// <returns>shape if found, null otherwise</returns>
        internal Shape getShape(string shapeNameU)
        {
            return visioPage.Shapes.Cast<Shape>().FirstOrDefault(shape => shape.NameU.Equals(shapeNameU));
        }

        /// <summary>
        /// updates priority from ShapeSheet
        /// </summary>
        /// <returns>new priority</returns>
        private int readOutPriority()
        {
            Cell cell = visioPage.PageSheet.CellsU[ALPSConstants.cellValuePropertyPriorityOrderNumber];
            string priority = cell.Formula.Trim(new Char[] { '"' });
            try
            {
                int prio = Convert.ToInt32(priority);
                return prio;
            }
            catch (FormatException e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
                return -1;
            }
        }

        /// <summary>
        /// reads out priority in shapeSheet
        /// </summary>
        internal void updatePriority()
        {
            Cell cell = visioPage.PageSheet.CellsU[ALPSConstants.cellValuePropertyPriorityOrderNumber];
            string priority = cell.Formula.Trim('"');
            try
            {
                int prio = Convert.ToInt32(priority);
                controlledSidPage.setPriorityOrder(prio);
            }
            catch (FormatException e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
        }

        internal void setPriorityOrder(int newPriority)
        {
            controlledSidPage.setPriorityOrder(newPriority);
            Cell cell = visioPage.PageSheet.CellsU[ALPSConstants.cellValuePropertyPriorityOrderNumber];
            cell.Formula = newPriority.ToString();
        }

        internal void setPriorityOrder(string newPriority)
        {
            newPriority = newPriority.Trim('\\', '"');
            try
            {
                int prio = Convert.ToInt32(newPriority);
                controlledSidPage.setPriorityOrder(prio);
                Cell cell = visioPage.PageSheet.CellsU[ALPSConstants.cellValuePropertyPriorityOrderNumber];
                cell.Formula = newPriority.ToString();
            }
            catch (FormatException e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// changes the shapeSheet property "modelUri" to the new given string
        /// </summary>
        /// <param name="newModelURI">name of the new model</param>
        internal void setModelUri(string newModelURI)
        {
            visioPage.PageSheet.CellsU[ALPSConstants.cellValuePropertyPageModelURI].Formula = newModelURI;
            controlledSidPage.setModelUri(newModelURI);
            this.modelURri = newModelURI;

            snapHandler.setModelUri(newModelURI);
        }

        /// <summary>
        /// has to be called if the sid page is not extending anything anymore.
        /// </summary>
        public void setNotExtended()
        {
            visioPage.Background = 0;
            controlledSidPage.setForeground(null);
        }

        public SIDPage getExtends()
        {
            return controlledSidPage.getExtends();
        }

        internal string getNameU()
        {
            return visioPage.NameU;
        }

        internal void setBackground(short background)
        {
            this.visioPage.Background = background;
        }

        internal short getBackground()
        {
            return this.visioPage.Background;
        }

        internal Page getPage()
        {
            return visioPage;
        }

        public string getModelUri()
        {
            return modelURri;
        }

        public SidSnapHandler getSidSnapHandler()
        {
            return snapHandler;
        }
    }
}
