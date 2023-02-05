using Microsoft.Office.Interop.Visio;
using System;
using System.Diagnostics;
using VisioAddIn;

namespace VisioAddIn.Snapping
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
            string pageLayer = visioPage.PageSheet.CellsU[ALPSConstants.cellValuePropertyPageLayer].Formula;
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
        /// <param name="cell">changed cell</param>
        private void onCellChanged(Cell cell)
        {
            //Debug.Print("event ons SBD Page: " + this.sbdPage.getNameU());
            SBDPage extends = sbdPage.getExtends();
            if (cell.Name == ALPSConstants.cellValuePropertyExtends)
            {
                if (extends != null)
                {
                    snapHandler.snap(cell.Shape, cell.Formula);
                }
            }
            else if (cell.Name == ALPSConstants.shapeCellShapeTransformPinX || cell.Name == ALPSConstants.shapeCellShapeTransformPinY)
            {
                //Debug.Print("XY transform change even! Extends null: " + (extends == null));
                if (extends != null)
                {
                    //Debug.Print("SBD Event check for snapping:");
                    snapHandler.checkForSnapping(cell.Shape);
                }
                //not else, bc a page could be in the middle (is extending and is extended)
                if (sbdPage.getForeground() != null)
                {
                    //if the page is a background page, listen if states are moved
                    //(and snapped extensions should move, too)
                    sidController.sbdBackgroundShapeMoved(cell.Shape, sbdPage.getForeground());
                }
            }
            else if (cell.Name == ALPSConstants.pageCellPagePropertiesPageWidth || cell.Name == ALPSConstants.pageCellPagePropertiesPageHeight)
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
            //Debug.Print(" visioPage is null: " + (visioPage == null));
            //Debug.Print(" visioPage IDl: " + visioPage.ID);
            if ((visioPage == null)||(visioPage.ID<0))
            {
                return "";
            }
            else
            {
                return visioPage.NameU;
            }
        }

        /// <summary>
        /// has to be called if the sid page is not extending anything anymore.
        /// </summary>
        public void setNotExtended()
        {
            //visioPage.Background = 0;
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
        /// <param name="newProperty">new extends-property (background); can be null</param>
        public void setExtends(SBDPage newProperty)
        {
            
            SBDPage extends = sbdPage.getExtends();

            //Debug.Print("newProperty == null: " + (newProperty == null));
           // Debug.Print("extends == null: " + (extends == null));

            //Boolean demo = ( newProperty != null && extends != null && !extends.getLayer().Equals(newProperty.getLayer())
               // || extends == null && newProperty != null);

               // Debug.Print(" demo: " + demo);

            setBackgroundForThis("");
            //assure that there really IS a change in extends.
            if (newProperty != null && extends != null && !extends.getLayer().Equals(newProperty.getLayer())
                || extends == null && newProperty != null)
            {
                Page backPage = Globals.ThisAddIn.getModelController().getSbdPageController(newProperty).visioPage;
                setBackgroundForThis(backPage.NameU);
                sbdPage.setExtends(newProperty);
                snapHandler.setBackgroundPage(newProperty);
                foreach (Shape shape in visioPage.Shapes)
                {
                    if (shape.CellExistsU[ALPSConstants.cellValuePropertyExtends, 0] != 0)
                    {
                        string formula = shape.CellsU[ALPSConstants.cellValuePropertyExtends].Formula;
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
            //SBDPage.SetExtends(newProperty);
            //SnapHandler.SetBackgroundPage(newProperty);
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
