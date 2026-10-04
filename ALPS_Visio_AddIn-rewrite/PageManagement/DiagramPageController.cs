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
            // Im Dokument der eigenen Seite suchen, nicht im (evtl. inzwischen anderen) aktiven.
            foreach (var page in this.visioPage.Document.Pages.Cast<Page>()
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

            // Look the master up with FirstOrDefault instead of Masters.ItemU: a missing master makes
            // ItemU throw a COMException that was merely swallowed below. The throw itself still fires
            // on every extends establishment (import AND every window switch on layered docs, via
            // reset()/updateExtends) and each one is an expensive debugger notification — the main
            // source of the "everything feels slow" symptom. FirstOrDefault returns null instead.
            Master visioRectMaster = visioStencil.Masters.Cast<Master>()
                .FirstOrDefault(master => master.NameU.Equals(Constants.ExtensionSeparatorMasterName));
            if (visioRectMaster == null) return;

            try
            {
                visioPage.Drop(visioRectMaster, 1, 1);
                // Den Trenn-Layer sperren, damit die Hintergrund-Flaeche nicht versehentlich
                // ausgewaehlt wird (CellsC[7] war visLayerSnap, nicht das Sperren).
                Layer backLayer = findBackLayer();
                if (backLayer != null)
                    backLayer.CellsC[(short)VisCellIndices.visLayerLock].FormulaU = "1";
            }
            catch (COMException)
            {
                // The layer/cell setup can still fail on some stencils; keep the dropped shape.
            }
        }

        /// <summary>
        /// Fit the separation shape in the background to the current dimensions of the extended page
        /// </summary>
        protected void rearrangeBackRectangle(DiagramPage diagramPage)
        {
            Shape visioRectShape = getBackRectangle();
            DiagramPageController newPropC = getController(diagramPage);
            // Kein Trenn-Rechteck (Master fehlte / vom Nutzer geloescht) oder Controller nicht
            // (mehr) bekannt — frueher NullReferenceException im PageWidth-Event.
            if (visioRectShape == null || newPropC == null) return;

            double pageWidth = newPropC.getWidth();
            double pageHeight = newPropC.getHeight();

            visioRectShape.NameU = Constants.ExtensionSeparatorMasterName;

            // Kulturunabhaengig (FormulaU + InvariantCulture) statt "12,5mm" ueber .Formula.
            VisioHelper.SetCellMM(visioRectShape, "PinX", pageWidth / 2);
            VisioHelper.SetCellMM(visioRectShape, "PinY", pageHeight / 2);
            VisioHelper.SetCellMM(visioRectShape, "Width", pageWidth);
            VisioHelper.SetCellMM(visioRectShape, "Height", pageHeight);

            visioRectShape.SendToBack();
        }

        protected void deleteBackRectangle()
        {
            // Bisher wurde ein Layer namens "alpsExtensionSeperator" (= Shape-Name) gesucht,
            // den es nie gibt — die Trenn-Flaeche blieb nach dem Entfernen von extends stehen.
            Layer backLayer = findBackLayer();

            if (backLayer == null) return;

            backLayer.CellsC[(short)VisCellIndices.visLayerLock].FormulaU = "0";
            backLayer.Delete(1);
        }

        /// <summary>
        /// Checks whether the current page contains a separating layer with a separation shape
        /// </summary>
        protected bool backLayerExists()
        {
            return findBackLayer() != null;
        }

        /// <summary>
        /// Der Trenn-Layer heisst in bestehenden Dokumenten/Stencils "BackgroundSeperatorLayer"
        /// (Tippfehler im Stencil), teils auch korrekt geschrieben — beide Namen akzeptieren.
        /// </summary>
        private Layer findBackLayer()
        {
            return visioPage.Layers.Cast<Layer>().FirstOrDefault(layer =>
                layer.NameU.Equals(Constants.Layers.BackgroundSeparatorAlt) ||
                layer.NameU.Equals(Constants.Layers.BackgroundSeparator));
        }

        /// <summary>
        /// Returns the background separator shape on the separation layer, or null
        /// </summary>
        protected Shape getBackRectangle()
        {
            return visioPage.Shapes.Cast<Shape>()
                .FirstOrDefault(shape => shape.NameU.Equals(Constants.ExtensionSeparatorMasterName));
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
