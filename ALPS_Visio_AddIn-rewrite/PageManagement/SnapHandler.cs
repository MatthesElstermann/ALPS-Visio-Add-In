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
        /// Shapes, fuer die gerade ein Trenn-Bestaetigungsdialog offen ist. Ein Move
        /// feuert onCellChanged zweimal (PinX und PinY) — ohne diesen Guard erschiene
        /// der (nicht-modale) Dialog deshalb doppelt.
        /// </summary>
        private readonly HashSet<Shape> shapesWithOpenMaintenanceDialog = new HashSet<Shape>();

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

            // Ein Verschieben feuert CellChanged fuer PinX UND PinY. Steht die Shape noch an der
            // zuletzt geprueften Position, wurde dieser Move schon behandelt — sonst erschiene
            // der Snap-Dialog zweimal.
            if (!isNewPosition(snappingShape)) return;

            removeDeletedShapes();
            List<Shape> snappableActorShapes = getSnappableShapesOnBackgroundPage().ToList();

            // Eine eingerastete Shape sitzt exakt auf ihrem Ziel (adjustSize kopiert dessen Pin).
            // Jede echte Verschiebung loest daher sofort die Trenn-Rueckfrage aus — frueher erst
            // ausserhalb des 20-mm-Fangbereichs, man musste die Shape also ein gutes Stueck wegziehen.
            if (snappedShapes.ContainsKey(snappingShape) && hasLeftTarget(snappingShape, snappedShapes[snappingShape]))
            {
                handleDistantSnappedShapes(snappingShape);
            }

            foreach (Shape possibleReferenceBackgroundShape in snappableActorShapes)
            {
                if (!isLocatedClosely(snappingShape, possibleReferenceBackgroundShape))
                {
                    // Aus der Naehe entfernt — beim naechsten Annaehern darf wieder gefragt werden.
                    declinedSnaps.Remove(snapKey(snappingShape, possibleReferenceBackgroundShape));
                    continue;
                }

                // Don't pop the dialog again for a shape that is already snapped to this target.
                if (snappedShapes.TryGetValue(snappingShape, out Shape current)
                    && current.Name == possibleReferenceBackgroundShape.Name) continue;

                // Bereits mit "Nein" beantwortet, solange die Shape in der Naehe bleibt.
                string key = snapKey(snappingShape, possibleReferenceBackgroundShape);
                if (declinedSnaps.Contains(key)) continue;

                WindowSnapConfirmation snapConf = new WindowSnapConfirmation(this, snappingShape, possibleReferenceBackgroundShape);
                UI.VisioOwner.Attach(snapConf);
                snapConf.ShowDialog();

                if (snapConf.SnapConfirmed) break;
                declinedSnaps.Add(key);
            }
        }

        /// <summary>Mit "Nein" beantwortete Paare (Shape → Snap-Ziel), siehe <see cref="checkForSnapping"/>.</summary>
        private readonly HashSet<string> declinedSnaps = new HashSet<string>();

        /// <summary>Zuletzt gepruefte Position je Shape (ID → PinX/PinY in mm).</summary>
        private readonly Dictionary<int, Tuple<double, double>> lastCheckedPositions = new Dictionary<int, Tuple<double, double>>();

        private static string snapKey(Shape shape, Shape target)
        {
            return shape.ID + "->" + target.ContainingPageID + "/" + target.ID;
        }

        private bool isNewPosition(Shape shape)
        {
            var position = Tuple.Create(shape.CellsU["PinX"].Result[VisUnitCodes.visMillimeters],
                shape.CellsU["PinY"].Result[VisUnitCodes.visMillimeters]);
            if (lastCheckedPositions.TryGetValue(shape.ID, out var last) && last.Equals(position)) return false;
            lastCheckedPositions[shape.ID] = position;
            return true;
        }

        /// <summary>
        /// Entfernt Eintraege, deren Shape oder Snap-Ziel inzwischen geloescht wurde. Der Zugriff
        /// auf ein geloeschtes Shape wirft eine COMException — ohne das Aufraeumen brach jede
        /// spaetere Snap-Pruefung fuer diese Shape ab.
        /// </summary>
        protected void removeDeletedShapes()
        {
            foreach (Shape shape in snappedShapes.Keys.ToList())
            {
                if (!isAlive(shape) || !isAlive(snappedShapes[shape]))
                    snappedShapes.Remove(shape);
            }
        }

        protected static bool isAlive(Shape shape)
        {
            if (shape == null) return false;
            try
            {
                return shape.ID > 0;
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                return false;
            }
        }

        protected abstract bool isShapeSnappable(IVShape shape);
        protected abstract void handleDistantSnappedShapes(Shape snappingShape);
        protected abstract IEnumerable<Shape> getSnappableShapesOnBackgroundPage();

        /// <summary>
        /// Fragt den Nutzer, ob die gesnappte Shape wirklich getrennt werden soll
        /// (Ja = Snap beibehalten und Position/Groesse nachziehen, Nein = trennen).
        /// Pro Shape ist maximal ein Dialog gleichzeitig offen.
        /// </summary>
        protected void showMaintenanceDialog(Shape snappingShape)
        {
            if (shapesWithOpenMaintenanceDialog.Contains(snappingShape)) return;
            shapesWithOpenMaintenanceDialog.Add(snappingShape);

            WindowSnapMaintenance snapMain = new WindowSnapMaintenance(this, snappingShape, snappedShapes[snappingShape]);
            UI.VisioOwner.Attach(snapMain);
            snapMain.Closed += (sender, args) => shapesWithOpenMaintenanceDialog.Remove(snappingShape);
            snapMain.Show();
        }

        /// <summary>
        /// Called from WindowSnapMaintenance (Ja-Button): der Snap bleibt bestehen,
        /// die Shape wird wieder auf ihr Snap-Ziel ausgerichtet.
        /// </summary>
        public virtual void maintainSnap(Shape shape, Shape snapToShape)
        {
            adjustSize(shape, snapToShape);
        }

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
            removeDeletedShapes();
            IList<Shape> listSnappedShapes = snappedShapes.Keys.ToList();
            foreach (Shape shape in listSnappedShapes)
            {
                // Eine fehlschlagende Shape darf den Wechsel der Hintergrundseite nicht abbrechen
                // (der Aufruf kommt u. a. ungeschuetzt aus dem Eigenschaften-Dialog).
                try { unsnap(shape); }
                catch (System.Runtime.InteropServices.COMException e)
                {
                    System.Diagnostics.Debug.WriteLine("[Snap] unsnap failed: " + e.Message);
                }
            }
            snappedShapes = new Dictionary<Shape, Shape>();
            declinedSnaps.Clear();
            setBackPage(newProperty);
        }

        protected void adjustSize(Shape snappingShape, Shape backgroundReferenceShape)
        {
            snappingShape.CellsU["PinX"].FormulaU = backgroundReferenceShape.CellsU["PinX"].FormulaU;
            snappingShape.CellsU["PinY"].FormulaU = backgroundReferenceShape.CellsU["PinY"].FormulaU;

            // Kulturunabhaengig schreiben (FormulaU + InvariantCulture) — "12,5 mm" vs. "12.5 mm".
            double width = backgroundReferenceShape.CellsU["Width"].Result[VisUnitCodes.visMillimeters] + 5;
            double height = backgroundReferenceShape.CellsU["Height"].Result[VisUnitCodes.visMillimeters] + 5;
            VisioHelper.SetCellMM(snappingShape, "Width", width);
            VisioHelper.SetCellMM(snappingShape, "Height", height);
        }

        /// <summary>Toleranz fuer "liegt noch auf dem Ziel" in mm (Rundung der Pin-Werte).</summary>
        private const double SNAPPED_TOLERANCE_MM = 0.5;

        /// <summary>True, sobald eine eingerastete Shape von der Pin-Position ihres Ziels abweicht.</summary>
        private static bool hasLeftTarget(Shape shape, Shape snapToShape)
        {
            double dx = shape.CellsU["PinX"].Result[VisUnitCodes.visMillimeters] - snapToShape.CellsU["PinX"].Result[VisUnitCodes.visMillimeters];
            double dy = shape.CellsU["PinY"].Result[VisUnitCodes.visMillimeters] - snapToShape.CellsU["PinY"].Result[VisUnitCodes.visMillimeters];
            return Math.Abs(dx) > SNAPPED_TOLERANCE_MM || Math.Abs(dy) > SNAPPED_TOLERANCE_MM;
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
