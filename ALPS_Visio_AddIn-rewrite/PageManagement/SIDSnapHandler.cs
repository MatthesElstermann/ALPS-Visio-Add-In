using Microsoft.Office.Interop.Visio;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ALPS_Visio_AddIn_rewrite
{
    /// <summary>
    /// Observes SID pages and checks for the snapping of subjects to subjects on the referenced background page
    /// </summary>
    public class SidSnapHandler : SnapHandler
    {
        /// <summary>
        /// The page which is currently observed and which contains a referenced background page
        /// </summary>
        private readonly SIDPage foregroundPage;

        /// <summary>
        /// The background to the currently active page
        /// </summary>
        private SIDPage referencedBackgroundPage;

        private readonly ModelController modelController;

        public SidSnapHandler(ModelController modelController, SIDPage foregroundPage) : base()
        {
            Debug.Print("Creating SidSnapHandler for: " + foregroundPage.getNameU());
            this.foregroundPage = foregroundPage;
            this.modelController = modelController;
            referencedBackgroundPage = null;
        }

        /// <summary>
        /// checks for given snappingShape if it should snap — shapes should snap when they are actor extensions.
        /// </summary>
        protected override bool isShapeSnappable(IVShape shape)
        {
            return shape.HasCategory("ActorExtension");
        }

        protected override void setBackPage(DiagramPage newProperty)
        {
            // Auch null uebernehmen: wird extends entfernt, darf kein veralteter
            // Hintergrund-Verweis zurueckbleiben.
            this.referencedBackgroundPage = newProperty as SIDPage;
        }

        /// <summary>
        /// snaps the snappingShape to another one, specified by name.
        /// </summary>
        public override void snap(Shape snappingShape, string backgroundReferenceShapeName)
        {
            if (!isShapeSnappable(snappingShape)) return;

            backgroundReferenceShapeName = backgroundReferenceShapeName.Trim('\\', '"');

            // performSnap schreibt NameU in den Hyperlink, Nutzer/Altbestand ggf. den lokalen Namen
            // — beide akzeptieren (sonst loeste der eigene Schreibvorgang einen erneuten Snap aus).
            if (snappedShapes.ContainsKey(snappingShape) && hasName(snappedShapes[snappingShape], backgroundReferenceShapeName)) return;

            if (string.IsNullOrWhiteSpace(backgroundReferenceShapeName))
            {
                unsnap(snappingShape);
                return;
            }

            IEnumerable<Shape> snappableShapes = getSnappableShapesOnBackgroundPage();

            Shape target = snappableShapes.FirstOrDefault(snappable => hasName(snappable, backgroundReferenceShapeName));
            if (target != null) performSnap(snappingShape, target);
        }

        private static bool hasName(Shape shape, string name)
        {
            return shape.Name.Equals(name) || shape.NameU.Equals(name);
        }

        /// <summary>
        /// A plug-in method which is called by the abstract base class.
        /// Frueher wurde hier kommentarlos getrennt — jetzt fragt (wie auf SBD-Seiten)
        /// ein Bestaetigungsdialog nach, ob der Snap wirklich geloest werden soll.
        /// </summary>
        protected override void handleDistantSnappedShapes(Shape snappingShape)
        {
            showMaintenanceDialog(snappingShape);
        }

        /// <summary>
        /// unsnaps a given snappingShape and the associated sbd page.
        /// </summary>
        public override void unsnap(Shape shape)
        {
            if (!snappedShapes.ContainsKey(shape)) return;
            Shape referenceBackgroundShape = snappedShapes[shape];
            SBDPage shapePage = null;
            SBDPage snapToShapePage = null;

            if (shape.CellExistsU["Hyperlink." + Constants.Properties.LinkedSBD, 0] != 0)
            {
                shapePage = foregroundPage.getSbdPage(shape.Hyperlinks.ItemU[Constants.Properties.LinkedSBD].SubAddress);
            }
            if (referenceBackgroundShape.CellExistsU["Hyperlink." + Constants.Properties.LinkedSBD, 0] != 0)
            {
                snapToShapePage = referencedBackgroundPage?.getSbdPage(referenceBackgroundShape.Hyperlinks.ItemU[Constants.Properties.LinkedSBD].SubAddress);
            }

            if (shapePage != null)
            {
                Debug.Print("setting to null");
                modelController.getSbdPageController(shapePage)?.setExtends(null);
            }
            // Immer austragen — auch ohne verlinkte SBD, sonst zieht notifyBackgroundShapeMoved
            // das Shape weiter mit, obwohl es nicht mehr gesnappt ist.
            snappedShapes.Remove(shape);
            if (snapToShapePage != null)
            {
                modelController.getSbdPageController(snapToShapePage)?.setNotExtended();
            }

            if (shape.CellExistsU["Hyperlink." + Constants.Properties.ExtendedSubject, 0] != 0)
                shape.Hyperlinks.ItemU[Constants.Properties.ExtendedSubject].SubAddress = "";

            if (shape.CellExistsU["Prop." + Constants.Properties.Transition.Extends + ".Value", 0] != 0)
                shape.CellsU["Prop." + Constants.Properties.Transition.Extends + ".Value"].Formula = "";
        }

        public void setModelUri(string newModelURI)
        {
            foreach (Shape shape in snappedShapes.Keys)
            {
                if (shape.CellExistsU["Prop." + Constants.Properties.Transition.Extends + ".Value", 0] == 0) continue;
                Cell cell = shape.CellsU["Prop." + Constants.Properties.Transition.Extends + ".Value"];
                cell.Formula = VisioHelper.QuoteLiteral(newModelURI + "#" + snappedShapes[shape].NameU);
            }
        }

        /// <summary>
        /// called from SnapConfirmation — eventually snaps a snappingShape and the page associated with it.
        /// </summary>
        public override void performSnap(Shape snappingShape, Shape backgroundReferenceShape)
        {
            base.performSnap(snappingShape, backgroundReferenceShape);
            if (referencedBackgroundPage == null) return;

            if (snappingShape.CellExistsU["Hyperlink." + Constants.Properties.ExtendedSubject, 0] != 0)
            {
                snappingShape.Hyperlinks.ItemU[Constants.Properties.ExtendedSubject].SubAddress =
                    referencedBackgroundPage.getLayerForUser() + "/" + backgroundReferenceShape.NameU;
            }

            if (snappingShape.CellExistsU["Prop." + Constants.Properties.Transition.Extends + ".Value", 0] != 0)
            {
                Cell snappingShapeExtendsCell = snappingShape.CellsU["Prop." + Constants.Properties.Transition.Extends + ".Value"];
                snappingShapeExtendsCell.Formula = VisioHelper.QuoteLiteral(referencedBackgroundPage.getModelUriForUser() + "#" + backgroundReferenceShape.NameU);
            }

            SBDPage shapePage = null;
            SBDPage snapToShapePage = null;

            if (snappingShape.CellExistsU["Hyperlink." + Constants.Properties.LinkedSBD, 0] != 0)
            {
                shapePage = foregroundPage.getSbdPage(snappingShape.Hyperlinks.ItemU[Constants.Properties.LinkedSBD].SubAddress);
            }
            if (backgroundReferenceShape.CellExistsU["Hyperlink." + Constants.Properties.LinkedSBD, 0] != 0)
            {
                snapToShapePage = referencedBackgroundPage.getSbdPage(backgroundReferenceShape.Hyperlinks.ItemU[Constants.Properties.LinkedSBD].SubAddress);
            }

            if (shapePage == null || snapToShapePage == null) return;

            SBDPageController shapePageC = modelController.getSbdPageController(shapePage);
            SBDPageController snapToShapePageC = modelController.getSbdPageController(snapToShapePage);

            SBDPage oldExtends = shapePage.getExtends();

            if (!snappingShape.HasCategory("MacroExtension"))
            {
                snapToShapePageC.setExtended(shapePage);
                shapePageC.setExtends(snapToShapePage);
            }

            if (oldExtends == null) return;
            modelController.getSbdPageController(oldExtends)?.setNotExtended();
        }

        /// <summary>
        /// checks for a given page if there are standard actors shapes should be snapping to.
        /// </summary>
        protected override IEnumerable<Shape> getSnappableShapesOnBackgroundPage()
        {
            SIDPageController referencedBackgroundPageController = modelController.getSidPageController(referencedBackgroundPage);
            return referencedBackgroundPageController == null ? new List<Shape>() :
                referencedBackgroundPageController.getPage().Shapes.Cast<Shape>()
                    .Where(shape => shape.HasCategory("StandardActor")).ToList();
        }
    }
}
