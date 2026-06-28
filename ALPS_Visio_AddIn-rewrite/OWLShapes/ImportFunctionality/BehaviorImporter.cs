using System.Collections.Generic;
using System.Linq;
using alps.net.api.ALPS;
using alps.net.api.StandardPASS;
using Visio = Microsoft.Office.Interop.Visio;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    /// <summary>
    /// Draws the states and transitions of a subject behaviour (SBD / GBD) onto a Visio page and,
    /// when the OWL model carries no coordinates, lays them out in left-to-right layers.
    /// Shared by every behaviour kind — base subject behaviour, extension behaviour and guard
    /// behaviour — so they all render identically. Extracted from <see cref="VisioSubjectBehavior"/>.
    /// </summary>
    public static class BehaviorImporter
    {
        // Layout constants in mm. StepX leaves room for the (wide) transition label boxes
        // between columns; StepY separates stacked branch states. ShapeWidth/Height are
        // rough estimates used only to size the page so nothing spills past its edge.
        private const double LayoutMarginX = 25.0;
        private const double LayoutMarginY = 25.0;
        private const double LayoutStepX = 100.0;
        private const double LayoutStepY = 50.0;
        private const double LayoutShapeWidth = 45.0;
        private const double LayoutShapeHeight = 30.0;

        /// <summary>
        /// Imports the given behaviour-describing components (states + transitions) onto the page.
        /// States are drawn first so transition connectors can glue to existing shapes.
        /// </summary>
        public static void Draw(IDictionary<string, IBehaviorDescribingComponent> components, Visio.Page page)
        {
            bool anyHadCoordinates = false;
            var importedStates = new List<IState>();

            foreach (IBehaviorDescribingComponent component in components.Values.OrderBy(c => c is ITransition))
            {
                if (!(component is IVisioImportable importable)) continue;

                if (importable is IVisioImportableWithShape shapeImportable)
                    if (shapeImportable.PrepareDimensions()) anyHadCoordinates = true;

                if (component is IState state)
                {
                    importable.ImportToVisio(page);
                    importedStates.Add(state);
                }
                else if (component is ITransition)
                {
                    importable.ImportToVisio(page);
                }
            }

            if (!anyHadCoordinates && importedStates.Count > 0)
                ApplyLayeredLayout(importedStates, page);
        }

        /// <summary>
        /// Positions state shapes in left-to-right layers when the OWL file has no coordinates.
        /// A state's column is its longest path from a root state, so it always sits to the
        /// right of every predecessor — merge points (e.g. an end state reached from two
        /// branches) stay on the right instead of snapping onto the first branch. Within a
        /// column the states are stacked and centered vertically, giving branches that fan
        /// out symmetrically around the page middle.
        /// </summary>
        private static void ApplyLayeredLayout(IList<IState> states, Visio.Page page)
        {
            // 1. Column = longest path from a root state (-1 = not yet reached).
            var column = new Dictionary<string, int>();
            foreach (IState s in states) column[s.getModelComponentID()] = -1;

            // The initial state is a root even when a loop-back transition gives it an
            // incoming edge; otherwise fall back to states with no incoming transitions.
            var roots = states.Where(s => s.isStateType(IState.StateType.InitialStateOfBehavior)
                                          || s.getIncomingTransitions().Count == 0).ToList();
            if (roots.Count == 0) roots.Add(states[0]);

            var onPath = new HashSet<string>();
            foreach (IState root in roots)
                AssignColumns(root, 0, column, onPath);

            // States only reachable through a loop body keep -1 — place them in column 0.
            foreach (IState s in states)
                if (column[s.getModelComponentID()] < 0) column[s.getModelComponentID()] = 0;

            var columnGroups = states.GroupBy(s => column[s.getModelComponentID()])
                                     .OrderBy(g => g.Key).ToList();

            // 2. Grow the page to fit the layout (so nothing spills into the off-page area).
            //    Result["mm"] gives the value in mm regardless of the document's unit setting.
            int maxColumn = column.Values.Max();
            int maxRows = columnGroups.Max(g => g.Count());
            double pageWidth = maxColumn * LayoutStepX + LayoutShapeWidth + 2 * LayoutMarginX;
            double pageHeight = (maxRows - 1) * LayoutStepY + LayoutShapeHeight + 2 * LayoutMarginY;
            pageWidth = System.Math.Max(pageWidth, page.PageSheet.CellsU[Constants.ShapeCells.PageWidth].Result["mm"]);
            pageHeight = System.Math.Max(pageHeight, page.PageSheet.CellsU[Constants.ShapeCells.PageHeight].Result["mm"]);
            VH.SetCellMM(page.PageSheet, Constants.ShapeCells.PageWidth, pageWidth);
            VH.SetCellMM(page.PageSheet, Constants.ShapeCells.PageHeight, pageHeight);

            // 3. Place each column, stacking its states centered on the page middle.
            double midY = pageHeight / 2.0;
            double x0 = LayoutMarginX + LayoutShapeWidth / 2.0;
            foreach (var columnGroup in columnGroups)
            {
                double x = x0 + columnGroup.Key * LayoutStepX;
                var columnStates = columnGroup.ToList();
                double startY = midY + (columnStates.Count - 1) * LayoutStepY / 2.0;

                for (int i = 0; i < columnStates.Count; i++)
                {
                    if (!(columnStates[i] is IVisioImportableWithShape importable)) continue;
                    Visio.Shape shape = importable.GetShape();
                    if (shape == null) continue;
                    VH.SetCellMM(shape, Constants.ShapeCells.PinX, x);
                    VH.SetCellMM(shape, Constants.ShapeCells.PinY, startY - i * LayoutStepY);
                }
            }
        }

        /// <summary>
        /// Longest-path column assignment via DFS. Cycles (loop-back transitions) are broken
        /// by the on-path set so a back edge does not push states into ever-growing columns;
        /// a node is only re-visited when a strictly longer path reaches it.
        /// </summary>
        private static void AssignColumns(IState state, int col, IDictionary<string, int> column, HashSet<string> onPath)
        {
            string id = state.getModelComponentID();
            if (onPath.Contains(id)) return;   // back edge — ignore for layering
            if (col <= column[id]) return;     // an equal-or-longer path already processed this node
            column[id] = col;

            onPath.Add(id);
            foreach (ITransition transition in state.getOutgoingTransitions().Values)
            {
                IState target = transition.getTargetState();
                if (target != null && column.ContainsKey(target.getModelComponentID()))
                    AssignColumns(target, col + 1, column, onPath);
            }
            onPath.Remove(id);
        }
    }
}
