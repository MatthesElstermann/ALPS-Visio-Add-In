using Microsoft.Office.Interop.Visio;
using System;
using System.Collections.Generic;
using System.Linq;
namespace ALPS_Visio_AddIn_rewrite
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
            else if (snappedShapes[snappingShape] != backgroundReferenceShape)
            {
                snappedShapes.Remove(snappingShape);
                snappedShapes.Add(snappingShape, backgroundReferenceShape);
            }

            adjustSize(snappingShape, backgroundReferenceShape);
        }

        /// <summary>
        /// checks for the given snappingShape if it should be snapping to a shape on the background page
        /// </summary>
        public void checkForSnapping(Shape snappingShape)
        {
            if (!isShapeSnappable(snappingShape)) return;

            List<Shape> snappableActorShapes = getSnappableShapesOnBackgroundPage().ToList();

            if (snappedShapes.ContainsKey(snappingShape) && !isLocatedClosely(snappingShape, snappedShapes[snappingShape]))
            {
                handleDistantSnappedShapes(snappingShape);
            }

            foreach (Shape possibleReferenceBackgroundShape in snappableActorShapes)
            {
                if (!isLocatedClosely(snappingShape, possibleReferenceBackgroundShape)) continue;

                // Don't pop the dialog again for a shape that is already snapped to this target.
                if (snappedShapes.TryGetValue(snappingShape, out Shape current)
                    && current.Name == possibleReferenceBackgroundShape.Name) continue;

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
        protected abstract void setBackPage(DiagramPage newProperty);

        /// <summary>
        /// sets the background page and resets all the snapped shapes.
        /// </summary>
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
            snappingShape.CellsU["PinX"].Formula = backgroundReferenceShape.CellsU["PinX"].Formula;
            snappingShape.CellsU["PinY"].Formula = backgroundReferenceShape.CellsU["PinY"].Formula;

            double width = backgroundReferenceShape.CellsU["Width"].Result[VisUnitCodes.visMillimeters] + 5;
            double height = backgroundReferenceShape.CellsU["Height"].Result[VisUnitCodes.visMillimeters] + 5;
            snappingShape.CellsU["Width"].Formula = width + " mm";
            snappingShape.CellsU["Height"].Formula = height + " mm";
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
            if (!snappedShapes.Values.Contains(snapToShape)) return;
            Shape shape = snappedShapes.FirstOrDefault(x => x.Value == snapToShape).Key;
            adjustSize(shape, snapToShape);
        }
    }
}
