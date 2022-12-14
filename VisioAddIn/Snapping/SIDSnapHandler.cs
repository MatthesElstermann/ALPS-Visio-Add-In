using Microsoft.Office.Interop.Visio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using VisioAddIn;

namespace VisioAddIn.Snapping
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
            this.foregroundPage = foregroundPage;
            this.modelController = modelController;

            referencedBackgroundPage = null;
        }

        /*public void notifyBackgroundShapeMoved(Shape movedReferenceBackgroundShape)
        {
            if (!snappedShapes.Values.Contains(movedReferenceBackgroundShape)) return;

            Shape shape = snappedShapes.FirstOrDefault(x => x.Value == movedReferenceBackgroundShape).Key;
            adjustSize(shape, movedReferenceBackgroundShape);
        }*/

        /// <summary>
        /// checks for given snappingShape if it should snap
        /// shapes should snap when they are actor extensions.
        /// </summary>
        /// <param name="shape">snappingShape to check</param>
        /// <returns>true if snappable, false otherwise</returns>
        protected override bool isShapeSnappable(IVShape shape)
        {
            //check for category of snappingShape - should it snap to other shapes?
            return shape.HasCategory(ALPSConstants.alpsShapeCategoryActorExtension);
        }

        protected override void setBackPage(DiagramPage newProperty)
        {
            if (newProperty is SIDPage sidPage)
                this.referencedBackgroundPage = sidPage;
        }

        /// <summary>
        /// snaps the snappingShape to a another one, specified by name.
        /// </summary>
        /// <param name="snappingShape">snappingShape to snap</param>
        /// <param name="backgroundReferenceShapeName">background snappingShape</param>
        public override void snap(Shape snappingShape, string backgroundReferenceShapeName)
        {
            if (!isShapeSnappable(snappingShape)) return;

            backgroundReferenceShapeName = backgroundReferenceShapeName.Trim('\\', '"');

            if (snappedShapes.ContainsKey(snappingShape) && snappedShapes[snappingShape].Name.Equals(backgroundReferenceShapeName)) return;

            if (string.IsNullOrWhiteSpace(backgroundReferenceShapeName))
            {
                unsnap(snappingShape);
                return;
            }

            IEnumerable<Shape> snappableShapes = getSnappableShapesOnBackgroundPage();

            foreach (Shape snappable in snappableShapes)
            {
                if (!snappable.Name.Equals(backgroundReferenceShapeName)) continue;
                performSnap(snappingShape, snappable);
            }
        }

        /// <summary>
        /// A plug-in method which is called by the abstract base class 
        /// </summary>
        protected override void handleDistantSnappedShapes(Shape snappingShape)
        {
            unsnap(snappingShape);
        }

        /// <summary>
        /// unsnaps a given snappingShape and the associated sbd page.
        /// </summary>
        /// <param name="shape">snappingShape to unsnap</param>
        public override void unsnap(Shape shape)
        {
            if (!snappedShapes.ContainsKey(shape)) return;
            Shape referenceBackgroundShape = snappedShapes[shape];
            SBDPage shapePage = null;
            SBDPage snapToShapePage = null;

            if (shape.CellExistsU[ALPSConstants.cellSubAdressHyperlinkLinkedSBD, 0] != 0)
            {
                shapePage = foregroundPage.getSbdPage(shape.CellsU[ALPSConstants.cellSubAdressHyperlinkLinkedSBD].Formula);
            }

            if (referenceBackgroundShape.CellExistsU[ALPSConstants.cellSubAdressHyperlinkLinkedSBD, 0] != 0)
            {
                snapToShapePage = referencedBackgroundPage.getSbdPage(referenceBackgroundShape.CellsU[ALPSConstants.cellSubAdressHyperlinkLinkedSBD].Formula);
            }



            if (shapePage != null && snapToShapePage != null)
            {
                modelController.getSbdPageController(shapePage).setExtends(null);
                modelController.getSbdPageController(snapToShapePage).setNotExtended();

                snappedShapes.Remove(shape);
            }

            // Clear snappingShape contents that are related to snapping
            if (shape.CellExistsU[ALPSConstants.cellSubAdressHyperlinkExtendedSubject, 0] != 0)
                shape.CellsU[ALPSConstants.cellSubAdressHyperlinkExtendedSubject].Formula = "";

            if (shape.CellExistsU[ALPSConstants.cellValuePropertyExtends, 0] != 0)
                shape.CellsU[ALPSConstants.cellValuePropertyExtends].Formula = "";

        }

        public void setModelUri(string newModelURI)
        {
            foreach (Shape shape in snappedShapes.Keys)
            {
                if (shape.CellExistsU[ALPSConstants.cellValuePropertyExtends, 0] == 0) continue;
                Cell cell = shape.CellsU[ALPSConstants.cellValuePropertyExtends];
                cell.Formula = "\"" + newModelURI + "#" + snappedShapes[shape].NameU + "\"";
            }
        }

        /// <summary>
        /// called after a snappingShape is added.
        /// if it's an actor extension, the diagram should be empty after adding 
        /// bc the new snappingShape isn't extending anything.
        /// </summary>
        /// <param name="shape"></param>
        internal void clearNewPage(Shape shape)
        {
            if (isShapeSnappable(shape))
            {
                SBDPage sbdPage = foregroundPage.getSbdPage(shape.NameU);
            }
        }

        /// <summary>
        /// called from SnapConfirmation.
        /// eventually snaps a snappingShape and the page associated with it.
        /// </summary>
        /// <param name="snap">true if it should snap, false if not</param>
        /// <param name="snappingShape"></param>
        /// <param name="backgroundReferenceShape"></param>
        public override void performSnap(Shape snappingShape, Shape backgroundReferenceShape)
        {
            base.performSnap(snappingShape, backgroundReferenceShape);

            if (snappingShape.CellExistsU[ALPSConstants.cellSubAdressHyperlinkExtendedSubject, 0] != 0)
            {
                Cell snappingShapeExtendedSubjectCell = snappingShape.CellsU[ALPSConstants.cellSubAdressHyperlinkExtendedSubject];
                snappingShapeExtendedSubjectCell.Formula = "\"" + referencedBackgroundPage.getLayerForUser() + "/" + backgroundReferenceShape.NameU + "\"";
            }
            if (snappingShape.CellExistsU[ALPSConstants.cellValuePropertyExtends, 0] != 0)
            {
                Cell snappingShapeExtendsCell = snappingShape.CellsU[ALPSConstants.cellValuePropertyExtends];
                snappingShapeExtendsCell.Formula = "\"" + referencedBackgroundPage.getModelUriForUser() + "#" + backgroundReferenceShape.NameU + "\"";
            }
            if (snappingShape.CellExistsU[ALPSConstants.cellValuePropertyLabel, 0] != 0)
            {
                //Cell cell = snappingShape.CellsU[GlobalVariables.LableProp];
                //cell.Formula = "\"" + GlobalVariables.LableExtension + snapToShape.nameU + "\"";
            }

            SBDPage shapePage = null;
            SBDPage snapToShapePage = null;

            if (snappingShape.CellExistsU[ALPSConstants.cellSubAdressHyperlinkLinkedSBD, 0] != 0)
            {
                shapePage = foregroundPage.getSbdPage(snappingShape.CellsU[ALPSConstants.cellSubAdressHyperlinkLinkedSBD].Formula);
            }
            if (backgroundReferenceShape.CellExistsU[ALPSConstants.cellSubAdressHyperlinkLinkedSBD, 0] != 0)
            {
                snapToShapePage = referencedBackgroundPage.getSbdPage(backgroundReferenceShape.CellsU[ALPSConstants.cellSubAdressHyperlinkLinkedSBD].Formula);
            }


            if (shapePage == null || snapToShapePage == null) return;

            SBDPageController shapePageC = modelController.getSbdPageController(shapePage);
            SBDPageController snapToShapePageC = modelController.getSbdPageController(snapToShapePage);

            // Gets the shapeType of the snappingShape currently snapping
            SBDPage oldExtends = shapePage.getExtends();
            string shapeType = snappingShape.CellsU[ALPSConstants.cellValuePropertyModelComponentType].Formula;
            shapeType = shapeType.Replace("\"", "");

            // Do not set Extends for SBD if it is a makro extension
            if (!shapeType.Equals(ALPSConstants.MacroExtension))
            {
                snapToShapePageC.setExtended(shapePage);
                shapePageC.setExtends(snapToShapePage);
            }

            if (oldExtends == null) return;
            SBDPageController oldExtendsC = modelController.getSbdPageController(oldExtends);
            oldExtendsC.setNotExtended();

        }

        /// <summary>
        /// checks for a given page if there are standard actors shapes should be snapping to.
        /// </summary>
        /// <param name="backPage"></param>
        /// <returns></returns>
        protected override IEnumerable<Shape> getSnappableShapesOnBackgroundPage()
        {
            SIDPageController referencedBackgroundPageController = modelController.getSidPageController(referencedBackgroundPage);
            return referencedBackgroundPageController == null ? new List<Shape>() :
                referencedBackgroundPageController.getPage().Shapes.Cast<Shape>()
                    .Where(shape => shape.HasCategory(ALPSConstants.alpsShapeCategoryStandardActor)).ToList();
        }
    }
}
