using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite
{
    /// <summary>
    /// An abstract representation of a snapping controller wrapping a PASS model page and handling snapping events
    /// </summary>
    public abstract class DiagramPageController
    {
        /// <summary>
        /// Reference to the visio page the diagramPageController is wrapping
        /// </summary>
        protected Page visioPage;

        public enum SeparationStyle
        {
            NO_SEP,
            STANDARD_SEP,
            FULL_SEP
        }

        private const string BG_TRANSPARENT = "100%", BG_NON_TRANSPARENT = "0%", BG_MEDIUM_TRANSPARENT = "80%";

        private readonly IDictionary<SeparationStyle, string> separationTransparency =
            new Dictionary<SeparationStyle, string>()
            {
                { SeparationStyle.NO_SEP, BG_TRANSPARENT },
                { SeparationStyle.STANDARD_SEP, BG_MEDIUM_TRANSPARENT },
                { SeparationStyle.FULL_SEP, BG_NON_TRANSPARENT },
            };

        protected DiagramPageController(Page page)
        {
            visioPage = page;
        }

        /// <summary>
        /// returns the controller of the background page of the current
        /// </summary>
        public abstract DiagramPageController getController(DiagramPage background);

        /// <summary>
        /// sets the given page as background for this page.
        /// </summary>
        protected void setBackgroundForThis(string backgroundPageName)
        {
            this.visioPage.BackPage = "";
            foreach (var page in Globals.ThisAddIn.Application.ActiveDocument.Pages.Cast<Page>()
                         .Where(page => page.NameU.Equals(backgroundPageName)))
            {
                this.visioPage.BackPage = page.NameU;
            }
        }

        protected void setBackgroundForThis(Page page)
        {
            this.visioPage.BackPage = page;
        }

        /// <summary>
        /// places a rectangle on a separate layer so that the user
        /// can easily distinguish back- and foreground.
        /// </summary>
        protected void placeBackRectangle(DiagramPage newProperty)
        {
            Documents visioDocs = Globals.ThisAddIn.Application.Documents;
            Document visioStencil = visioDocs.OpenEx(ShapeFinder.getSIDName(),
                    (short)VisOpenSaveArgs.visOpenDocked);

            try
            {
                Master visioRectMaster = visioStencil.Masters.ItemU["alpsExtensionSeperator"];

                DiagramPageController newPropC = getController(newProperty);
                double width = newPropC.getWidth();
                double height = newPropC.getHeight();

                Shape visioRectShape = visioPage.Drop(visioRectMaster, 1, 1);
                visioPage.Layers.ItemU["BackgroundSeparatorLayer"].CellsC[7].Formula = "1";
            }
            catch (COMException)
            {
                // Happens when the visioRectMaster cannot be retrieved properly
            }
        }

        /// <summary>
        /// Fit the separation shape in the background to the current dimensions of the extended page
        /// </summary>
        protected void rearrangeBackRectangle(DiagramPage diagramPage)
        {
            Shape visioRectShape = getBackRectangle();

            DiagramPageController newPropC = getController(diagramPage);

            double pageWidth = newPropC.getWidth();
            double pageHeight = newPropC.getHeight();

            visioRectShape.NameU = "alpsExtensionSeperator";

            visioRectShape.CellsU["PinX"].Formula = (pageWidth / 2) + "mm";
            visioRectShape.CellsU["PinY"].Formula = (pageHeight / 2) + "mm";

            visioRectShape.CellsU["Width"].Formula = pageWidth + " mm";
            visioRectShape.CellsU["Height"].Formula = pageHeight + " mm";

            visioRectShape.SendToBack();
        }

        protected void deleteBackRectangle()
        {
            Layer backLayer = visioPage.Layers.Cast<Layer>()
                .FirstOrDefault(layer => layer.NameU.Equals("alpsExtensionSeperator"));

            if (backLayer == null) return;

            short row = backLayer.CellsC[7].Row;

            Cell cell = visioPage.PageSheet.CellsSRC[(short)VisSectionIndices.visSectionLayer,
                (short)(VisRowIndices.visRowLayer + row),
                (short)VisCellIndices.visLayerLock];
            cell.Formula = "0";
            backLayer.Delete(1);
        }

        /// <summary>
        /// Checks whether the current page contains a separating layer with a separation shape
        /// </summary>
        protected bool backLayerExists()
        {
            return visioPage.Layers.Cast<Layer>()
                .Any(layer => layer.NameU.Equals("BackgroundSeperatorLayer"));
        }

        /// <summary>
        /// Returns the background separator shape on the separation layer, or null
        /// </summary>
        protected Shape getBackRectangle()
        {
            return visioPage.Shapes.Cast<Shape>()
                .FirstOrDefault(shape => shape.NameU.Equals("alpsExtensionSeperator"));
        }

        /// <summary>
        /// Sets one of the predefined separation styles for the separation layer.
        /// The styles define the opacity of the layer
        /// </summary>
        public void setSeparationStyle(SeparationStyle style)
        {
            Shape backRectangle = getBackRectangle();
            if (backRectangle == null) return;

            if (style != SeparationStyle.NO_SEP)
            {
                int color = style == SeparationStyle.FULL_SEP ? 255 : 0;
                System.Drawing.Color fillColor = System.Drawing.Color.FromArgb(0, color, color, color);
                var targetCell = backRectangle.CellsSRC[(short)VisSectionIndices.visSectionObject,
                    (short)VisRowIndices.visRowFill,
                    (short)VisCellIndices.visFillForegnd];
                targetCell.FormulaU = "RGB(" + fillColor.R
                                             + ',' + fillColor.G
                                             + ',' + fillColor.B + ')';
            }

            backRectangle.CellsU["FillForegndTrans"].Formula = separationTransparency[style];
        }

        /// <summary>
        /// Returns the height of the wrapped page in mm
        /// </summary>
        public double getHeight()
        {
            return visioPage.PageSheet.CellsU["PageHeight"].Result[VisUnitCodes.visMillimeters];
        }

        /// <summary>
        /// Returns the width of the wrapped page in mm
        /// </summary>
        public double getWidth()
        {
            return visioPage.PageSheet.CellsU["PageWidth"].Result[VisUnitCodes.visMillimeters];
        }
    }
}
