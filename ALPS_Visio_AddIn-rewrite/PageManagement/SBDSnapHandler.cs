using Microsoft.Office.Interop.Visio;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
namespace ALPS_Visio_AddIn_rewrite
{
    /// <summary>
    /// manages the snapping of state extensions to states on sbd pages.
    /// </summary>
    public class SbdSnapHandler : SnapHandler
    {
        private SBDPage foregroundPage;
        private SBDPage referencedBackgroundPage;

        private readonly ModelController modelController;

        public SbdSnapHandler(SBDPage foregroundPage, ModelController modelController) : base()
        {
            Debug.Print("Creating SbdSnapHandler for: " + foregroundPage.getNameU());
            this.modelController = modelController;
            this.foregroundPage = foregroundPage;
            referencedBackgroundPage = null;
        }

        /// <summary>
        /// checks for given snappingShape if it should snap — shapes should snap when they are state extensions.
        /// </summary>
        protected override bool isShapeSnappable(IVShape shape)
        {
            // A state extension snaps onto a background state. The stencil tags the generic
            // extension master as "StateExtension", but the guard-behaviour states
            // (GuardReceive/Send/Do) instead carry a per-type "Guard…State" category — accept both.
            if (shape.HasCategory("StateExtension")) return true;
            if (shape.CellExistsU["User.msvShapeCategories", 0] == 0) return false;
            return shape.CellsU["User.msvShapeCategories"].ResultStr[""].Contains("Guard");
        }

        protected override void setBackPage(DiagramPage newProperty)
        {
            if (newProperty is SBDPage sbdPage)
                this.referencedBackgroundPage = sbdPage;
        }

        /// <summary>
        /// A plug-in method which is called by the abstract base class
        /// </summary>
        protected override void handleDistantSnappedShapes(Shape snappingShape)
        {
            WindowSnapMaintenance snapMain = new WindowSnapMaintenance(this, snappingShape, snappedShapes[snappingShape]);
            snapMain.Show();
        }

        protected override IEnumerable<Shape> getSnappableShapesOnBackgroundPage()
        {
            SBDPageController referencedBackgroundPageController = modelController.getSbdPageController(referencedBackgroundPage);

            if (referencedBackgroundPageController == null) return new List<Shape>();
            return referencedBackgroundPageController.getPage().Shapes.Cast<Shape>()
                .Where(shape => shape.HasCategory("alpsSBDstate")).ToList();
        }

        /// <summary>
        /// snaps the snappingShape to another one, specified by name.
        /// </summary>
        public override void snap(Shape snappingShape, string backgroundReferenceShapeName)
        {
            if (!isShapeSnappable(snappingShape)) return;
            backgroundReferenceShapeName = backgroundReferenceShapeName.Trim('\\', '"');
            // already snapped to the requested shape — nothing to do
            if (snappedShapes.ContainsKey(snappingShape) && snappedShapes[snappingShape].Name.Equals(backgroundReferenceShapeName)) return;
            if (string.IsNullOrWhiteSpace(backgroundReferenceShapeName))
            {
                if (snappedShapes.ContainsKey(snappingShape))
                {
                    unsnap(snappingShape);
                }
            }
            else
            {
                IEnumerable<Shape> snappableShapes = getSnappableShapesOnBackgroundPage();
                bool found = false;

                foreach (Shape snappable in snappableShapes)
                {
                    string modelCompId = snappable.CellsU["Prop.modelComponentID.Value"].ResultStr[""];
                    if (!modelCompId.Equals(backgroundReferenceShapeName)) continue;
                    performSnap(snappingShape, snappable);
                    found = true;
                }
                if (!found)
                {
                    MessageBox.Show(
                        string.Format("Eingabe \"{0}\" wurde nicht gefunden. Ort der fehlerhaften Eingabe: \"{1}\"",
                            backgroundReferenceShapeName, snappingShape.NameU),
                        "Error", MessageBoxButton.OK);
                }
            }
        }

        public void maintainSnap(Shape shape, Shape snapToShape)
        {
            if (!checkBorders(shape, snapToShape))
            {
                adjustSize(shape, snapToShape);
            }
        }

        /// <summary>
        /// checks if the corners of two shapes are near to each other
        /// </summary>
        private static bool checkBorders(IVShape shape, IVShape snapToShape)
        {
            ShapeCorners snappingShapeVectors = new ShapeCorners(shape);
            ShapeCorners referenceBackgroundShapeVectors = new ShapeCorners(snapToShape);
            return snappingShapeVectors.isCloseToAtLeastOneOtherCorner(referenceBackgroundShapeVectors);
        }

        /// <summary>
        /// unsnaps a snappingShape
        /// </summary>
        public override void unsnap(Shape shape)
        {
            if (!snappedShapes.ContainsKey(shape)) return;
            snappedShapes.Remove(shape);
            if (shape.CellExistsU["Prop." + Constants.Properties.Transition.Extends + ".Value", 0] == 0) return;
            Cell cell = shape.CellsU["Prop." + Constants.Properties.Transition.Extends + ".Value"];
            cell.Formula = "";
        }

        /// <summary>
        /// called from SnapConfirmation.
        /// </summary>
        public override void performSnap(Shape snappingShape, Shape backgroundReferenceShape)
        {
            base.performSnap(snappingShape, backgroundReferenceShape);

            if (snappingShape.CellExistsU["Prop." + Constants.Properties.Transition.Extends + ".Value", 0] != 0)
            {
                Cell cell = snappingShape.CellsU["Prop." + Constants.Properties.Transition.Extends + ".Value"];
                string snapToShapeId = backgroundReferenceShape.CellsU["Prop.modelComponentID.Value"].ResultStr[""];
                cell.Formula = VisioHelper.QuoteLiteral(snapToShapeId);
            }
            if (snappingShape.CellExistsU["Prop.lable.Value", 0] != 0)
            {
                // lable cell exists but update logic is intentionally commented out
            }
        }
    }
}
