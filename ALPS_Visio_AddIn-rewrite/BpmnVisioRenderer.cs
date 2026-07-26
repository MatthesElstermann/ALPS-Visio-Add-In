using System;
using System.Collections.Generic;
using System.Linq;
using PassBpmnConverter.Bpmn;
using PassBpmnConverter.Bpmn.BpmnDI;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite
{
    /// <summary>
    /// Zeichnet ein konvertiertes BPMN-Modell (inkl. des vom
    /// <see cref="PassBpmnConverter.Bpmn.BpmnDiagramGenerator"/> erzeugten BPMN-DI-Layouts)
    /// auf ein neues Zeichenblatt des aktiven Visio-Dokuments — mit den in Visio
    /// eingebauten BPMN-Shapes (Schablone „BPMN Basic Shapes“, Visio Professional/
    /// Plan 2). Pools werden als Rechteck-Container mit vertikalem Namens-Streifen
    /// gezeichnet (robuster als das CFF-basierte Pool/Lane-Shape); Flow-Elemente
    /// kommen als BPMN-Master, Verbinder werden dynamisch geklebt und von Visio
    /// geroutet (die DI-Wegpunkte werden bewusst ignoriert). Element-Untertypen
    /// (Task-Typ, Event-Trigger, Gateway-Typ) werden best effort ueber die
    /// Shape-Data-Listen der Master gesetzt — Zellnamen/Wertelisten variieren je
    /// Visio-Version/Sprache, deshalb tolerant per Format-Listen-Abgleich.
    /// </summary>
    public static class BpmnVisioRenderer
    {
        /// <summary>BPMN-DI-Konvention (bpmn.io, Camunda): 96 Pixel je Zoll.</summary>
        private const double PixelsPerInch = 96.0;
        private const double PageMarginInch = 0.4;
        private const double PoolLabelStripInch = 0.35;

        /// <summary>Dateinamen der eingebauten Visio-BPMN-Schablone (metrisch/US).</summary>
        private static readonly string[] BpmnStencilFiles = { "BPMNBASI_M.VSSX", "BPMNBASI_U.VSSX" };

        /// <summary>
        /// Rendert das BPMN-Modell auf ein neues Zeichenblatt „BPMN: &lt;Modellname&gt;“
        /// im aktiven Dokument und liefert Warnungen (fehlende Master, nicht
        /// aufloesbare Verbinder-Enden) zurueck.
        /// </summary>
        public static IList<string> Render(Visio.Application app, IBpmnModel bpmnModel, string modelName)
        {
            var warnings = new List<string>();

            IBpmnPlane plane = bpmnModel == null || bpmnModel.Definitions == null
                ? null
                : bpmnModel.Definitions.Diagrams.Select(d => d.BpmnPlane).FirstOrDefault(p => p != null);
            if (plane == null || plane.DiagramElements.Count == 0)
                throw new InvalidOperationException(
                    "Das BPMN-Modell enthält kein Diagramm-Layout (BPMN DI) — nichts zu zeichnen.");

            List<IBpmnShape> diShapes = plane.DiagramElements.OfType<IBpmnShape>()
                .Where(s => s.Bounds != null && s.BpmnElement != null).ToList();
            List<IBpmnEdge> diEdges = plane.DiagramElements.OfType<IBpmnEdge>()
                .Where(e => e.BpmnElement != null).ToList();
            if (diShapes.Count == 0)
                throw new InvalidOperationException("Das BPMN-Diagramm enthält keine platzierbaren Elemente.");

            // BPMN-DI misst von links OBEN, Visio von links UNTEN — fuer den Flip wird
            // die Gesamthoehe des Layouts gebraucht.
            double maxYPx = diShapes.Max(s => s.Bounds.Y + s.Bounds.Height);
            double maxXPx = diShapes.Max(s => s.Bounds.X + s.Bounds.Width);

            Visio.Document stencil = OpenBpmnStencil(app);
            Visio.Document targetDoc = app.ActiveDocument;
            Visio.Page page = AddBpmnPage(targetDoc,
                "BPMN: " + (string.IsNullOrWhiteSpace(modelName) ? "Modell" : modelName));

            short prevScreenUpdating = app.ScreenUpdating;
            app.ScreenUpdating = 0;
            try
            {
                page.PageSheet.CellsU["PageWidth"].ResultIU = maxXPx / PixelsPerInch + 2 * PageMarginInch;
                page.PageSheet.CellsU["PageHeight"].ResultIU = maxYPx / PixelsPerInch + 2 * PageMarginInch;

                var shapeByElementId = new Dictionary<string, Visio.Shape>();
                var masterCache = new Dictionary<string, Visio.Master>();

                // 1. Pools zuerst — sie liegen hinter allen Flow-Elementen.
                foreach (IBpmnShape di in diShapes)
                {
                    if (di.BpmnElement is IParticipant participant)
                        DrawPool(page, di, participant, maxYPx, shapeByElementId);
                }

                // 2. Flow-Elemente (Tasks, Events, Gateways, Sub-Prozesse).
                foreach (IBpmnShape di in diShapes)
                {
                    if (!(di.BpmnElement is IParticipant))
                        DropFlowNode(page, stencil, masterCache, di, maxYPx, shapeByElementId, warnings);
                }

                // 3. Verbinder (Sequenz- und Nachrichtenfluesse).
                foreach (IBpmnEdge diEdge in diEdges)
                    DropConnector(app, page, stencil, masterCache, diEdge, shapeByElementId, warnings);

                if (app.ActiveWindow != null)
                    app.ActiveWindow.Page = page;
            }
            finally
            {
                app.ScreenUpdating = prevScreenUpdating;
            }

            return warnings;
        }

        /// <summary>
        /// Oeffnet die eingebaute BPMN-Schablone. Drei Stufen: (1) eine bereits
        /// geoeffnete BPMN-Schablone wiederverwenden, (2) den Visio-Content-Ordner
        /// ueber <c>GetBuiltInStencilFile</c> ermitteln (OpenEx sucht bei blossen
        /// Dateinamen NICHT dort) und die BPMN-Datei per Wildcard finden,
        /// (3) zuletzt die bekannten Dateinamen direkt probieren.
        /// </summary>
        private static Visio.Document OpenBpmnStencil(Visio.Application app)
        {
            // (1) Bereits geoeffnete BPMN-Schablone (z. B. von Hand geoeffnet).
            foreach (Visio.Document open in app.Documents)
            {
                try
                {
                    if ((int)open.Type == (int)Visio.VisDocumentTypes.visTypeStencil
                        && open.Name.StartsWith("BPMN", StringComparison.OrdinalIgnoreCase))
                        return open;
                }
                catch (System.Runtime.InteropServices.COMException) { }
            }

            // (2) Content-Ordner ueber ein garantiert vorhandenes eingebautes Stencil
            //     bestimmen — der zurueckgegebene Pfad zeigt in "…\Visio Content\<LCID>\",
            //     wo auch die BPMN-Schablone liegt.
            string contentDir = null;
            foreach (Visio.VisMeasurementSystem measurement in new[]
                { Visio.VisMeasurementSystem.visMSMetric, Visio.VisMeasurementSystem.visMSUS })
            {
                try
                {
                    string builtIn = app.GetBuiltInStencilFile(
                        Visio.VisBuiltInStencilTypes.visBuiltInStencilContainers, measurement);
                    if (!string.IsNullOrEmpty(builtIn))
                    {
                        contentDir = System.IO.Path.GetDirectoryName(builtIn);
                        break;
                    }
                }
                catch (System.Runtime.InteropServices.COMException) { }
            }

            if (contentDir != null && System.IO.Directory.Exists(contentDir))
            {
                // "BASI"-Schablone (BPMN Basic Shapes) bevorzugen, metrisch (_M) vor US (_U).
                IEnumerable<string> candidates = System.IO.Directory.GetFiles(contentDir, "BPMN*.vssx")
                    .OrderByDescending(p => System.IO.Path.GetFileName(p)
                        .IndexOf("BASI", StringComparison.OrdinalIgnoreCase) >= 0)
                    .ThenByDescending(p => p.EndsWith("_M.vssx", StringComparison.OrdinalIgnoreCase));
                foreach (string path in candidates)
                {
                    try
                    {
                        return app.Documents.OpenEx(path, (short)Visio.VisOpenSaveArgs.visOpenDocked);
                    }
                    catch (System.Runtime.InteropServices.COMException) { }
                }
            }

            // (3) Blosse Dateinamen (greift, wenn der Content-Ordner im Suchpfad liegt).
            foreach (string fileName in BpmnStencilFiles)
            {
                try
                {
                    return app.Documents.OpenEx(fileName, (short)Visio.VisOpenSaveArgs.visOpenDocked);
                }
                catch (System.Runtime.InteropServices.COMException) { }
            }

            throw new InvalidOperationException(
                "Die eingebaute BPMN-Schablone (BPMN Basic Shapes) wurde nicht gefunden. "
                + "BPMN-Shapes sind nur in Visio Professional bzw. Visio Plan 2 enthalten. "
                + "Workaround: die Schablone einmal manuell öffnen (Shapes-Fenster → Weitere Shapes "
                + "→ Geschäftsprozess → „BPMN-Standardformen“) und die Aktion wiederholen."
                + (contentDir != null ? "\nDurchsuchter Content-Ordner: " + contentDir : ""));
        }

        /// <summary>Legt das Ziel-Zeichenblatt mit dokumentweit eindeutigem Namen an (NameU = Name, siehe Seiten-Gotchas).</summary>
        private static Visio.Page AddBpmnPage(Visio.Document doc, string desiredName)
        {
            var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Visio.Page p in doc.Pages)
                existing.Add(p.Name);

            string name = desiredName;
            for (int suffix = 2; existing.Contains(name); suffix++)
                name = desiredName + " (" + suffix + ")";

            Visio.Page page = doc.Pages.Add();
            page.Name = name;
            page.NameU = name;
            return page;
        }

        private static double ToVisioX(double xPx)
        {
            return xPx / PixelsPerInch + PageMarginInch;
        }

        private static double ToVisioY(double yPxTopBased, double maxYPx)
        {
            return (maxYPx - yPxTopBased) / PixelsPerInch + PageMarginInch;
        }

        /// <summary>
        /// Zeichnet einen Participant als Pool: transparentes Rechteck ueber die
        /// DI-Bounds plus schmaler Namens-Streifen mit um 90 Grad gedrehtem Text am
        /// linken Rand. Bewusst kein Drop des „Pool / Lane“-Masters — dessen
        /// CFF-Container-Logik (Lanes, List-Verhalten) ist fuer programmatisches
        /// Platzieren fragil; das Rechteck traegt trotzdem die BPMN-Optik.
        /// </summary>
        private static void DrawPool(Visio.Page page, IBpmnShape di, IParticipant participant,
            double maxYPx, Dictionary<string, Visio.Shape> shapeByElementId)
        {
            double x1 = ToVisioX(di.Bounds.X);
            double x2 = ToVisioX(di.Bounds.X + di.Bounds.Width);
            double y1 = ToVisioY(di.Bounds.Y + di.Bounds.Height, maxYPx);
            double y2 = ToVisioY(di.Bounds.Y, maxYPx);

            Visio.Shape pool = page.DrawRectangle(x1, y1, x2, y2);
            pool.CellsU["FillPattern"].ResultIU = 0;

            Visio.Shape label = page.DrawRectangle(x1, y1, x1 + PoolLabelStripInch, y2);
            label.CellsU["FillPattern"].ResultIU = 0;
            label.Text = participant.Name ?? "";
            // Textblock um 90 Grad drehen und auf die (gedrehte) Streifen-Flaeche legen.
            label.CellsU["TxtAngle"].FormulaU = "90 deg";
            label.CellsU["TxtWidth"].FormulaU = "Height*1";
            label.CellsU["TxtHeight"].FormulaU = "Width*1";
            label.CellsU["TxtPinX"].FormulaU = "Width*0.5";
            label.CellsU["TxtPinY"].FormulaU = "Height*0.5";
            label.CellsU["TxtLocPinX"].FormulaU = "TxtWidth*0.5";
            label.CellsU["TxtLocPinY"].FormulaU = "TxtHeight*0.5";

            pool.SendToBack();
            label.SendToBack();

            if (!string.IsNullOrEmpty(participant.Id))
                shapeByElementId[participant.Id] = pool;
        }

        /// <summary>Platziert ein Flow-Element als BPMN-Master an seiner DI-Position.</summary>
        private static void DropFlowNode(Visio.Page page, Visio.Document stencil,
            Dictionary<string, Visio.Master> masterCache, IBpmnShape di, double maxYPx,
            Dictionary<string, Visio.Shape> shapeByElementId, List<string> warnings)
        {
            IBaseElement element = di.BpmnElement;
            string[] masterCandidates;
            string[] typePropRows = null;
            string[] typeKeywords = null;

            if (element is IStartEvent)
            {
                masterCandidates = new[] { "Start Event" };
                typePropRows = EventTriggerRows;
                typeKeywords = TriggerKeywordsFor(element);
            }
            else if (element is IEndEvent)
            {
                masterCandidates = new[] { "End Event" };
                typePropRows = EventTriggerRows;
                typeKeywords = TriggerKeywordsFor(element);
            }
            else if (element is IBoundaryEvent || element is IIntermediateCatchEvent || element is IIntermediateThrowEvent)
            {
                masterCandidates = new[] { "Intermediate Event" };
                typePropRows = EventTriggerRows;
                typeKeywords = TriggerKeywordsFor(element);
            }
            else if (element is IEventBasedGateway)
            {
                masterCandidates = new[] { "Gateway" };
                typePropRows = GatewayTypeRows;
                typeKeywords = new[] { "event", "ereignis" };
            }
            else if (element is IParallelGateway)
            {
                masterCandidates = new[] { "Gateway" };
                typePropRows = GatewayTypeRows;
                typeKeywords = new[] { "parallel" };
            }
            else if (element is IInclusiveGateway)
            {
                masterCandidates = new[] { "Gateway" };
                typePropRows = GatewayTypeRows;
                typeKeywords = new[] { "inclusive", "inklusiv" };
            }
            else if (element is IComplexGateway)
            {
                masterCandidates = new[] { "Gateway" };
                typePropRows = GatewayTypeRows;
                typeKeywords = new[] { "complex", "komplex" };
            }
            else if (element is IExclusiveGateway)
            {
                masterCandidates = new[] { "Gateway" };
                typePropRows = GatewayTypeRows;
                typeKeywords = new[] { "exclusive", "exklusiv", "xor" };
            }
            else if (element is ISubProcess)
            {
                masterCandidates = new[] { "Sub-Process", "Sub-process", "Subprocess", "Task" };
            }
            else if (element is IReceiveTask)
            {
                masterCandidates = new[] { "Task" };
                typePropRows = TaskTypeRows;
                typeKeywords = new[] { "receive", "empfang" };
            }
            else if (element is ISendTask)
            {
                masterCandidates = new[] { "Task" };
                typePropRows = TaskTypeRows;
                typeKeywords = new[] { "send", "sende" };
            }
            else if (element is IScriptTask)
            {
                masterCandidates = new[] { "Task" };
                typePropRows = TaskTypeRows;
                typeKeywords = new[] { "script", "skript" };
            }
            else if (element is ICallActivity)
            {
                masterCandidates = new[] { "Call Activity", "Task" };
            }
            else if (element is PassBpmnConverter.Bpmn.ITask)
            {
                masterCandidates = new[] { "Task" };
            }
            else
            {
                warnings.Add("BPMN-Element „" + (element.Id ?? "?") + "“ (" + element.GetType().Name
                    + ") wird von der Visio-Anzeige nicht unterstützt und wurde ausgelassen.");
                return;
            }

            Visio.Master master = FindMaster(stencil, masterCache, masterCandidates);
            if (master == null)
            {
                warnings.Add("Master „" + masterCandidates[0] + "“ fehlt in der BPMN-Schablone — Element „"
                    + DescribeElement(element) + "“ wurde ausgelassen.");
                return;
            }

            double pinX = ToVisioX(di.Bounds.X + di.Bounds.Width / 2);
            double pinY = ToVisioY(di.Bounds.Y + di.Bounds.Height / 2, maxYPx);
            Visio.Shape shape = page.Drop(master, pinX, pinY);

            // DI-Groesse uebernehmen; GUARD-geschuetzte Zellen (fixe Event-/Gateway-
            // Groessen) behalten dabei einfach ihren Standard.
            try
            {
                shape.CellsU["Width"].ResultIU = di.Bounds.Width / PixelsPerInch;
                shape.CellsU["Height"].ResultIU = di.Bounds.Height / PixelsPerInch;
            }
            catch (System.Runtime.InteropServices.COMException) { }

            var flowElement = element as IFlowElement;
            if (flowElement != null && !string.IsNullOrEmpty(flowElement.Name))
                shape.Text = flowElement.Name;

            if (typePropRows != null && typeKeywords != null)
                TrySetListProp(shape, typePropRows, typeKeywords);

            if (!string.IsNullOrEmpty(element.Id))
                shapeByElementId[element.Id] = shape;
        }

        /// <summary>
        /// Platziert einen Sequenz- bzw. Nachrichtenfluss und klebt beide Enden
        /// dynamisch an die zugehoerigen Shapes (Visio uebernimmt das Routing; die
        /// DI-Wegpunkte werden ignoriert). Fallback ohne passenden Master: ein
        /// Dynamischer Verbinder, bei Nachrichtenfluessen gestrichelt.
        /// </summary>
        private static void DropConnector(Visio.Application app, Visio.Page page, Visio.Document stencil,
            Dictionary<string, Visio.Master> masterCache, IBpmnEdge diEdge,
            Dictionary<string, Visio.Shape> shapeByElementId, List<string> warnings)
        {
            string sourceId, targetId, label;
            string[] masterCandidates;
            bool isMessageFlow;

            if (diEdge.BpmnElement is ISequenceFlow sequenceFlow)
            {
                sourceId = sequenceFlow.SourceRef == null ? null : sequenceFlow.SourceRef.Id;
                targetId = sequenceFlow.TargetRef == null ? null : sequenceFlow.TargetRef.Id;
                label = sequenceFlow.Name;
                masterCandidates = new[] { "Sequence Flow" };
                isMessageFlow = false;
            }
            else if (diEdge.BpmnElement is IMessageFlow messageFlow)
            {
                // IInteractionNode ist ein Marker-Interface ohne Id; alle konkreten
                // Knoten (Participants, Tasks, Events) sind aber IBaseElement.
                sourceId = (messageFlow.SourceRef as IBaseElement)?.Id;
                targetId = (messageFlow.TargetRef as IBaseElement)?.Id;
                label = messageFlow.Name;
                masterCandidates = new[] { "Message Flow" };
                isMessageFlow = true;
            }
            else
            {
                return; // andere Kanten (Associations) erzeugt der Konverter nicht
            }

            Visio.Shape source, target;
            if (sourceId == null || targetId == null
                || !shapeByElementId.TryGetValue(sourceId, out source)
                || !shapeByElementId.TryGetValue(targetId, out target))
            {
                warnings.Add("Verbinder „" + (label ?? diEdge.BpmnElement.Id ?? "?")
                    + "“ konnte nicht gezeichnet werden (Quelle oder Ziel fehlt auf dem Blatt).");
                return;
            }

            Visio.Master master = FindMaster(stencil, masterCache, masterCandidates);
            Visio.Shape connector;
            if (master != null)
            {
                connector = page.Drop(master, 0, 0);
            }
            else
            {
                // Dynamischer Standard-Verbinder als Ersatz.
                connector = page.Drop(app.ConnectorToolDataObject, 0, 0);
                if (isMessageFlow)
                    connector.CellsU["LinePattern"].ResultIU = 2;
                warnings.Add("Master „" + masterCandidates[0]
                    + "“ fehlt in der BPMN-Schablone — Standard-Verbinder verwendet.");
            }

            connector.CellsU["BeginX"].GlueTo(source.CellsU["PinX"]);
            connector.CellsU["EndX"].GlueTo(target.CellsU["PinX"]);
            if (!string.IsNullOrEmpty(label))
                connector.Text = label;
        }

        // ---------------------------------------------------------------------
        // Master- und Shape-Data-Aufloesung
        // ---------------------------------------------------------------------

        private static readonly string[] EventTriggerRows = { "BpmnTriggerType", "BpmnTrigger", "TriggerType", "Trigger" };
        private static readonly string[] GatewayTypeRows = { "BpmnGatewayType", "GatewayType" };
        private static readonly string[] TaskTypeRows = { "BpmnTaskType", "TaskType" };

        /// <summary>Leitet die Trigger-Stichwoerter eines Events aus seinen EventDefinitions ab (null = keiner).</summary>
        private static string[] TriggerKeywordsFor(IBaseElement element)
        {
            IList<IEventDefinition> definitions =
                element is ICatchEvent catchEvent ? catchEvent.EventDefinitions
                : element is IThrowEvent throwEvent ? throwEvent.EventDefinitions
                : null;
            IEventDefinition definition = definitions == null ? null : definitions.FirstOrDefault();

            if (definition is IMessageEventDefinition) return new[] { "message", "nachricht" };
            if (definition is ITimerEventDefinition) return new[] { "timer" };
            if (definition is ISignalEventDefinition) return new[] { "signal" };
            if (definition is IEscalationEventDefinition) return new[] { "escalation", "eskalation" };
            if (definition is IErrorEventDefinition) return new[] { "error", "fehler" };
            if (definition is IConditionalEventDefinition) return new[] { "conditional", "bedingung" };
            if (definition is ILinkEventDefinition) return new[] { "link", "verkn" };
            return null;
        }

        /// <summary>
        /// Sucht einen Master erst per Universal-Name, dann per Namensvergleich ueber
        /// alle Master der Schablone (Name/NameU, exakt vor Contains). Ergebnis wird
        /// je Kandidatenliste gecacht (auch Fehltreffer, als null).
        /// </summary>
        private static Visio.Master FindMaster(Visio.Document stencil,
            Dictionary<string, Visio.Master> cache, string[] candidates)
        {
            string cacheKey = candidates[0];
            if (cache.TryGetValue(cacheKey, out Visio.Master cached)) return cached;

            Visio.Master found = null;
            foreach (string candidate in candidates)
            {
                try
                {
                    found = stencil.Masters.get_ItemU(candidate);
                    break;
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    // weiter mit Namens-Scan
                }
            }

            if (found == null)
            {
                foreach (Visio.Master master in stencil.Masters)
                {
                    foreach (string candidate in candidates)
                    {
                        if (string.Equals(master.NameU, candidate, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(master.Name, candidate, StringComparison.OrdinalIgnoreCase))
                        {
                            found = master;
                            break;
                        }
                    }
                    if (found != null) break;
                }
            }

            cache[cacheKey] = found;
            return found;
        }

        /// <summary>
        /// Setzt eine Shape-Data-Listen-Zelle (z. B. Task-Typ, Event-Trigger) auf den
        /// Listeneintrag, der eines der Stichwoerter enthaelt. Zellnamen und
        /// Wertelisten unterscheiden sich je Visio-Version/Sprache — deshalb wird die
        /// Format-Liste der Zelle gelesen und abgeglichen; ohne Treffer passiert
        /// bewusst nichts (das Shape bleibt dann generisch).
        /// </summary>
        private static void TrySetListProp(Visio.Shape shape, string[] rowNames, string[] keywords)
        {
            foreach (string rowName in rowNames)
            {
                try
                {
                    if (shape.CellExistsU["Prop." + rowName, 0] == 0)
                        continue;

                    string format = shape.CellsU["Prop." + rowName + ".Format"]
                        .ResultStr[(short)Visio.VisUnitCodes.visNoCast] ?? "";
                    foreach (string entry in format.Split(';'))
                    {
                        string cleaned = entry.Trim().Trim('"');
                        string lower = cleaned.ToLowerInvariant();
                        foreach (string keyword in keywords)
                        {
                            if (!lower.Contains(keyword)) continue;
                            shape.CellsU["Prop." + rowName].FormulaU = VisioHelper.QuoteLiteral(cleaned);
                            return;
                        }
                    }
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    // Zelle nicht lesbar/setzbar -- Untertyp bleibt Standard.
                }
            }
        }

        private static string DescribeElement(IBaseElement element)
        {
            var flowElement = element as IFlowElement;
            return !string.IsNullOrEmpty(flowElement?.Name) ? flowElement.Name : (element.Id ?? "?");
        }
    }
}
