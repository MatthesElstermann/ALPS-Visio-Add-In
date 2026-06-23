using Microsoft.Office.Interop.Visio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ALPS_Visio_AddIn_rewrite
{
    public class SIDPageController : DiagramPageController
    {
        private readonly ThisAddIn addIn;

        private static readonly IList<SIDPageController> controllers = new List<SIDPageController>();

        private ModelController modelController;
        private string modelURri;

        private string xCoordinate = "";

        private SIDPage controlledSidPage;

        private SidSnapHandler snapHandler;

        private SIDPageController(ThisAddIn addIn, ModelController modelController, string modelUri, Page page) : base(page)
        {
            Debug.Print("Creating SIDPageController for " + page.NameU);
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

        public static SIDPageController getController(ThisAddIn addIn, ModelController modelController, string modelUri, Page page)
        {
            foreach (SIDPageController controller in controllers)
            {
                if (!controller.modelURri.Equals(modelUri) || !controller.visioPage.Equals(page)) continue;
                controller.refresh(modelController);
                return controller;
            }

            SIDPageController newController = new SIDPageController(addIn, modelController, modelUri, page);
            controllers.Add(newController);
            return newController;
        }

        private void createSidPage()
        {
            string layer = visioPage.PageSheet.CellsU["Prop." + Constants.Properties.PageLayer].Formula;
            string nameU = visioPage.NameU;

            int priority = readOutPriority();
            if (priority == -1)
            {
                priority = modelController.getCurrentPriority(modelURri);
                visioPage.PageSheet.CellsU["Prop." + Constants.Properties.PriorityOrderNumber].Formula = priority.ToString();
            }

            controlledSidPage = new SIDPage(layer, nameU, modelURri, priority);
        }

        private void onCellChanged(Cell cell)
        {
            SIDPage extends = controlledSidPage.getExtends();
            switch (cell.Name)
            {
                case "Hyperlink." + Constants.Properties.ExtendedSubject:
                {
                    if (extends == null) break;
                    var parts = cell.Formula.Split('/');
                    string subjectName = parts.Length > 1 ? parts[1] : parts[0];
                    snapHandler.snap(cell.Shape, subjectName);
                    break;
                }
                case "Prop." + Constants.Properties.Transition.Extends:
                    uUpdateExtends(visioPage.PageSheet.CellsU["Prop." + Constants.Properties.Transition.Extends].Formula);
                    break;
                case "PinX":
                {
                    string newXCoordinate = cell.Formula.Replace("\"", "");
                    if (!newXCoordinate.Equals(xCoordinate))
                    {
                        xCoordinate = newXCoordinate;
                        if (extends != null)
                            snapHandler.checkForSnapping(cell.Shape);
                        if (controlledSidPage.getForeground() != null)
                            modelController.backgroundShapeMoved(cell.Shape, controlledSidPage.getForeground());
                    }
                    break;
                }
                case "PinY":
                    xCoordinate = "";
                    break;
                case "Prop." + Constants.Properties.PageModelURI:
                {
                    string newModelURI = visioPage.PageSheet.CellsU["Prop." + Constants.Properties.PageModelURI].Formula;
                    string trimmed = newModelURI.Trim('\\', '"');
                    if (string.IsNullOrWhiteSpace(trimmed))
                        setModelUri(modelURri);
                    else
                        modelController.moveSidPageToNewModel(this, newModelURI);
                    break;
                }
                case "Prop." + Constants.Properties.PriorityOrderNumber:
                    setPriorityOrder(visioPage.PageSheet.CellsU["Prop." + Constants.Properties.PriorityOrderNumber].Formula);
                    addIn.refreshLayerExplorerTreeView();
                    break;
                case "PageWidth":
                case "PageHeight":
                {
                    if (extends != null)
                        rearrangeBackRectangle(this.getSidPage());
                    break;
                }
            }
        }

        public void setExtendsCell(string newProperty)
        {
            visioPage.PageSheet.CellsU["Prop." + Constants.Properties.Transition.Extends].Formula = newProperty;
        }

        public void setLayerName(string newName)
        {
            newName = "\"" + newName + "\"";
            visioPage.PageSheet.CellsU["Prop." + Constants.Properties.PageLayer].Formula = newName;
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

        public void backgroundShapeMoved(Shape shape)
        {
            snapHandler.notifyBackgroundShapeMoved(shape);
        }

        public void setExtended(SIDPage extended)
        {
            visioPage.Background = -1;
            controlledSidPage.setForeground(extended);
        }

        internal void setExtends(SIDPage extendedPage)
        {
            setBackgroundForThis("");

            if (extendedPage != null)
            {
                visioPage.PageSheet.CellsU["Prop." + Constants.Properties.Transition.Extends].Formula = extendedPage.getLayer();
                setBackgroundForThis(extendedPage.getNameU());
                if (!backLayerExists()) placeBackRectangle(extendedPage);
            }
            else
            {
                visioPage.PageSheet.CellsU["Prop." + Constants.Properties.Transition.Extends].Formula = "";
                deleteBackRectangle();
            }

            snapHandler.setBackgroundPage(extendedPage);
            controlledSidPage.setExtends(extendedPage);

            foreach (Shape shape in visioPage.Shapes)
            {
                if (shape.CellExistsU["Hyperlink." + Constants.Properties.ExtendedSubject + ".SubAdress", 0] == 0) continue;
                string formula = shape.CellsU["Hyperlink." + Constants.Properties.ExtendedSubject + ".SubAdress"].Formula;
                string subjectName = (formula.Contains('/')) ? formula.Split('/')[1] : formula;
                snapHandler.snap(shape, subjectName);
            }
        }

        public override DiagramPageController getController(DiagramPage background)
        {
            return modelController.getSidPageController(background);
        }

        public void updateExtends()
        {
            string userInput = visioPage.PageSheet.CellsU["Prop." + Constants.Properties.Transition.Extends].Formula;
            uUpdateExtends(userInput);
        }

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

        internal Shape getShape(string shapeNameU)
        {
            return visioPage.Shapes.Cast<Shape>().FirstOrDefault(shape => shape.NameU.Equals(shapeNameU));
        }

        private int readOutPriority()
        {
            Cell cell = visioPage.PageSheet.CellsU["Prop." + Constants.Properties.PriorityOrderNumber];
            string priority = cell.Formula.Trim(new Char[] { '"' });
            try { return Convert.ToInt32(priority); }
            catch (FormatException e) { System.Diagnostics.Debug.WriteLine(e.ToString()); return -1; }
        }

        internal void updatePriority()
        {
            Cell cell = visioPage.PageSheet.CellsU["Prop." + Constants.Properties.PriorityOrderNumber];
            string priority = cell.Formula.Trim('"');
            try { controlledSidPage.setPriorityOrder(Convert.ToInt32(priority)); }
            catch (FormatException e) { System.Diagnostics.Debug.WriteLine(e.ToString()); }
        }

        internal void setPriorityOrder(int newPriority)
        {
            controlledSidPage.setPriorityOrder(newPriority);
            visioPage.PageSheet.CellsU["Prop." + Constants.Properties.PriorityOrderNumber].Formula = newPriority.ToString();
        }

        internal void setPriorityOrder(string newPriority)
        {
            newPriority = newPriority.Trim('\\', '"');
            try
            {
                int prio = Convert.ToInt32(newPriority);
                controlledSidPage.setPriorityOrder(prio);
                visioPage.PageSheet.CellsU["Prop." + Constants.Properties.PriorityOrderNumber].Formula = newPriority.ToString();
            }
            catch (FormatException e) { System.Diagnostics.Debug.WriteLine(e.ToString()); }
        }

        internal void setModelUri(string newModelURI)
        {
            visioPage.PageSheet.CellsU["Prop." + Constants.Properties.PageModelURI].Formula = newModelURI;
            controlledSidPage.setModelUri(newModelURI);
            this.modelURri = newModelURI;
            snapHandler.setModelUri(newModelURI);
        }

        public void setNotExtended()
        {
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
