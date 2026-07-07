using System;
using System.Collections.Generic;
using System.Linq;
using Visio = Microsoft.Office.Interop.Visio;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;

namespace ALPS_Visio_AddIn_rewrite
{
    /// <summary>
    /// Re-arranges an already drawn SID or SBD page from the shapes alone — no parsed model
    /// required. The node graph is rebuilt from the connectors' glue, then a layered layout is
    /// applied: states fall into layers by longest path from a root, each layer's states are
    /// spread perpendicular to the flow; subjects line up in a single line. Works in two
    /// directions so the user can pick the flow that fits the diagram.
    /// </summary>
    public static class AutoArranger
    {
        /// <summary>Direction the layout flows from layer to layer.</summary>
        public enum LayoutDirection
        {
            /// <summary>Layers go left → right; states within a layer stack vertically.</summary>
            LeftToRight,
            /// <summary>Layers go top → bottom; states within a layer spread horizontally.</summary>
            TopToBottom
        }

        // Spacing between shape centers, by axis (mm). Horizontal is wider because the shapes
        // and the transition-label boxes are wider than tall. Used for whichever role (layer
        // step or sibling step) maps onto that axis in the chosen direction.
        private const double StepX = 100.0, StepY = 55.0;
        private const double MarginX = 25.0, MarginY = 25.0;
        private const double ShapeWidth = 45.0, ShapeHeight = 30.0;

        // SID subject row/column metrics (mm).
        private const double SubjectWidth = 32.0, SubjectSpacing = 55.0;
        private const double SubjectHeight = 50.0, SubjectVSpacing = 30.0;

        // Visio shape categories that mark the layout nodes (from the ALPS/PASS stencils).
        private const string StateCategory = "alpsSBDstate";
        private static readonly string[] SubjectCategories =
            { "StandardActor", "InterfaceActor", "StandAloneMakro", "SubjectGroup", "SystemInterfaceSubject" };

        /// <summary>
        /// Arranges the application's active page if it is a SID or SBD page, flowing in the
        /// given direction. The whole operation is one undo scope, so a single Ctrl+Z reverts it.
        /// </summary>
        public static void ArrangeActivePage(Visio.Application app, LayoutDirection direction)
        {
            Visio.Page page = app?.ActivePage;
            if (page == null) return;

            int scope = app.BeginUndoScope("Auto Arrange");

            // Rendering und Neuberechnung waehrend des Umsortierens aussetzen — sonst
            // zeichnet Visio nach jedem einzelnen PinX/PinY-Set die Seite neu.
            short prevScreenUpdating = app.ScreenUpdating;
            short prevDeferRecalc = app.DeferRecalc;
            app.ScreenUpdating = 0;
            app.DeferRecalc = 1;

            bool committed = false;
            try
            {
                string pageType = ReadPageTypeFormula(page);
                if (pageType.Contains(Constants.Properties.SBDPage))
                {
                    ArrangeStates(page, direction);
                    committed = true;
                }
                else if (pageType.Contains(Constants.Properties.SIDPage))
                {
                    ArrangeSubjects(page, direction);
                    committed = true;
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        "Die aktive Seite ist kein SID- oder SBD-Diagramm.", "Auto Arrange");
                }
            }
            finally
            {
                app.DeferRecalc = prevDeferRecalc;
                app.ScreenUpdating = prevScreenUpdating;
                app.EndUndoScope(scope, committed);
            }
        }

        /// <summary>
        /// SBD: rebuild the state graph from the connectors and apply the layered layout.
        /// </summary>
        private static void ArrangeStates(Visio.Page page, LayoutDirection direction)
        {
            IDictionary<string, Visio.Shape> nodes = CollectNodes(page, s => s.HasCategory(StateCategory));
            if (nodes.Count == 0) return;

            var adjacency = nodes.Keys.ToDictionary(id => id, id => new List<string>());
            BuildEdges(page, nodes, adjacency);

            // Roots = explicit start states, otherwise states without an incoming edge.
            var hasIncoming = new HashSet<string>(adjacency.Values.SelectMany(targets => targets));
            var roots = nodes.Keys.Where(id => IsStartState(nodes[id]) || !hasIncoming.Contains(id)).ToList();
            if (roots.Count == 0) roots.Add(nodes.Keys.First());

            var layer = nodes.Keys.ToDictionary(id => id, id => -1);
            var onPath = new HashSet<string>();
            foreach (string root in roots) AssignLayers(root, 0, layer, adjacency, onPath);
            foreach (string id in nodes.Keys.ToList())
                if (layer[id] < 0) layer[id] = 0;

            PlaceLayers(page, nodes, layer, direction);
        }

        /// <summary>
        /// SID: place the subject shapes in a single line — a row (LeftToRight) or a column
        /// (TopToBottom) — centered on the page.
        /// </summary>
        private static void ArrangeSubjects(Visio.Page page, LayoutDirection direction)
        {
            var subjects = new List<Visio.Shape>();
            foreach (Visio.Shape shape in page.Shapes)
                if (Is2D(shape) && SubjectCategories.Any(category => shape.HasCategory(category)))
                    subjects.Add(shape);
            if (subjects.Count == 0) return;

            if (direction == LayoutDirection.LeftToRight)
            {
                double rowWidth = (subjects.Count - 1) * (SubjectWidth + SubjectSpacing);
                double pageWidth = Math.Max(rowWidth + SubjectWidth + 2 * MarginX, PageDimension(page, true));
                VH.SetCellMM(page.PageSheet, Constants.ShapeCells.PageWidth, pageWidth);

                double midY = PageDimension(page, false) / 2.0;
                double x = MarginX + SubjectWidth / 2.0;
                foreach (Visio.Shape shape in subjects)
                {
                    VH.SetCellMM(shape, Constants.ShapeCells.PinX, x);
                    VH.SetCellMM(shape, Constants.ShapeCells.PinY, midY);
                    x += SubjectWidth + SubjectSpacing;
                }
            }
            else
            {
                double colHeight = (subjects.Count - 1) * (SubjectHeight + SubjectVSpacing);
                double pageHeight = Math.Max(colHeight + SubjectHeight + 2 * MarginY, PageDimension(page, false));
                VH.SetCellMM(page.PageSheet, Constants.ShapeCells.PageHeight, pageHeight);

                double midX = PageDimension(page, true) / 2.0;
                double y = pageHeight - MarginY - SubjectHeight / 2.0; // start at the top, go down
                foreach (Visio.Shape shape in subjects)
                {
                    VH.SetCellMM(shape, Constants.ShapeCells.PinX, midX);
                    VH.SetCellMM(shape, Constants.ShapeCells.PinY, y);
                    y -= SubjectHeight + SubjectVSpacing;
                }
            }
        }

        /// <summary>
        /// Grows the page to fit the layered layout and places each layer's nodes spread
        /// perpendicular to the flow and centered. LeftToRight lays layers along X (siblings
        /// stacked along Y); TopToBottom lays layers along Y from the top (siblings along X).
        /// </summary>
        private static void PlaceLayers(Visio.Page page, IDictionary<string, Visio.Shape> nodes,
            IDictionary<string, int> layer, LayoutDirection direction)
        {
            var groups = nodes.Keys.GroupBy(id => layer[id]).OrderBy(g => g.Key).ToList();
            int maxLayer = layer.Values.Max();
            int maxSiblings = groups.Max(g => g.Count());

            if (direction == LayoutDirection.LeftToRight)
            {
                double pageWidth = Math.Max(maxLayer * StepX + ShapeWidth + 2 * MarginX, PageDimension(page, true));
                double pageHeight = Math.Max((maxSiblings - 1) * StepY + ShapeHeight + 2 * MarginY, PageDimension(page, false));
                VH.SetCellMM(page.PageSheet, Constants.ShapeCells.PageWidth, pageWidth);
                VH.SetCellMM(page.PageSheet, Constants.ShapeCells.PageHeight, pageHeight);

                double midY = pageHeight / 2.0;
                double x0 = MarginX + ShapeWidth / 2.0;
                foreach (var group in groups)
                {
                    double x = x0 + group.Key * StepX;
                    var ids = group.ToList();
                    double startY = midY + (ids.Count - 1) * StepY / 2.0;
                    for (int i = 0; i < ids.Count; i++)
                        SetPin(nodes[ids[i]], x, startY - i * StepY);
                }
            }
            else
            {
                double pageWidth = Math.Max((maxSiblings - 1) * StepX + ShapeWidth + 2 * MarginX, PageDimension(page, true));
                double pageHeight = Math.Max(maxLayer * StepY + ShapeHeight + 2 * MarginY, PageDimension(page, false));
                VH.SetCellMM(page.PageSheet, Constants.ShapeCells.PageWidth, pageWidth);
                VH.SetCellMM(page.PageSheet, Constants.ShapeCells.PageHeight, pageHeight);

                double midX = pageWidth / 2.0;
                double y0 = pageHeight - MarginY - ShapeHeight / 2.0; // top layer, go down
                foreach (var group in groups)
                {
                    double y = y0 - group.Key * StepY;
                    var ids = group.ToList();
                    double startX = midX - (ids.Count - 1) * StepX / 2.0;
                    for (int i = 0; i < ids.Count; i++)
                        SetPin(nodes[ids[i]], startX + i * StepX, y);
                }
            }
        }

        private static void SetPin(Visio.Shape shape, double x, double y)
        {
            VH.SetCellMM(shape, Constants.ShapeCells.PinX, x);
            VH.SetCellMM(shape, Constants.ShapeCells.PinY, y);
        }

        /// <summary>
        /// Collects the 2D node shapes (matching <paramref name="isNode"/>) keyed by their
        /// model component id.
        /// </summary>
        private static IDictionary<string, Visio.Shape> CollectNodes(Visio.Page page, Func<Visio.Shape, bool> isNode)
        {
            var nodes = new Dictionary<string, Visio.Shape>();
            foreach (Visio.Shape shape in page.Shapes)
            {
                if (!Is2D(shape) || !isNode(shape)) continue;
                string id = ReadId(shape);
                if (!string.IsNullOrEmpty(id) && !nodes.ContainsKey(id)) nodes[id] = shape;
            }
            return nodes;
        }

        /// <summary>
        /// Reconstructs directed edges from the 1D connector shapes: a connector's begin point
        /// is glued to the source node, its end point to the target node.
        /// </summary>
        private static void BuildEdges(Visio.Page page, IDictionary<string, Visio.Shape> nodes, IDictionary<string, List<string>> adjacency)
        {
            foreach (Visio.Shape shape in page.Shapes)
            {
                if (Is2D(shape)) continue; // only connectors

                string source = null, target = null;
                foreach (Visio.Connect connect in shape.Connects)
                {
                    string id = ReadId(connect.ToSheet);
                    if (id == null || !nodes.ContainsKey(id)) continue;
                    string fromCell = connect.FromCell.Name;
                    if (fromCell.StartsWith("Begin")) source = id;
                    else if (fromCell.StartsWith("End")) target = id;
                }
                if (source != null && target != null && source != target)
                    adjacency[source].Add(target);
            }
        }

        /// <summary>
        /// Longest-path layer assignment via DFS; cycles are broken by the on-path set and a
        /// node is only revisited when a strictly longer path reaches it.
        /// </summary>
        private static void AssignLayers(string id, int level, IDictionary<string, int> layer,
            IDictionary<string, List<string>> adjacency, HashSet<string> onPath)
        {
            if (onPath.Contains(id)) return;
            if (level <= layer[id]) return;
            layer[id] = level;

            onPath.Add(id);
            foreach (string target in adjacency[id])
                AssignLayers(target, level + 1, layer, adjacency, onPath);
            onPath.Remove(id);
        }

        private static bool Is2D(Visio.Shape shape) => shape.OneD == 0;

        private static string ReadId(Visio.Shape shape)
        {
            string cell = "Prop." + Constants.Properties.ID + ".Value";
            if (shape.CellExistsU[cell, 0] == 0) return null;
            return shape.CellsU[cell].ResultStr[""];
        }

        private static bool IsStartState(Visio.Shape shape)
        {
            string cell = "Prop." + Constants.Properties.State.Start + ".Value";
            if (shape.CellExistsU[cell, 0] == 0) return false;
            return shape.CellsU[cell].Result[""] != 0;
        }

        private static string ReadPageTypeFormula(Visio.Page page)
        {
            string cell = "Prop." + Constants.Properties.PageType;
            if (page.PageSheet.CellExistsU[cell, 0] == 0) return "";
            return page.PageSheet.CellsU[cell].Formula;
        }

        private static double PageDimension(Visio.Page page, bool width)
        {
            string cell = width ? Constants.ShapeCells.PageWidth : Constants.ShapeCells.PageHeight;
            return page.PageSheet.CellsU[cell].Result["mm"];
        }
    }
}
