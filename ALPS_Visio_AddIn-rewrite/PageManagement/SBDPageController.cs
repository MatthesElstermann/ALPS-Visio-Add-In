using Microsoft.Office.Interop.Visio;
using System.Diagnostics;

namespace ALPS_Visio_AddIn_rewrite
{
    public class SBDPageController : DiagramPageController
    {
        private readonly ModelController modelController;
        private readonly SIDPageController sidController;
        private readonly SbdSnapHandler snapHandler;

        private SBDPage sbdPage;

        public SBDPageController(ModelController modelController, SIDPageController sidController, Page page) : base(page)
        {
            Debug.Print("Creating SBDPageController for: " + page.NameU);
            this.modelController = modelController;
            this.sidController = sidController;

            createSbdPage();

            snapHandler = new SbdSnapHandler(sbdPage, this.modelController);

            visioPage.CellChanged += onCellChanged;
            visioPage.ShapeAdded += shapeAdded;
        }

        public Page getPage()
        {
            return visioPage;
        }

        private void createSbdPage()
        {
            string pageLayer = visioPage.PageSheet.CellsU["Prop." + Constants.Properties.PageLayer].Formula;
            sbdPage = new SBDPage(pageLayer, visioPage.NameU, sidController.getModelUri());
        }

        private void shapeAdded(Shape shape)
        {
            if (sbdPage.getExtends() != null)
            {
                snapHandler.checkForSnapping(shape);
            }
        }

        /// <summary>
        /// event listener for the cell changed event; active as soon as the page has a background
        /// every time a shape is moved, event is fired 2 times: for x and y axis.
        /// shape accessible via cell.Shape
        /// </summary>
        private void onCellChanged(Cell cell)
        {
            SBDPage extends = sbdPage.getExtends();
            if (cell.Name == "Prop." + Constants.Properties.Transition.Extends + ".Value")
            {
                if (extends != null)
                {
                    snapHandler.snap(cell.Shape, cell.Formula);
                }
            }
            else if (cell.Name == "PinX" || cell.Name == "PinY")
            {
                if (extends != null)
                {
                    snapHandler.checkForSnapping(cell.Shape);
                }
                if (sbdPage.getForeground() != null)
                {
                    sidController.sbdBackgroundShapeMoved(cell.Shape, sbdPage.getForeground());
                }
            }
            else if (cell.Name == "PageWidth" || cell.Name == "PageHeight")
            {
                if (extends != null)
                {
                    rearrangeBackRectangle(this.getSbdPage());
                }
            }
        }

        public SBDPage getSbdPage()
        {
            return sbdPage;
        }

        public void backgroundShapeMoved(Shape shape)
        {
            snapHandler.notifyBackgroundShapeMoved(shape);
        }

        public string getNameU()
        {
            if ((visioPage == null) || (visioPage.ID < 0))
                return "";
            return visioPage.NameU;
        }

        /// <summary>
        /// has to be called if the sid page is not extending anything anymore.
        /// </summary>
        public void setNotExtended()
        {
            sbdPage.setForeground(null);
        }

        public void setExtended(SBDPage extended)
        {
            visioPage.Background = -1;
            sbdPage.setForeground(extended);
        }

        /// <summary>
        /// sets the extends property of the page,
        /// places the rectangle-layer for visualization purposes
        /// and resets the snap handler.
        /// </summary>
        public void setExtends(SBDPage newProperty)
        {
            SBDPage extends = sbdPage.getExtends();

            setBackgroundForThis("");
            if (newProperty != null && extends != null && !extends.getLayer().Equals(newProperty.getLayer())
                || extends == null && newProperty != null)
            {
                Page backPage = Globals.ThisAddIn.getModelController().getSbdPageController(newProperty).visioPage;
                setBackgroundForThis(backPage.NameU);
                sbdPage.setExtends(newProperty);
                snapHandler.setBackgroundPage(newProperty);
                foreach (Shape shape in visioPage.Shapes)
                {
                    if (shape.CellExistsU["Prop." + Constants.Properties.Transition.Extends + ".Value", 0] != 0)
                    {
                        string formula = shape.CellsU["Prop." + Constants.Properties.Transition.Extends + ".Value"].Formula;
                        snapHandler.snap(shape, formula);
                    }
                }
                if (!backLayerExists())
                {
                    placeBackRectangle(newProperty);
                }
            }
            else if (newProperty == null)
            {
                setBackgroundForThis("");
                deleteBackRectangle();
                sbdPage.setExtends(null);
            }
        }

        public override DiagramPageController getController(DiagramPage background)
        {
            return modelController.getSbdPageController(background);
        }

        public SbdSnapHandler getSbdSnapHandler()
        {
            return snapHandler;
        }
    }
}
