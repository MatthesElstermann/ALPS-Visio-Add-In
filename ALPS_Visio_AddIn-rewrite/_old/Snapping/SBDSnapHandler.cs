using Microsoft.Office.Interop.Visio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using VisioAddIn;
using VisioAddIn.util;
using MessageBox = System.Windows.MessageBox;

namespace VisioAddIn.Snapping
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
        /// checks for given snappingShape if it should snap
        /// shapes should snap when they are state extensions.
        /// </summary>
        /// <param name="shape">snappingShape to check</param>
        /// <returns>true if snappable, false otherwise</returns>
        protected override bool isShapeSnappable(IVShape shape)
        {
            Debug.Print("testing shape: " + shape.NameU + " - is snappable: " + shape.HasCategory(ALPSConstants.alpsShapeCategoryStateExtension) +
                 " on: " + this.foregroundPage.getNameU() + " with background: " + this.referencedBackgroundPage.getNameU());
            return shape.HasCategory(ALPSConstants.alpsShapeCategoryStateExtension);
        }

        protected override void setBackPage(DiagramPage newProperty)
        {
            if (newProperty is SBDPage sbdPage)
                this.referencedBackgroundPage = sbdPage;
        }
        
        /// <summary>
        /// A plug-in method which is called by the abstract base class 
        /// </summary>
        /// <param name="snappingShape"></param>
        protected override void handleDistantSnappedShapes(Shape snappingShape)
        {
            // Ask if the shapes should stay snapped
            WindowSnapMaintenance snapMain = new WindowSnapMaintenance(this, snappingShape, snappedShapes[snappingShape]);
            snapMain.Show();
        }

        protected override IEnumerable<Shape> getSnappableShapesOnBackgroundPage()
        {
            SBDPageController referencedBackgroundPageController = modelController.getSbdPageController(referencedBackgroundPage);
            
            if (referencedBackgroundPageController == null) return new List<Shape>();
            return referencedBackgroundPageController.getPage().Shapes.Cast<Shape>()
                .Where(shape => shape.HasCategory(ALPSConstants.alpsShapeCategorySBDState)).ToList();
        }

        /// <summary>
        /// snaps the snappingShape to a another one, specified by name.
        /// </summary>
        /// <param name="snappingShape"></param>
        /// <param name="backgroundReferenceShapeName"></param>
        public override void snap(Shape snappingShape, string backgroundReferenceShapeName)
        {
            if (!isShapeSnappable(snappingShape)) return;
            backgroundReferenceShapeName = backgroundReferenceShapeName.Trim('\\', '"');
            if ((!snappedShapes.ContainsKey(snappingShape) || snappedShapes[snappingShape].Name.Equals(backgroundReferenceShapeName)) &&
                snappedShapes.ContainsKey(snappingShape)) return;
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
                if (found == false)
                {
                    // Deprecated
                    // UserInputNotFound notFound = UserInputNotFound.GetInstance(ModelController, backgroundReferenceShapeName, snappingShape.nameU);
                    // notFound.Show();
                    MessageBox.Show(string.Format(ALPSConstants.InputNotFound, backgroundReferenceShapeName, snappingShape.NameU), "Error", MessageBoxButton.OK);
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
        /// <param name="shape"></param>
        /// <param name="snapToShape"></param>
        /// <returns>true if minimum one corner is near, false otherwise</returns>
        private static bool checkBorders(IVShape shape, IVShape snapToShape)
        {
            //check if one of the borders is near
            //subj ext: *0.5, Width, Height
            //state the same. :)

            //Check if outer borders are near to each other.
            ShapeCorners snappingShapeVectors = new ShapeCorners(shape);
            ShapeCorners referenceBackgroundShapeVectors = new ShapeCorners(snapToShape);

            return snappingShapeVectors.isCloseToAtLeastOneOtherCorner(referenceBackgroundShapeVectors);
        }



        /// <summary>
        /// unsnaps a snappingShape
        /// </summary>
        /// <param name="shape"></param>
        public override void unsnap(Shape shape)
        {
            if (!snappedShapes.ContainsKey(shape)) return;
            snappedShapes.Remove(shape);
            if (shape.CellExistsU[
                    ALPSConstants.cellPropertyCategoryPrefix + ALPSConstants.alpsPropertieTypeExtends +
                    ALPSConstants.cellValueSuffix, 0] == 0) return;
            Cell cell = shape.CellsU[ALPSConstants.cellValuePropertyExtends];
            cell.Formula = "";
        }

        /// <summary>
        /// called from SnapConfirmation.
        /// </summary>
        /// <param name="snap">true if it should snap, false if not</param>
        public override void performSnap(Shape snappingShape, Shape backgroundReferenceShape)
        {
            base.performSnap(snappingShape, backgroundReferenceShape);

            if (snappingShape.CellExistsU[ALPSConstants.cellValuePropertyExtends, 0] != 0)
            {
                Cell cell = snappingShape.CellsU[ALPSConstants.cellValuePropertyExtends];
                string snapToShapeId = backgroundReferenceShape.CellsU[ALPSConstants.cellValuePropertyModelComponentId].ResultStr[""];
                cell.Formula = "\"" + snapToShapeId + "\"";

            }
            if (snappingShape.CellExistsU[ALPSConstants.cellValuePropertyLabel, 0] != 0)
            {
                //Cell cell = snappingShape.CellsU[GlobalVariables.LableProp];
                //string snapToShapeLable = backgroundReferenceShape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeLable + ".Value"].ResultStr[""];
                //cell.Formula = "\"" + GlobalVariables.LableExtension + snapToShapeLable + "\"";
            }
        }

        

    }


}
