#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using PassBpmnConverter.Bpmn.BpmnDI;
using PassBpmnConverter.Bpmn.DC;
using PassBpmnConverter.Bpmn.DI;

namespace PassBpmnConverter.Bpmn.Serialization;

/// <summary>
/// Liest eine BPMN-2.0-XML-Datei (z. B. aus bpmn.io, Camunda Modeler oder dem
/// eigenen Export) in das Objektmodell dieses Konverters ein — Gegenstueck zum
/// <see cref="BpmnSerializer"/>, aber bewusst pragmatisch: gelesen wird, was die
/// Visio-Anzeige braucht (Prozesse, Flow-Elemente, Collaboration/Pools,
/// Sequenz-/Nachrichtenfluesse und das BPMN-DI-Layout). Elemente werden ueber
/// ihren XML-LocalName erkannt (namespace-tolerant); unbekannte Flow-Elemente
/// und nicht darstellbare Konstrukte (Lanes, Data Objects, Annotationen)
/// werden mit Warnung uebersprungen statt den Import abzubrechen. Task-Varianten
/// ohne eigene Modellklasse (user/service/manual/businessRule) werden als
/// generischer Task eingelesen.
/// </summary>
public static class BpmnDeserializer
{
    public static IBpmnModel Deserialize(string filePath, IList<string> warnings)
    {
        XDocument document = XDocument.Load(filePath);
        XElement? definitionsXml = document.Root;
        if (definitionsXml == null || definitionsXml.Name.LocalName != "definitions")
            throw new InvalidOperationException(
                "Die Datei hat keine BPMN-<definitions>-Wurzel und ist keine BPMN-2.0-Datei.");

        var elementsById = new Dictionary<string, IBaseElement>();
        var processes = new List<IProcess>();
        var skippedTypes = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var pendingBoundaryEvents = new List<(XElement Xml, Process Process)>();
        var pendingSequenceFlows = new List<(XElement Xml, Process Process)>();

        // Phase 1: Prozesse mit ihren Flow-Knoten (Sequenzfluesse und Boundary-
        // Events brauchen aufgeloeste Referenzen und folgen in Phase 2/3).
        foreach (XElement processXml in definitionsXml.Elements().Where(e => e.Name.LocalName == "process"))
        {
            var process = new Process
            {
                Id = Attr(processXml, "id") ?? BpmnUtility.GenerateUniqueIdentifier(),
                Name = Attr(processXml, "name"),
            };
            processes.Add(process);
            Register(elementsById, process);

            foreach (XElement child in processXml.Elements())
            {
                switch (child.Name.LocalName)
                {
                    case "sequenceFlow":
                        pendingSequenceFlows.Add((child, process));
                        break;
                    case "boundaryEvent":
                        pendingBoundaryEvents.Add((child, process));
                        break;
                    default:
                        IFlowNode? node = CreateFlowNode(child, skippedTypes, warnings);
                        if (node != null)
                        {
                            process.FlowElements.Add(node);
                            Register(elementsById, node);
                        }
                        break;
                }
            }
        }

        // Phase 2: Boundary-Events (attachedToRef muss auf eine Activity zeigen).
        foreach ((XElement xml, Process process) in pendingBoundaryEvents)
        {
            CatchEvent boundary;
            if (Resolve(elementsById, Attr(xml, "attachedToRef")) is IActivity attachedTo)
            {
                boundary = new BoundaryEvent
                {
                    AttachedToRef = attachedTo,
                    CancelActivity = !string.Equals(Attr(xml, "cancelActivity"), "false", StringComparison.OrdinalIgnoreCase),
                };
            }
            else
            {
                warnings.Add("Boundary-Event „" + (Attr(xml, "name") ?? Attr(xml, "id") ?? "?")
                    + "“: attachedToRef nicht auflösbar — als Zwischen-Event eingelesen.");
                boundary = new IntermediateCatchEvent();
            }
            boundary.Id = Attr(xml, "id") ?? BpmnUtility.GenerateUniqueIdentifier();
            boundary.Name = Attr(xml, "name");
            ReadEventDefinitions(xml, boundary.EventDefinitions);
            process.FlowElements.Add(boundary);
            Register(elementsById, boundary);
        }

        // Phase 3: Sequenzfluesse inkl. Incoming/Outgoing-Verkettung (die braucht
        // der Layout-Generator, falls die Datei kein BPMN DI mitbringt).
        foreach ((XElement xml, Process process) in pendingSequenceFlows)
        {
            var source = Resolve(elementsById, Attr(xml, "sourceRef")) as IFlowNode;
            var target = Resolve(elementsById, Attr(xml, "targetRef")) as IFlowNode;
            if (source == null || target == null)
            {
                warnings.Add("Sequenzfluss „" + (Attr(xml, "name") ?? Attr(xml, "id") ?? "?")
                    + "“ übersprungen (Quelle oder Ziel fehlt bzw. wurde nicht eingelesen).");
                continue;
            }

            var flow = new SequenceFlow
            {
                SourceRef = source,
                TargetRef = target,
                Id = Attr(xml, "id") ?? BpmnUtility.GenerateUniqueIdentifier(),
                Name = Attr(xml, "name"),
            };
            source.Outgoing.Add(flow);
            target.Incoming.Add(flow);
            process.FlowElements.Add(flow);
            Register(elementsById, flow);
        }

        // Phase 4: Collaboration (Pools + Nachrichtenfluesse). Fehlt sie, bekommt
        // jeder Prozess einen synthetischen Participant — Layout-Generator und
        // Renderer arbeiten pool-orientiert.
        var collaboration = new Collaboration { Id = BpmnUtility.GenerateUniqueIdentifier() };
        XElement? collaborationXml = definitionsXml.Elements().FirstOrDefault(e => e.Name.LocalName == "collaboration");
        if (collaborationXml != null)
        {
            collaboration.Id = Attr(collaborationXml, "id") ?? collaboration.Id;
            collaboration.Name = Attr(collaborationXml, "name");

            foreach (XElement participantXml in collaborationXml.Elements().Where(e => e.Name.LocalName == "participant"))
            {
                var participant = new Participant
                {
                    Id = Attr(participantXml, "id") ?? BpmnUtility.GenerateUniqueIdentifier(),
                    Name = Attr(participantXml, "name"),
                    ProcessRef = Resolve(elementsById, Attr(participantXml, "processRef")) as IProcess,
                };
                collaboration.Participants.Add(participant);
                Register(elementsById, participant);
            }

            foreach (XElement messageFlowXml in collaborationXml.Elements().Where(e => e.Name.LocalName == "messageFlow"))
            {
                var source = Resolve(elementsById, Attr(messageFlowXml, "sourceRef")) as IInteractionNode;
                var target = Resolve(elementsById, Attr(messageFlowXml, "targetRef")) as IInteractionNode;
                if (source == null || target == null)
                {
                    warnings.Add("Nachrichtenfluss „" + (Attr(messageFlowXml, "name") ?? Attr(messageFlowXml, "id") ?? "?")
                        + "“ übersprungen (Quelle oder Ziel fehlt bzw. wurde nicht eingelesen).");
                    continue;
                }

                var messageFlow = new MessageFlow
                {
                    SourceRef = source,
                    TargetRef = target,
                    Id = Attr(messageFlowXml, "id") ?? BpmnUtility.GenerateUniqueIdentifier(),
                    Name = Attr(messageFlowXml, "name"),
                };
                collaboration.MessageFlows.Add(messageFlow);
                Register(elementsById, messageFlow);
            }
        }

        foreach (IProcess process in processes)
        {
            if (!collaboration.Participants.Any(p => ReferenceEquals(p.ProcessRef, process)))
            {
                collaboration.Participants.Add(new Participant
                {
                    Id = BpmnUtility.GenerateUniqueIdentifier(),
                    Name = process.Name,
                    ProcessRef = process,
                });
            }
        }

        if (processes.Count == 0)
            throw new InvalidOperationException("Die Datei enthält keinen BPMN-Prozess.");

        // Phase 5: BPMN-DI-Layout (falls vorhanden). Nur Elemente uebernehmen, die
        // in Phase 1-4 eingelesen wurden — der Rest wird gezaehlt und gemeldet.
        var planeElements = new List<IDiagramElement>();
        int unresolvedDiElements = 0;
        XElement? planeXml = definitionsXml.Descendants().FirstOrDefault(e => e.Name.LocalName == "BPMNPlane");
        if (planeXml != null)
        {
            foreach (XElement shapeXml in planeXml.Elements().Where(e => e.Name.LocalName == "BPMNShape"))
            {
                IBaseElement? element = Resolve(elementsById, Attr(shapeXml, "bpmnElement"));
                XElement? boundsXml = shapeXml.Elements().FirstOrDefault(e => e.Name.LocalName == "Bounds");
                if (element == null || boundsXml == null)
                {
                    unresolvedDiElements++;
                    continue;
                }

                planeElements.Add(new BpmnShape
                {
                    BpmnElement = element,
                    Bounds = new Bounds
                    {
                        X = Dbl(boundsXml, "x"),
                        Y = Dbl(boundsXml, "y"),
                        Width = Dbl(boundsXml, "width"),
                        Height = Dbl(boundsXml, "height"),
                    },
                    IsHorizontal = BoolAttr(shapeXml, "isHorizontal"),
                    IsExpanded = BoolAttr(shapeXml, "isExpanded"),
                });
            }

            foreach (XElement edgeXml in planeXml.Elements().Where(e => e.Name.LocalName == "BPMNEdge"))
            {
                IBaseElement? element = Resolve(elementsById, Attr(edgeXml, "bpmnElement"));
                if (element == null)
                {
                    unresolvedDiElements++;
                    continue;
                }

                var edge = new BpmnEdge { BpmnElement = element };
                foreach (XElement waypointXml in edgeXml.Elements().Where(e => e.Name.LocalName == "waypoint"))
                    edge.Waypoints.Add(new Point { X = Dbl(waypointXml, "x"), Y = Dbl(waypointXml, "y") });
                planeElements.Add(edge);
            }
        }

        if (unresolvedDiElements > 0)
            warnings.Add(unresolvedDiElements + " Diagramm-Element(e) der Datei verweisen auf nicht "
                + "eingelesene Modell-Elemente und wurden ausgelassen.");
        if (skippedTypes.Count > 0)
            warnings.Add("Nicht unterstützte BPMN-Elemente übersprungen: " + string.Join(", ", skippedTypes) + ".");

        var definitions = new Definitions
        {
            Id = Attr(definitionsXml, "id"),
            Name = Attr(definitionsXml, "name"),
            TargetNamespace = Attr(definitionsXml, "targetNamespace") ?? "http://alps-visio-addin/bpmn-import",
        };
        definitions.RootElements.Add(collaboration);
        foreach (IProcess process in processes)
            definitions.RootElements.Add(process);

        if (planeElements.OfType<IBpmnShape>().Any())
        {
            definitions.Diagrams.Add(new BpmnDiagram
            {
                Id = BpmnUtility.GenerateUniqueIdentifier(),
                BpmnPlane = new BpmnPlane
                {
                    BpmnElement = collaboration,
                    DiagramElements = planeElements,
                },
            });
        }

        return new BpmnModel { Definitions = definitions };
    }

    /// <summary>Erzeugt den Flow-Knoten zum XML-Element; null = unbekannt/nicht darstellbar.</summary>
    private static IFlowNode? CreateFlowNode(XElement xml, ISet<string> skippedTypes, IList<string> warnings)
    {
        FlowNode? node;
        switch (xml.Name.LocalName)
        {
            case "task":
            case "userTask":
            case "serviceTask":
            case "manualTask":
            case "businessRuleTask":
                node = new Task();
                break;
            case "sendTask":
                node = new SendTask();
                break;
            case "receiveTask":
                node = new ReceiveTask();
                break;
            case "scriptTask":
                node = new ScriptTask();
                break;
            case "callActivity":
                node = new CallActivity();
                break;
            case "subProcess":
            case "adHocSubProcess":
            case "transaction":
                node = new SubProcess();
                if (xml.Elements().Any(e => e.Name.LocalName == "sequenceFlow" || e.Name.LocalName.EndsWith("Task")
                    || e.Name.LocalName.EndsWith("Event") || e.Name.LocalName.EndsWith("Gateway") || e.Name.LocalName == "task"))
                    warnings.Add("Sub-Prozess „" + (Attr(xml, "name") ?? Attr(xml, "id") ?? "?")
                        + "“: eingebettete Inhalte werden nicht dargestellt (nur das Sub-Prozess-Shape).");
                break;
            case "startEvent":
                node = new StartEvent();
                break;
            case "endEvent":
                node = new EndEvent();
                break;
            case "intermediateCatchEvent":
                node = new IntermediateCatchEvent();
                break;
            case "intermediateThrowEvent":
                node = new IntermediateThrowEvent();
                break;
            case "exclusiveGateway":
                node = new ExclusiveGateway();
                break;
            case "parallelGateway":
                node = new ParallelGateway();
                break;
            case "inclusiveGateway":
                node = new InclusiveGateway();
                break;
            case "complexGateway":
                node = new ComplexGateway();
                break;
            case "eventBasedGateway":
                node = new EventBasedGateway();
                break;
            // Metadaten/Nicht-Knoten: still ueberspringen.
            case "documentation":
            case "extensionElements":
            case "ioSpecification":
            case "property":
            case "dataObject":
                return null;
            default:
                // Sichtbare, aber nicht unterstuetzte Konstrukte gesammelt melden.
                skippedTypes.Add(xml.Name.LocalName);
                return null;
        }

        node.Id = Attr(xml, "id") ?? BpmnUtility.GenerateUniqueIdentifier();
        node.Name = Attr(xml, "name");

        if (node is CatchEvent catchEvent)
            ReadEventDefinitions(xml, catchEvent.EventDefinitions);
        else if (node is ThrowEvent throwEvent)
            ReadEventDefinitions(xml, throwEvent.EventDefinitions);

        return node;
    }

    private static void ReadEventDefinitions(XElement eventXml, IList<IEventDefinition> target)
    {
        foreach (XElement definitionXml in eventXml.Elements())
        {
            switch (definitionXml.Name.LocalName)
            {
                case "messageEventDefinition": target.Add(new MessageEventDefinition()); break;
                case "timerEventDefinition": target.Add(new TimerEventDefinition()); break;
                case "signalEventDefinition": target.Add(new SignalEventDefinition()); break;
                case "escalationEventDefinition": target.Add(new EscalationEventDefinition()); break;
                case "errorEventDefinition": target.Add(new ErrorEventDefinition()); break;
                case "conditionalEventDefinition": target.Add(new ConditionalEventDefinition { Condition = new Expression() }); break;
                case "linkEventDefinition": target.Add(new LinkEventDefinition { Name = Attr(definitionXml, "name") ?? "" }); break;
                // terminateEventDefinition u. a. haben keine Modellklasse — das
                // Event selbst wird trotzdem (ohne Marker) dargestellt.
            }
        }
    }

    private static void Register(IDictionary<string, IBaseElement> elementsById, IBaseElement element)
    {
        if (!string.IsNullOrEmpty(element.Id))
            elementsById[element.Id!] = element;
    }

    /// <summary>Loest eine IDREF auf; QName-Praefixe („ns:id“) werden toleriert.</summary>
    private static IBaseElement? Resolve(IDictionary<string, IBaseElement> elementsById, string? reference)
    {
        if (string.IsNullOrEmpty(reference)) return null;
        if (elementsById.TryGetValue(reference!, out IBaseElement? element)) return element;

        int colon = reference!.LastIndexOf(':');
        if (colon >= 0 && elementsById.TryGetValue(reference.Substring(colon + 1), out element)) return element;
        return null;
    }

    private static string? Attr(XElement element, string name)
    {
        return element.Attribute(name)?.Value;
    }

    private static double Dbl(XElement element, string name)
    {
        return double.TryParse(Attr(element, name), NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
            ? value
            : 0.0;
    }

    private static bool? BoolAttr(XElement element, string name)
    {
        return bool.TryParse(Attr(element, name), out bool value) ? value : (bool?)null;
    }
}
