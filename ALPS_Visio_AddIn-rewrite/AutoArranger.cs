using System;
using System.Collections.Generic;
using System.Linq;
using Visio = Microsoft.Office.Interop.Visio;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;

namespace ALPS_Visio_AddIn_rewrite
{
    /// <summary>
    /// Re-arranges an already drawn SID or SBD page from the shapes alone — no parsed model
    /// required. The node graph is rebuilt from the connectors' glue, then the same layered
    /// layout used at import time is applied: states fall into columns by longest path from a
    /// root and each column is centered vertically; subjects line up in a row. Lets the user
    /// tidy a diagram they moved around by hand, or one opened from a saved file.
    /// </summary>
    public static class AutoArranger
    {
        // Layout constants in mm — kept in sync with the import-time layout for a consistent look.
        private const double StepX = 100.0, StepY = 50.0;
        private const double MarginX = 25.0, MarginY = 25.0;
        private const double ShapeWidth = 45.0, ShapeHeight = 30.0;
        private const double SubjectWidth = 32.0, SubjectSpacing = 55.0;

        // Visio shape categories that mark the layout nodes (from the ALPS/PASS stencils).
        private const string StateCategory = "alpsSBDstate";
        private static readonly string[] SubjectCategories =
            { "StandardActor", "InterfaceActor", "StandAloneMakro", "SubjectGroup", "SystemInterfaceSubject" };

        /// <summary>
        /// Arranges the application's active page if it is a SID or SBD page.
        /// The whole operation is one undo scope, so a single Ctrl+Z reverts it.
        /// </summary>
        public static void ArrangeActivePage(Visio.Application app)
        {
            Visio.Page page = app?.ActivePage;
            if (page == null) return;

            int scope = app.BeginUndoScope("Auto Arrange");
            bool committed = false;
            try
            {
                string pageType = ReadPageTypeFormula(page);
                if (pageType.Contains(Constants.Properties.SBDPage))
                {
                    ArrangeStates(page);
                    committed = true;
                }
                else if (pageType.Contains(Constants.Properties.SIDPage))
                {
                    ArrangeSubjects(page);
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
                app.EndUndoScope(scope, committed);
            }
        }

        /// <summary>
        /// SBD: rebuild the state graph from the connectors and apply the layered layout.
        /// </summary>
        private static void ArrangeStates(Visio.Page page)
        {
            IDictionary<string, Visio.Shape> nodes = CollectNodes(page, s => s.HasCategory(StateCategory));
            if (nodes.Count == 0) return;

            var adjacency = nodes.Keys.ToDictionary(id => id, id => new List<string>());
            BuildEdges(page, nodes, adjacency);

            // Roots = explicit start states, otherwise states without an incoming edge.
            var hasIncoming = new HashSet<string>(adjacency.Values.SelectMany(targets => targets));
            var roots = nodes.Keys.Where(id => IsStartState(nodes[id]) || !hasIncoming.Contains(id)).ToList();
            if (roots.Count == 0) roots.Add(nodes.Keys.First());

            var column = nodes.Keys.ToDictionary(id => id, id => -1);
            var onPath = new HashSet<string>();
            foreach (string root in roots) AssignColumns(root, 0, column, adjacency, onPath);
            foreach (string id in nodes.Keys.ToList())
                if (column[id] < 0) column[id] = 0;

            PlaceColumns(page, nodes, column);
        }

        /// <summary>
        /// SID: place the subject shapes in a horizontal row, centered vertically.
        /// </summary>
        private static void ArrangeSubjects(Visio.Page page)
        {
            var subjects = new List<Visio.Shape>();
            foreach (Visio.Shape shape in page.Shapes)
                if (Is2D(shape) && SubjectCategories.Any(category => shape.HasCategory(category)))
                    subjects.Add(shape);
            if (subjects.Count == 0) return;

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

        /// <summary>
        /// Grows the page to fit the column layout, then places each column centered on the
        /// page middle (identical geometry to the import-time SBD layout).
        /// </summary>
        private static void PlaceColumns(Visio.Page page, IDictionary<string, Visio.Shape> nodes, IDictionary<string, int> column)
        {
            var groups = nodes.Keys.GroupBy(id => column[id]).OrderBy(g => g.Key).ToList();
            int maxColumn = column.Values.Max();
            int maxRows = groups.Max(g => g.Count());

            double pageWidth = Math.Max(maxColumn * StepX + ShapeWidth + 2 * MarginX, PageDimension(page, true));
            double pageHeight = Math.Max((maxRows - 1) * StepY + ShapeHeight + 2 * MarginY, PageDimension(page, false));
            VH.SetCellMM(page.PageSheet, Constants.ShapeCells.PageWidth, pageWidth);
            VH.SetCellMM(page.PageSheet, Constants.ShapeCells.PageHeight, pageHeight);

            double midY = pageHeight / 2.0;
            double x0 = MarginX + ShapeWidth / 2.0;
            foreach (var group in groups)
            {
                double x = x0 + group.Key * StepX;
                var columnIds = group.ToList();
                double startY = midY + (columnIds.Count - 1) * StepY / 2.0;
                for (int i = 0; i < columnIds.Count; i++)
                {
                    Visio.Shape shape = nodes[columnIds[i]];
                    VH.SetCellMM(shape, Constants.ShapeCells.PinX, x);
                    VH.SetCellMM(shape, Constants.ShapeCells.PinY, startY - i * StepY);
                }
            }
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
        /// Longest-path column assignment via DFS; cycles are broken by the on-path set and a
        /// node is only revisited when a strictly longer path reaches it.
        /// </summary>
        private static void AssignColumns(string id, int col, IDictionary<string, int> column,
            IDictionary<string, List<string>> adjacency, HashSet<string> onPath)
        {
            if (onPath.Contains(id)) return;
            if (col <= column[id]) return;
            column[id] = col;

            onPath.Add(id);
            foreach (string target in adjacency[id])
                AssignColumns(target, col + 1, column, adjacency, onPath);
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
