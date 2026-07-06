using Microsoft.Office.Interop.Visio;
using System.Diagnostics;
using System.Linq;

namespace ALPS_Visio_AddIn_rewrite
{
    public class SBDPageController : DiagramPageController
    {
        private readonly ModelController modelController;
        private readonly SIDPageController sidController;
        private readonly SbdSnapHandler snapHandler;

        private SBDPage sbdPage;

        /// <summary>Guards <see cref="tryDeriveExtends"/> against re-entrancy while it mutates the page.</summary>
        private bool derivingExtends;

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
            tryDeriveExtends();
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
                tryDeriveExtends();
                if (sbdPage.getExtends() != null)
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

        /// <summary>
        /// Establishes this SBD's background (extended) page without requiring a live SID snap,
        /// so that snapping on a GBD works on its own. The background is derived from the owning
        /// subject's SID-layer relationship: if the SID page extends a base layer, the base
        /// subject — identified by this subject's <c>extendedSubject</c> link, or by an identical
        /// name as a fallback — contributes its SBD as this page's background.
        ///
        /// Idempotent and cheap: it is safe to call on every shape interaction and does nothing
        /// once an extends is set or while the relationship cannot be derived yet.
        /// </summary>
        private void tryDeriveExtends()
        {
            if (derivingExtends || sbdPage.getExtends() != null) return;

            SIDPage baseSidPage = sidController.getExtends();
            if (baseSidPage == null) return;

            Shape owningSubject = getOwningSubjectShape();
            if (owningSubject == null) return;

            string baseSubjectName = getExtendedSubjectName(owningSubject);
            if (string.IsNullOrWhiteSpace(baseSubjectName)) baseSubjectName = owningSubject.NameU;

            SBDPage baseSbd = getSbdOfSubjectOn(baseSidPage, baseSubjectName);
            if (baseSbd == null) return;

            derivingExtends = true;
            try { setExtends(baseSbd); }
            finally { derivingExtends = false; }
        }

        /// <summary>Finds the subject shape on the owning SID page that this SBD belongs to.</summary>
        private Shape getOwningSubjectShape()
        {
            if (visioPage.PageSheet.CellExistsU["Prop." + Constants.Properties.SBDLinkedSubjectID, 0] == 0) return null;
            int subjectId = (int)visioPage.PageSheet.CellsU["Prop." + Constants.Properties.SBDLinkedSubjectID].ResultIU;
            try { return sidController.getPage().Shapes.ItemFromID[subjectId]; }
            catch { return null; }
        }

        /// <summary>
        /// Reads the base subject name from a subject's <c>extendedSubject</c> hyperlink. The link is
        /// stored as <c>&lt;layer&gt;/&lt;subjectName&gt;</c>; returns the bare subject name (empty if unset).
        /// </summary>
        private static string getExtendedSubjectName(Shape subjectShape)
        {
            if (subjectShape.CellExistsU["Hyperlink." + Constants.Properties.ExtendedSubject, 0] == 0) return "";
            string sub = subjectShape.Hyperlinks.ItemU[Constants.Properties.ExtendedSubject].SubAddress;
            if (string.IsNullOrWhiteSpace(sub)) return "";
            return sub.Contains("/") ? sub.Substring(sub.LastIndexOf('/') + 1) : sub;
        }

        /// <summary>
        /// Resolves the SBD page of the base subject identified by <paramref name="subjectKey"/> on the given SID
        /// page. The key matches either the shape's NameU (manual build / SID snap write the bare subject name)
        /// or its <c>Prop.modelComponentID</c> (import writes the model ID, since dropped shapes get a generic NameU).
        /// </summary>
        private SBDPage getSbdOfSubjectOn(SIDPage baseSidPage, string subjectKey)
        {
            Page basePage = modelController.getSidPageController(baseSidPage)?.getPage();
            Shape baseSubject = basePage?.Shapes.Cast<Shape>()
                .FirstOrDefault(s => s.NameU.Equals(subjectKey) || readModelComponentId(s).Equals(subjectKey));
            if (baseSubject == null) return null;
            if (baseSubject.CellExistsU["Hyperlink." + Constants.Properties.LinkedSBD, 0] == 0) return null;
            string sbdName = baseSubject.Hyperlinks.ItemU[Constants.Properties.LinkedSBD].SubAddress;
            return modelController.getSbdPage(sbdName);
        }

        /// <summary>Reads a shape's <c>Prop.modelComponentID</c> value (empty string if the cell is absent).</summary>
        private static string readModelComponentId(Shape shape)
        {
            return shape.CellExistsU["Prop." + Constants.Properties.ID, 0] != 0
                ? shape.CellsU["Prop." + Constants.Properties.ID].ResultStr[""] : "";
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
