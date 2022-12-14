using Microsoft.Office.Interop.Visio;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VisioAddIn.Snapping
{
    public abstract class SnapHandler
    {
        protected IDictionary<Shape, Shape> snappedShapes;

        /// <summary>
        /// const for distance btw 2 shapes
        /// </summary>
        public const int SNAP_RANGE = 20;

        protected SnapHandler()
        {
            snappedShapes = new Dictionary<Shape, Shape>();
        }

        public virtual void performSnap(Shape snappingShape, Shape backgroundReferenceShape)
        {
            if (!snappedShapes.ContainsKey(snappingShape))
            {
                snappedShapes.Add(snappingShape, backgroundReferenceShape);
            }

            // Delete inconsistent entries where the snapping snappingShape is still snapped with other shapes
            else if (snappedShapes[snappingShape] != backgroundReferenceShape)
            {
                snappedShapes.Remove(snappingShape);
                snappedShapes.Add(snappingShape, backgroundReferenceShape);
            }

            adjustSize(snappingShape, backgroundReferenceShape);
        }


        /// <summary>
        /// checks for the given snappingShape if it should be snapping to a snappingShape on the given backPage
        /// </summary>
        /// <param name="snappingShape">given snappingShape</param>
        public void checkForSnapping(Shape snappingShape)
        {
            if (!isShapeSnappable(snappingShape)) return;

            //check if on the backPage is sth it could be snapping to.
            IEnumerable<Shape> snappableActorShapes = getSnappableShapesOnBackgroundPage();

            //eventually unsnap here.
            if (snappedShapes.ContainsKey(snappingShape) && !isLocatedClosely(snappingShape, snappedShapes[snappingShape]))
            {
                handleDistantSnappedShapes(snappingShape);
            }

            foreach (Shape possibleReferenceBackgroundShape in snappableActorShapes)
            {
                bool snapValid = isLocatedClosely(snappingShape, possibleReferenceBackgroundShape);

                // TODO why is a short x distance a criteria to skip this snappingShape? 
                if (!snapValid || isLocatedCloselyInXDirection(snappingShape, possibleReferenceBackgroundShape, 0.1)) continue;

                // Bug: Method gets called 5 times, window can only be opened once
                WindowSnapConfirmation snapConf = new WindowSnapConfirmation(this, snappingShape, possibleReferenceBackgroundShape);
                snapConf.ShowDialog();
            }
        }

        protected abstract bool isShapeSnappable(IVShape shape);
        protected abstract void handleDistantSnappedShapes(Shape snappingShape);
        protected abstract IEnumerable<Shape> getSnappableShapesOnBackgroundPage();


        public abstract void snap(Shape snappingShape, string backgroundReferenceShapeName);
        public abstract void unsnap(Shape shape);
        /// <summary>
        /// sets the BackPage-Property to the newProperty given
        /// </summary>
        /// <param name="newProperty">new back page</param>
        protected abstract void setBackPage(DiagramPage newProperty);

        /// <summary>
        /// sets the background page and resets all the snapped shapes.
        /// </summary>
        /// <param name="newProperty">new background page; null if there is no background</param>
        public void setBackgroundPage(DiagramPage newProperty)
        {
            IList<Shape> listSnappedShapes = snappedShapes.Keys.ToList();
            foreach (Shape shape in listSnappedShapes)
            {
                unsnap(shape);
            }
            snappedShapes = new Dictionary<Shape, Shape>();
            setBackPage(newProperty);
        }

        protected void adjustSize(Shape snappingShape, Shape backgroundReferenceShape)
        {
            // Position the snappingShape exactly over the reference
            snappingShape.CellsU[ALPSConstants.shapeCellShapeTransformPinX].Formula =
                backgroundReferenceShape.CellsU[ALPSConstants.shapeCellShapeTransformPinX].Formula;
            snappingShape.CellsU[ALPSConstants.shapeCellShapeTransformPinY].Formula =
                backgroundReferenceShape.CellsU[ALPSConstants.shapeCellShapeTransformPinY].Formula;

            // Adjust boundaries
            double width = backgroundReferenceShape.CellsU[ALPSConstants.shapeCellShapeTransformWidth].Result[VisUnitCodes.visMillimeters] + 5;
            double height = backgroundReferenceShape.CellsU[ALPSConstants.shapeCellShapeTransformHeight].Result[VisUnitCodes.visMillimeters] + 5;
            snappingShape.CellsU[ALPSConstants.shapeCellShapeTransformWidth].Formula = width + " mm";
            snappingShape.CellsU[ALPSConstants.shapeCellShapeTransformHeight].Formula = height + " mm";
        }

        protected bool isLocatedCloselyInXDirection(Shape shape, Shape snapToShape, double snapRange)
        {
            double shapeX = shape.CellsU["PinX"].Result[VisUnitCodes.visMillimeters];

            double snapToShapeX = snapToShape.CellsU["PinX"].Result[VisUnitCodes.visMillimeters];

            return Math.Abs(shapeX - snapToShapeX) <= snapRange;
        }

        protected bool isLocatedClosely(Shape shape, Shape snapToShape)
        {
            double shapeX = shape.CellsU["PinX"].Result[VisUnitCodes.visMillimeters];
            double shapeY = shape.CellsU["PinY"].Result[VisUnitCodes.visMillimeters];

            double snapToShapeX = snapToShape.CellsU["PinX"].Result[VisUnitCodes.visMillimeters];
            double snapToShapeY = snapToShape.CellsU["PinY"].Result[VisUnitCodes.visMillimeters];

            return Math.Abs(shapeX - snapToShapeX) <= SNAP_RANGE && Math.Abs(shapeY - snapToShapeY) <= SNAP_RANGE;
        }

        public void notifyBackgroundShapeMoved(Shape snapToShape)
        {
            //if something is snapped to this snappingShape.
            if (!snappedShapes.Values.Contains(snapToShape)) return;
            Shape shape = snappedShapes.FirstOrDefault(x => x.Value == snapToShape).Key;
                adjustSize(shape, snapToShape);
        }


    }
}

