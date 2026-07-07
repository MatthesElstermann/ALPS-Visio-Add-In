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
    /// ordered by the barycenter of their predecessors (crossing reduction) and spread
    /// perpendicular to the flow; subjects line up in a single line. The connectors are re-glued
    /// to flow-aligned points (e.g. bottom → top for Top-Down), because the import glues them
    /// for left-to-right flow only. Works in two directions so the user can pick the flow that
    /// fits the diagram.
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

        // Spacing between shape centers by role and direction (mm). The layer step must leave
        // room for the transition-label boxes that sit mid-connector: they are wider than tall,
        // so the horizontal layer step (LeftToRight) can stay tighter relative to the shape
        // size than the vertical one (TopToBottom).
        private const double LayerStepLR = 100.0, SiblingStepLR = 55.0;
        private const double LayerStepTB = 70.0, SiblingStepTB = 100.0;
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
            bool centerMessageBoxes = false;
            try
            {
                string pageType = ReadPageTypeFormula(page);
                if (pageType.Contains(Constants.Properties.SBDPage))
                {
                    // SBD-Label-Boxen folgen ihren Connectoren per Stencil-Formel —
                    // kein Nachzentrieren noetig.
                    ArrangeStates(page, direction);
                    committed = true;
                }
                else if (pageType.Contains(Constants.Properties.SIDPage))
                {
                    ArrangeSubjects(page, direction);
                    committed = true;
                    centerMessageBoxes = true;
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        "Die aktive Seite ist kein SID- oder SBD-Diagramm.", "Auto Arrange");
                }
            }
            finally
            {
                // Recalc ZUERST zurueck: die Center-Actions der Boxen lesen die Geometrie
                // ihrer Connectoren — mit aufgeschobenem Recalc waere die noch stale.
                app.DeferRecalc = prevDeferRecalc;
                try
                {
                    if (centerMessageBoxes) CenterMessageBoxes(page);
                }
                finally
                {
                    app.ScreenUpdating = prevScreenUpdating;
                    app.EndUndoScope(scope, committed);
                }
            }
        }

        /// <summary>
        /// Snaps the SID message boxes back onto their connectors after the subjects moved.
        /// Only the connector-companion containers are touched — they carry the
        /// <c>User.idOnPage</c> cell, the same marker the import uses to find them. The
        /// stencil's "Center" action moves ONLY the container itself, so the message shapes
        /// inside (container list members) are shifted by the same offset afterwards —
        /// otherwise they slip out of the box.
        /// </summary>
        private static void CenterMessageBoxes(Visio.Page page)
        {
            foreach (Visio.Shape shape in page.Shapes)
            {
                try
                {
                    if (shape.CellExistsU["User.idOnPage", 0] == 0) continue;
                    if (shape.CellExistsU["Actions.Center.Action", 0] == 0) continue;

                    // Durchgaengig in mm rechnen: Result[""] liefert interne Einheiten (Zoll),
                    // waehrend eine nackte Zahl im Formel-Set als Dokumenteinheit (mm) gelesen
                    // wird — dieser Mix hat die Mitglieder zuvor an falsche Koordinaten geschossen.
                    double beforeX = PinMM(shape, "PinX");
                    double beforeY = PinMM(shape, "PinY");
                    shape.CellsU["Actions.Center.Action"].Trigger();
                    double deltaX = PinMM(shape, "PinX") - beforeX;
                    double deltaY = PinMM(shape, "PinY") - beforeY;
                    if (Math.Abs(deltaX) < 0.01 && Math.Abs(deltaY) < 0.01) continue;

                    Visio.ContainerProperties container = shape.ContainerProperties;
                    if (container == null) continue;
                    foreach (object memberId in (System.Array)container.GetMemberShapes(
                        (int)Visio.VisContainerFlags.visContainerFlagsDefault))
                    {
                        try
                        {
                            Visio.Shape member = page.Shapes.ItemFromID[Convert.ToInt32(memberId)];
                            VH.SetCellMM(member, "PinX", PinMM(member, "PinX") + deltaX);
                            VH.SetCellMM(member, "PinY", PinMM(member, "PinY") + deltaY);
                        }
                        catch (System.Runtime.InteropServices.COMException e)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                "CenterMessageBoxes: Mitglied " + memberId + " nicht verschiebbar: " + e.Message);
                        }
                    }
                }
                catch (System.Runtime.InteropServices.COMException e)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "CenterMessageBoxes: Zentrieren von " + shape.NameU + " fehlgeschlagen: " + e.Message);
                }
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
            var edges = new List<(Visio.Shape connector, string source, string target)>();
            var danglers = new List<(Visio.Shape connector, string nodeId, bool gluedAtEnd)>();
            BuildEdges(page, nodes, adjacency, edges, danglers);

            // Roots = explicit start states, otherwise states without an incoming edge.
            var hasIncoming = new HashSet<string>(adjacency.Values.SelectMany(targets => targets));
            var roots = nodes.Keys.Where(id => IsStartState(nodes[id]) || !hasIncoming.Contains(id)).ToList();
            if (roots.Count == 0) roots.Add(nodes.Keys.First());

            var layer = nodes.Keys.ToDictionary(id => id, id => -1);
            var onPath = new HashSet<string>();
            foreach (string root in roots) AssignLayers(root, 0, layer, adjacency, onPath);
            foreach (string id in nodes.Keys.ToList())
                if (layer[id] < 0) layer[id] = 0;

            IDictionary<string, (double x, double y)> positions =
                PlaceLayers(page, nodes, layer, adjacency, direction, out double perpendicularMid);

            bool leftRight = direction == LayoutDirection.LeftToRight;
            IDictionary<string, double> perpendicularOffset = positions.Keys.ToDictionary(
                id => id,
                id => leftRight ? positions[id].y - perpendicularMid : positions[id].x - perpendicularMid);

            GlueEdgesToFlow(edges, nodes, id => layer[id], perpendicularOffset, direction);
            PositionDanglingMarkers(danglers, positions, direction);
        }

        /// <summary>
        /// Moves the free end of one-sided connectors (e.g. the start-state marker arrow, which
        /// is glued to its state at only one end) next to the state's new position. The glued
        /// end follows the state automatically, but the free end keeps its old ABSOLUTE page
        /// position — after re-arranging it points from somewhere random into the diagram.
        /// </summary>
        private static void PositionDanglingMarkers(
            List<(Visio.Shape connector, string nodeId, bool gluedAtEnd)> danglers,
            IDictionary<string, (double x, double y)> positions, LayoutDirection direction)
        {
            const double markerLength = 15.0; // mm freier Vorlauf vor/hinter dem Zustand

            foreach ((Visio.Shape connector, string nodeId, bool gluedAtEnd) in danglers)
            {
                try
                {
                    if (!positions.TryGetValue(nodeId, out (double x, double y) position)) continue;

                    // Ende am Zustand geklebt (Start-Marker) -> freies Ende GEGEN die
                    // Flussrichtung davor; Beginn am Zustand -> freies Ende dahinter.
                    double sign = gluedAtEnd ? 1.0 : -1.0;
                    double freeX = position.x, freeY = position.y;
                    if (direction == LayoutDirection.TopToBottom)
                        freeY = position.y + sign * (ShapeHeight / 2.0 + markerLength);
                    else
                        freeX = position.x - sign * (ShapeWidth / 2.0 + markerLength);

                    string freeEnd = gluedAtEnd ? "Begin" : "End";
                    VH.SetCellMM(connector, freeEnd + "X", freeX);
                    VH.SetCellMM(connector, freeEnd + "Y", freeY);
                }
                catch (System.Runtime.InteropServices.COMException e)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "PositionDanglingMarkers: " + connector.NameU + " nicht verschiebbar: " + e.Message);
                }
            }
        }

        /// <summary>
        /// SID: place the subject shapes in a single line — a row (LeftToRight) or a column
        /// (TopToBottom) — centered on the page, and re-glue the message connectors to match.
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

            // Message connectors are import-glued for left-to-right flow — re-glue them to the
            // chosen direction. The subject's position in the line acts as its rank.
            var subjectNodes = new Dictionary<string, Visio.Shape>();
            var rank = new Dictionary<string, int>();
            for (int i = 0; i < subjects.Count; i++)
            {
                string id = ReadId(subjects[i]);
                if (string.IsNullOrEmpty(id) || subjectNodes.ContainsKey(id)) continue;
                subjectNodes[id] = subjects[i];
                rank[id] = i;
            }
            var subjectAdjacency = subjectNodes.Keys.ToDictionary(id => id, id => new List<string>());
            var edges = new List<(Visio.Shape connector, string source, string target)>();
            BuildEdges(page, subjectNodes, subjectAdjacency, edges);
            // Subjekte stehen alle auf der Mittellinie — Spurwahl faellt auf die Kanten-Art zurueck.
            IDictionary<string, double> perpendicularOffset = subjectNodes.Keys.ToDictionary(id => id, id => 0.0);
            GlueEdgesToFlow(edges, subjectNodes, id => rank[id], perpendicularOffset, direction);
        }

        /// <summary>
        /// Grows the page to fit the layered layout and places each layer's nodes spread
        /// perpendicular to the flow and centered. LeftToRight lays layers along X (siblings
        /// stacked along Y); TopToBottom lays layers along Y from the top (siblings along X).
        /// </summary>
        /// <returns>Each node's placed center position (mm). <paramref name="perpendicularMid"/>
        /// is the layout's center line perpendicular to the flow (LR: midY, TB: midX) — both are
        /// used for the outer-lane choice and the dangling-marker placement.</returns>
        private static IDictionary<string, (double x, double y)> PlaceLayers(Visio.Page page,
            IDictionary<string, Visio.Shape> nodes, IDictionary<string, int> layer,
            IDictionary<string, List<string>> adjacency, LayoutDirection direction, out double perpendicularMid)
        {
            List<KeyValuePair<int, List<string>>> groups = BuildOrderedLayers(layer, adjacency);
            int maxLayer = layer.Values.Max();
            int maxSiblings = groups.Max(g => g.Value.Count);
            var positions = new Dictionary<string, (double x, double y)>();

            if (direction == LayoutDirection.LeftToRight)
            {
                double pageWidth = Math.Max(maxLayer * LayerStepLR + ShapeWidth + 2 * MarginX, PageDimension(page, true));
                double pageHeight = Math.Max((maxSiblings - 1) * SiblingStepLR + ShapeHeight + 2 * MarginY, PageDimension(page, false));
                VH.SetCellMM(page.PageSheet, Constants.ShapeCells.PageWidth, pageWidth);
                VH.SetCellMM(page.PageSheet, Constants.ShapeCells.PageHeight, pageHeight);

                double midY = pageHeight / 2.0;
                perpendicularMid = midY;
                double x0 = MarginX + ShapeWidth / 2.0;
                foreach (KeyValuePair<int, List<string>> group in groups)
                {
                    double x = x0 + group.Key * LayerStepLR;
                    List<string> ids = group.Value;
                    double startY = midY + (ids.Count - 1) * SiblingStepLR / 2.0;
                    for (int i = 0; i < ids.Count; i++)
                    {
                        double y = startY - i * SiblingStepLR;
                        SetPin(nodes[ids[i]], x, y);
                        positions[ids[i]] = (x, y);
                    }
                }
            }
            else
            {
                double pageWidth = Math.Max((maxSiblings - 1) * SiblingStepTB + ShapeWidth + 2 * MarginX, PageDimension(page, true));
                double pageHeight = Math.Max(maxLayer * LayerStepTB + ShapeHeight + 2 * MarginY, PageDimension(page, false));
                VH.SetCellMM(page.PageSheet, Constants.ShapeCells.PageWidth, pageWidth);
                VH.SetCellMM(page.PageSheet, Constants.ShapeCells.PageHeight, pageHeight);

                double midX = pageWidth / 2.0;
                perpendicularMid = midX;
                double y0 = pageHeight - MarginY - ShapeHeight / 2.0; // top layer, go down
                foreach (KeyValuePair<int, List<string>> group in groups)
                {
                    double y = y0 - group.Key * LayerStepTB;
                    List<string> ids = group.Value;
                    double startX = midX - (ids.Count - 1) * SiblingStepTB / 2.0;
                    for (int i = 0; i < ids.Count; i++)
                    {
                        double x = startX + i * SiblingStepTB;
                        SetPin(nodes[ids[i]], x, y);
                        positions[ids[i]] = (x, y);
                    }
                }
            }
            return positions;
        }

        /// <summary>
        /// Groups the nodes by layer and orders each layer's members by the barycenter (average
        /// position) of their predecessors in the previous layer — the classic crossing-reduction
        /// step. Nodes without predecessors keep their relative position.
        /// </summary>
        private static List<KeyValuePair<int, List<string>>> BuildOrderedLayers(
            IDictionary<string, int> layer, IDictionary<string, List<string>> adjacency)
        {
            List<KeyValuePair<int, List<string>>> layers = layer.Keys
                .GroupBy(id => layer[id])
                .OrderBy(g => g.Key)
                .Select(g => new KeyValuePair<int, List<string>>(g.Key, g.ToList()))
                .ToList();

            var predecessors = new Dictionary<string, List<string>>();
            foreach (KeyValuePair<string, List<string>> entry in adjacency)
                foreach (string target in entry.Value)
                {
                    if (!predecessors.TryGetValue(target, out List<string> list))
                        predecessors[target] = list = new List<string>();
                    list.Add(entry.Key);
                }

            for (int k = 1; k < layers.Count; k++)
            {
                var positionInPrevious = new Dictionary<string, int>();
                for (int i = 0; i < layers[k - 1].Value.Count; i++)
                    positionInPrevious[layers[k - 1].Value[i]] = i;

                List<string> current = layers[k].Value;
                var fallback = new Dictionary<string, double>();
                for (int i = 0; i < current.Count; i++) fallback[current[i]] = i;

                // OrderBy ist stabil — Knoten ohne Vorgaenger behalten ihre relative Lage.
                layers[k] = new KeyValuePair<int, List<string>>(layers[k].Key,
                    current.OrderBy(id => Barycenter(id, predecessors, positionInPrevious) ?? fallback[id]).ToList());
            }
            return layers;
        }

        /// <summary>Average position of the node's predecessors in the previous layer, if any.</summary>
        private static double? Barycenter(string id, IDictionary<string, List<string>> predecessors,
            IDictionary<string, int> positionInPrevious)
        {
            if (!predecessors.TryGetValue(id, out List<string> preds)) return null;
            List<double> positions = preds.Where(positionInPrevious.ContainsKey)
                .Select(p => (double)positionInPrevious[p]).ToList();
            if (positions.Count == 0) return null;
            return positions.Average();
        }

        /// <summary>
        /// Re-glues the connectors to flow-aligned points on their nodes. The import glues every
        /// connector for left-to-right flow (begin at the source's right-center, end at the
        /// target's left-center); for Top-Down that produces awkward sideways S-curves.
        /// Adjacent forward edges flow straight with the layout. All other edges (layer
        /// skippers, back and same-rank edges) take an OUTER lane — on the side where their
        /// most-displaced endpoint already sits, so the line runs around the diagram instead of
        /// cutting through it. For endpoints on the center line the edge kinds are separated:
        /// skippers go top/left, back edges go bottom/right.
        /// </summary>
        private static void GlueEdgesToFlow(List<(Visio.Shape connector, string source, string target)> edges,
            IDictionary<string, Visio.Shape> nodes, Func<string, int> rank,
            IDictionary<string, double> perpendicularOffset, LayoutDirection direction)
        {
            bool leftRight = direction == LayoutDirection.LeftToRight;

            foreach ((Visio.Shape connector, string source, string target) in edges)
            {
                int rankDelta = rank(target) - rank(source);
                try
                {
                    if (rankDelta == 1)
                    {
                        if (leftRight)
                        {
                            connector.CellsU["BeginX"].GlueToPos(nodes[source], 1.0, 0.5); // right-center
                            connector.CellsU["EndY"].GlueToPos(nodes[target], 0.0, 0.5);   // left-center
                        }
                        else
                        {
                            connector.CellsU["BeginX"].GlueToPos(nodes[source], 0.5, 0.0); // bottom-center
                            connector.CellsU["EndY"].GlueToPos(nodes[target], 0.5, 1.0);   // top-center
                        }
                        continue;
                    }

                    // Aussenspur: der staerker von der Mittellinie versetzte Endpunkt bestimmt
                    // die Seite (seine Kante laeuft dann aussen herum, nicht quer durchs Feld).
                    // Die Offsets stammen aus der Layout-Rechnung selbst — bewusst KEIN
                    // Result-Rueckgelese aus Visio, DeferRecalc ist hier noch aktiv.
                    double sourceOffset = perpendicularOffset[source];
                    double targetOffset = perpendicularOffset[target];
                    double offset = Math.Abs(sourceOffset) >= Math.Abs(targetOffset) ? sourceOffset : targetOffset;

                    bool positiveSide; // LR: obere Spur (y+), TB: rechte Spur (x+)
                    if (offset > 1.0) positiveSide = true;
                    else if (offset < -1.0) positiveSide = false;
                    else positiveSide = leftRight ? rankDelta > 1 : rankDelta <= 0;

                    double glueX = leftRight ? 0.5 : (positiveSide ? 1.0 : 0.0);
                    double glueY = leftRight ? (positiveSide ? 1.0 : 0.0) : 0.5;

                    connector.CellsU["BeginX"].GlueToPos(nodes[source], glueX, glueY);
                    connector.CellsU["EndY"].GlueToPos(nodes[target], glueX, glueY);
                }
                catch (System.Runtime.InteropServices.COMException e)
                {
                    // Ein nicht klebbarer Connector soll das Arrangieren nicht abbrechen.
                    System.Diagnostics.Debug.WriteLine(
                        "GlueEdgesToFlow: Umkleben von " + connector.NameU + " fehlgeschlagen: " + e.Message);
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
        /// is glued to the source node, its end point to the target node. Fills the adjacency
        /// map and the edge list (with the connector shape, for re-gluing). Connectors glued to
        /// exactly ONE node (e.g. the start-state marker arrow) are collected as danglers so
        /// their free end can be re-positioned after the layout.
        /// </summary>
        private static void BuildEdges(Visio.Page page, IDictionary<string, Visio.Shape> nodes,
            IDictionary<string, List<string>> adjacency,
            List<(Visio.Shape connector, string source, string target)> edges,
            List<(Visio.Shape connector, string nodeId, bool gluedAtEnd)> danglers = null)
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
                {
                    adjacency[source].Add(target);
                    edges.Add((shape, source, target));
                }
                else if (danglers != null && (source != null ^ target != null) && shape.Connects.Count == 1)
                {
                    // Nur echte Einseiter (genau EINE Klebung insgesamt): Connectoren, deren
                    // zweites Ende an einem Fremd-Shape ohne Modell-ID klebt, bleiben unberuehrt —
                    // ihnen das Ende umzusetzen wuerde die bestehende Klebung zerstoeren.
                    danglers.Add((shape, source ?? target, target != null));
                }
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

        /// <summary>Reads a shape cell in millimetres (matches the layout's mm-based math).</summary>
        private static double PinMM(Visio.Shape shape, string cell)
        {
            return shape.CellsU[cell].Result["mm"];
        }

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
