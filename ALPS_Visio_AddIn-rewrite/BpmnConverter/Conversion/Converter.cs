#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using alps.net.api.ALPS;
using alps.net.api.StandardPASS;
using PassBpmnConverter.Bpmn;
using PassBpmnConverter.Pass;

namespace PassBpmnConverter.Conversion;

public class Converter
{
    private IGraph? _graph;

    private ISignal? _startSignal;
    private readonly Dictionary<IMessageSpecification, IMessage> _messages;
    private readonly Dictionary<IMacroBehavior, IEscalation> _macroCallEscalations;
    private readonly Dictionary<IMacroBehavior, ISignal> _macroReturnToOriginSignals;

    public Converter()
    {
        _messages = [];
        _macroCallEscalations = [];
        _macroReturnToOriginSignals = [];
    }

    /// <summary>
    /// Converts a PASS model to a BPMN model
    /// </summary>
    /// <param name="passModel"></param>
    /// <returns>The converter BPMN model</returns>
    public static IBpmnModel ConvertPassToBpmn(IPASSProcessModel passModel)
    {
        return new Converter().ConvertModel(passModel);
    }

    private IBpmnModel ConvertModel(IPASSProcessModel passModel)
    {
        Import(passModel);
        Transformation();
        return Export();
    }

    private void Import(IPASSProcessModel passModel)
    {
        ImportModel(passModel);

        ImportSubjects();
        ImportSubjectBehaviors();
        ImportStatesAndTransitions();
    }

    private void Transformation()
    {
        ProcessBaseAndMacroStartStates();
        ProcessGuardReceiveTransitions();
        ProcessReturnToOriginReferences();
        ProcessEndStates();

        ProcessMultipleIncomingEdges();

        ProcessUserCancelTransitions();
        ProcessTimeTransitions();
        ProcessSendingFailedTransitions();
        ProcessDoTransitions();
        ProcessSendTransitions();
        ProcessReceiveTransitions();

        ProcessMacroStates();
        ProcessDoStates();
        ProcessSendStates();
        ProcessReceiveStates();

        ProcessBoundaryEvents();

        // order is important here
        ProcessRedundantGateways<IEventBasedGateway>();
        ProcessRedundantGateways<IExclusiveGateway>();

        ProcessEdges();
    }

    private IBpmnModel Export()
    {
        ExportFlowElements();

        return _graph.BpmnElement as BpmnModel;
    }

    // Rule: Import PASS Model
    private void ImportModel(IPASSProcessModel passModel)
    {
        IBpmnModel bpmnModel = BpmnUtility.CreateModel();

        IDefinitions definitions = BpmnUtility.CreateDefinitions(PassUtility.GetElementName(passModel));

        bpmnModel.Definitions = definitions;

        ICollaboration collaboration = BpmnUtility.CreateCollaboration();

        definitions.RootElements.Add(collaboration);

        _graph = new Graph()
        {
            PassElement = passModel,
            BpmnElement = bpmnModel
        };
    }

    // Rule A: Import Fully Specified Subjects
    // Rule B: Import Interface Subjects
    private void ImportSubjects()
    {
        if (!(_graph?.PassElement is IPASSProcessModel passModel && _graph.BpmnElement is IBpmnModel bpmnModel))
            return;

        foreach (ISubject subject in passModel.getBaseLayer().getElements().Values.OfType<ISubject>())
        {
            if (!(subject is IInterfaceSubject || subject is IFullySpecifiedSubject))
            {
                Console.WriteLine($"Warning: {nameof(ISubject)} ({PassUtility.GetElementId(subject)}) of type {subject.GetType()} is not supported. Skipping {nameof(ISubject)} conversion.");
                continue;
            }

            IParticipant participant = BpmnUtility.CreateParticipant(name: PassUtility.GetElementName(subject));

            int maxMultiplicity = subject.getInstanceRestriction();
            if (maxMultiplicity == 0 || maxMultiplicity > 1)
            {
                participant.ParticipantMultiplicity = new ParticipantMultiplicity()
                {
                    // TODO: find a better solution for uncapped/inf
                    Maximum = maxMultiplicity == 0 ? int.MaxValue : maxMultiplicity
                };
            }

            ICollaboration? collaboration = GetCollaboration() ?? throw new InvalidOperationException();
            collaboration.Participants.Add(participant);

            ISubGraph subjectSubGraph = new SubGraph()
            {
                PassElement = subject,
                BpmnElement = participant
            };
            AddElementToGraph(subjectSubGraph, _graph);
        }
    }

    // Rule A: Import Base Beahaviors
    // Rule B: Import Macro Beahaviors
    // Rule C: Import Guard Beahaviors
    private void ImportSubjectBehaviors()
    {
        foreach (ISubGraph subGraph in Query<ISubGraph>())
        {
            if (!(subGraph.PassElement is IFullySpecifiedSubject fullySpecifiedSubject
                && subGraph.BpmnElement is IParticipant participant))
                continue;

            // the base behavior must be converted first, therefore we order the collection
            IEnumerable<ISubjectBehavior> subjectBehaviors = fullySpecifiedSubject.getBehaviors().Values.OrderByDescending(IsBaseBehavior);

            foreach (ISubjectBehavior subjectBehavior in subjectBehaviors)
            {
                if (IsBaseBehavior(subjectBehavior))
                {
                    IProcess baseProcess = BpmnUtility.CreateProcess();
                    participant.ProcessRef = baseProcess;

                    IDefinitions? definitions = GetDefinitions() ?? throw new InvalidOperationException();
                    definitions.RootElements.Add(baseProcess);

                    ISubGraph behaviorSubGraph = new SubGraph()
                    {
                        PassElement = subjectBehavior,
                        BpmnElement = baseProcess
                    };
                    AddElementToGraph(behaviorSubGraph, subGraph);
                }
                else if (subjectBehavior is IMacroBehavior || subjectBehavior is IGuardBehavior)
                {
                    // TODO: improve error handling
                    if (participant.ProcessRef == null)
                        throw new InvalidOperationException();

                    ISubProcess eventSubProcess = BpmnUtility.CreateSubProcess(name: PassUtility.GetElementName(subjectBehavior), triggeredByEvent: true);
                    participant.ProcessRef.FlowElements.Add(eventSubProcess);

                    ISubGraph behaviorSubGraph = new SubGraph()
                    {
                        PassElement = subjectBehavior,
                        BpmnElement = eventSubProcess
                    };
                    AddElementToGraph(behaviorSubGraph, subGraph);
                }
                else
                {
                    Console.WriteLine($"Warning: {nameof(ISubjectBehavior)} ({PassUtility.GetElementId(subjectBehavior)}) of type {subjectBehavior.GetType()} is not supported. Skipping {nameof(ISubjectBehavior)} conversion.");
                    continue;
                }
            }
        }
    }

    // Rule A: Import States
    // Rule B: Import Transitions
    private void ImportStatesAndTransitions()
    {
        foreach (ISubGraph subGraph in Query<ISubGraph>())
        {
            if (!(subGraph.PassElement is ISubjectBehavior subjectBehavior))
                continue;

            IEnumerable<IBehaviorDescribingComponent> behaviorDescribingComponents = GetBehaviorDescribingComponents(subjectBehavior);

            foreach (IState state in behaviorDescribingComponents.OfType<IState>())
            {
                INode stateNode = new Node()
                {
                    State = state
                };
                AddElementToGraph(stateNode, subGraph);
            }

            foreach (ITransition transition in behaviorDescribingComponents.OfType<ITransition>())
            {
                IState sourceState = transition.getSourceState();
                IState targetState = transition.getTargetState();

                if (sourceState == null || targetState == null)
                {
                    Console.WriteLine($"Warning: {nameof(ITransition)} ({PassUtility.GetElementId(transition)}) is missing a source or target {nameof(IState)}. Skipping {nameof(ITransition)} import.");
                    continue;
                }

                INode? sourceNode = FindElementInGraph<INode>(node => node.State == sourceState, subGraph);
                INode? targetNode = FindElementInGraph<INode>(node => node.State == targetState, subGraph);

                if (sourceNode == null || targetNode == null)
                {
                    Console.WriteLine($"Warning: No valid source of target {nameof(INode)} found to connect {nameof(IEdge)} to. Skipping {nameof(ITransition)} import.");
                    continue;
                }

                IEdge transitionEdge = new Edge()
                {
                    Transition = transition,
                    Source = sourceNode,
                    Target = targetNode
                };
                AddElementToGraph(transitionEdge, subGraph);
            }
        }
    }

    // Rule A: Process Base Behavior Start States
    // Rule B: Process Macro Behavior Start States
    private void ProcessBaseAndMacroStartStates()
    {
        foreach ((INode node, IGraph graph) in QueryWithGraph<INode>())
        {
            if (!(node.State != null && node.State.isStateType(IState.StateType.InitialStateOfBehavior)))
                continue;

            IStartEvent startEvent;

            if (graph.PassElement is ISubjectBehavior subjectBehavior && IsBaseBehavior(subjectBehavior))
            {
                startEvent = BpmnUtility.CreateStartEvent(eventDefinition: BpmnUtility.CreateSignalEventDefinition(signal: GetOrCreateStartSignal()),
                                                          name: "Start Process");
            }
            else if (graph.PassElement is IMacroBehavior macroBehavior)
            {
                IEscalation callEscalation = GetOrCreateMacroCallEscalation(macroBehavior);
                startEvent = BpmnUtility.CreateStartEvent(eventDefinition: BpmnUtility.CreateEscalationEventDefinition(escalation: callEscalation),
                                                          isInterrupting: false);
            }
            else
            {
                continue;
            }

            INode startEventNode = new Node()
            {
                FlowNode = startEvent
            };
            PrependNode(startEventNode, node);
        }
    }

    // Rule: Process Guard Receive Transitions
    private void ProcessGuardReceiveTransitions()
    {
        foreach (IEdge edge in Query<IEdge>())
        {
            if (!(edge.Transition is IReceiveTransition receiveTransition && edge.Source.State is IGuardReceiveState))
                continue;

            IMessageSpecification? messageSpecification = receiveTransition.getTransitionCondition().getReceptionOfMessage();

            INode exclusiveGatewayNode = new Node()
            {
                FlowNode = BpmnUtility.CreateExclusiveGateway()
            };
            InsertNodeAfter(exclusiveGatewayNode, edge);

            INode startEventNode = new Node()
            {
                FlowNode = BpmnUtility.CreateStartEvent(eventDefinition: BpmnUtility.CreateMessageEventDefinition(message: GetOrCreateMessage(messageSpecification)),
                                                        name: $"{PassUtility.GetElementName(messageSpecification)} received")
            };
            PrependNode(startEventNode, exclusiveGatewayNode);
        }
    }

    // Rule A: Process Macro Behavior Return To Origins
    // Rule B: Process Guard Behavior Return To Origins
    private void ProcessReturnToOriginReferences()
    {
        foreach ((INode node, IGraph graph) in QueryWithGraph<INode>())
        {
            if (!(node.State is IGenericReturnToOriginReference))
                continue;

            if (graph.PassElement is IMacroBehavior macroBehavior)
            {
                ISignal returnToOriginSignal = GetOrCreateMacroReturnToOriginSignal(macroBehavior);
                node.FlowNode = BpmnUtility.CreateEndEvent(eventDefinition: BpmnUtility.CreateSignalEventDefinition(signal: returnToOriginSignal),
                                                           name: "Return to Origin");
            }
            else if (graph.PassElement is IGuardBehavior)
            {
                // TODO: reference the correct state
                node.FlowNode = BpmnUtility.CreateScripTask(name: "Return to Origin");
            }
        }
    }

    // Rule: Process End States
    private void ProcessEndStates()
    {
        foreach ((INode node, IGraph graph) in QueryWithGraph<INode>())
        {
            if (!(node.State != null && node.State.isStateType(IState.StateType.EndState)))
                continue;

            IExclusiveGateway exclusiveGateway = BpmnUtility.CreateExclusiveGateway();
            INode exclusiveGatewayNode = new Node()
            {
                FlowNode = exclusiveGateway
            };
            InsertNodeAfter(exclusiveGatewayNode, node);

            IEndEvent endEvent = BpmnUtility.CreateEndEvent();
            INode endEventNode = new Node()
            {
                FlowNode = endEvent
            };
            AddElementToGraph(endEventNode, graph);

            IEdge edge = new Edge()
            {
                Source = exclusiveGatewayNode,
                Target = endEventNode,
                SequenceFlow = BpmnUtility.CreateSequenceFlow(source: exclusiveGateway, target: endEvent),
            };
            AddElementToGraph(edge, graph);

            exclusiveGateway.Default = edge.SequenceFlow;
        }
    }

    // Rule: Process multiple incoming Edges
    private void ProcessMultipleIncomingEdges()
    {
        foreach (INode node in Query<INode>())
        {
            ICollection<IEdge> incomingEdges = GetIncomingEdges(node);

            if (incomingEdges.Count < 2)
                continue;

            INode exclusiveGatewayNode = new Node()
            {
                FlowNode = BpmnUtility.CreateExclusiveGateway()
            };
            InsertNodeBefore(exclusiveGatewayNode, node);
        }
    }

    // Rule: Process User Cancel Transitions
    private void ProcessUserCancelTransitions()
    {
        foreach (IEdge edge in Query<IEdge>())
        {
            if (!(edge.Transition is IUserCancelTransition userCancelTransition))
                continue;

            INode conditionalEventNode = new Node()
            {
                FlowNode = BpmnUtility.CreateIntermediateCatchEvent(eventDefinition: BpmnUtility.CreateConditionalEventDefinition(),
                                                                    name: PassUtility.GetElementName(userCancelTransition))
            };
            InsertNodeBefore(conditionalEventNode, edge);
        }
    }

    // Rule: Process Time Transitions
    private void ProcessTimeTransitions()
    {
        foreach (IEdge edge in Query<IEdge>())
        {
            if (!(edge.Transition is ITimeTransition timeTransition))
                continue;

            INode timerEventNode = new Node()
            {
                FlowNode = BpmnUtility.CreateIntermediateCatchEvent(eventDefinition: BpmnUtility.CreateTimerEventDefinition(),
                                                                    name: PassUtility.GetElementName(timeTransition))
            };
            InsertNodeBefore(timerEventNode, edge);
        }
    }

    // Rule: Process Sending Failed Transitions
    private void ProcessSendingFailedTransitions()
    {
        foreach (IEdge edge in Query<IEdge>())
        {
            if (!(edge.Transition is ISendingFailedTransition sendingFailedTransition))
                continue;

            INode errorEventNode = new Node()
            {
                // TODO: an error intermediate event is technically not possible, but it will be converted to an event based gateway
                FlowNode = BpmnUtility.CreateIntermediateCatchEvent(eventDefinition: BpmnUtility.CreateErrorEventDefinition(),
                                                                    name: PassUtility.GetElementName(sendingFailedTransition))
            };
            InsertNodeBefore(errorEventNode, edge);
        }
    }

    // Rule: Process Do Transitions
    private void ProcessDoTransitions()
    {
        foreach (IEdge edge in Query<IEdge>())
        {
            if (!(edge.Transition is IDoTransition doTransition))
                continue;

            // TODO: this is okay as the source and target of the sequence flow are assigned later, but the compiler doesn't like it since we assign null to a non-nullable.
            edge.SequenceFlow = BpmnUtility.CreateSequenceFlow(null, null, name: PassUtility.GetElementName(doTransition));
        }
    }

    // Rule: Process Send Transitions
    private void ProcessSendTransitions()
    {
        foreach (IEdge edge in Query<IEdge>())
        {
            if (!(edge.Transition is ISendTransition sendTransition))
                continue;

            IMessageSpecification messageSpecification = sendTransition.getTransitionCondition().getRequiresSendingOfMessage();

            INode sendTaskNode = new Node()
            {
                FlowNode = BpmnUtility.CreateSendTask(name: $"Send {PassUtility.GetElementName(messageSpecification)}",
                                                      message: GetOrCreateMessage(messageSpecification)),
            };
            InsertNodeAfter(sendTaskNode, edge);
        }
    }

    // Rule: Process Send Transitions
    private void ProcessReceiveTransitions()
    {
        foreach (IEdge edge in Query<IEdge>())
        {
            if (!(edge.Transition is IReceiveTransition receiveTransition))
                continue;

            if (receiveTransition.getSourceState() is IGuardReceiveState guardReceiveState)
            {
                if (guardReceiveState.getIncomingTransitions().Count == 0)
                    continue;
            }

            IMessageSpecification messageSpecification = receiveTransition.getTransitionCondition().getReceptionOfMessage();

            INode receiveTaskNode = new Node()
            {
                FlowNode = BpmnUtility.CreateReceiveTask(name: $"Receive {PassUtility.GetElementName(messageSpecification)}",
                                                         message: GetOrCreateMessage(messageSpecification)),
            };
            InsertNodeAfter(receiveTaskNode, edge);
        }
    }

    // Rule: Process Macro States
    private void ProcessMacroStates()
    {
        foreach (INode node in Query<INode>())
        {
            if (!(node.State is IMacroState macroState && macroState.getReferencedMacroBehavior() != null))
                continue;

            ISubGraph? macroSubGraph = FindElementInGraphRecursive<ISubGraph>(subGraph => subGraph.PassElement == macroState.getReferencedMacroBehavior());

            if (!(macroSubGraph != null && macroSubGraph.PassElement is IMacroBehavior macroBehavior && macroSubGraph.BpmnElement is ISubProcess subProcess))
            {
                Console.WriteLine($"Error: No appropriate {nameof(ISubGraph)} exsits for the referenced {nameof(IMacroBehavior)}. Skipping {nameof(IMacroState)} conversion.");
                continue;
            }

            IEscalation callEscalation = GetOrCreateMacroCallEscalation(macroBehavior);
            ISignal returnToOriginSignal = GetOrCreateMacroReturnToOriginSignal(macroBehavior);

            INode callMacroNode = new Node()
            {
                FlowNode = BpmnUtility.CreateIntermediateThrowEvent(eventDefinition: BpmnUtility.CreateEscalationEventDefinition(escalation: callEscalation),
                                                                    name: $"Call {subProcess?.Name}"),
            };
            InsertNodeBefore(callMacroNode, node);

            INode waitForReturnToOriginNode = new Node()
            {
                FlowNode = BpmnUtility.CreateIntermediateCatchEvent(eventDefinition: BpmnUtility.CreateSignalEventDefinition(signal: returnToOriginSignal),
                                                                    name: "Wait for macro completion"),
            };
            InsertNodeAfter(waitForReturnToOriginNode, callMacroNode);
        }
    }

    // Rule: Process Do States
    private void ProcessDoStates()
    {
        foreach (INode node in Query<INode>())
        {
            if (!(node.State is DoState doState))
                continue;

            node.FlowNode = BpmnUtility.CreateAbstractTask(name: PassUtility.GetElementName(doState));

            IEnumerable<IEdge> outgoingDoTransitionEdges = GetOutgoingEdges(node).Where(edge => edge.Transition is IDoTransition);

            INode exclusiveGatewayNode = new Node()
            {
                FlowNode = BpmnUtility.CreateExclusiveGateway(),
            };
            AppendNode(exclusiveGatewayNode, node);

            foreach (IEdge outgoingDoTransitionEdge in outgoingDoTransitionEdges)
            {
                outgoingDoTransitionEdge.Source = exclusiveGatewayNode;
            }

            // TODO: handle outgoingDoTransitionEdges.Count() == 0
        }
    }

    // Rule: Process Send States
    private void ProcessSendStates()
    {
        foreach (INode node in Query<INode>())
        {
            if (!(node.State is ISendState sendState))
                continue;

            ICollection<IEdge> outgoingEdges = GetOutgoingEdges(node);

            IEnumerable<INode> sendTaskNodes = outgoingEdges.Where(edge => edge.Target.FlowNode is ISendTask).Select(edge => edge.Target);

            if (sendTaskNodes.Count() > 1)
            {
                throw new InvalidOperationException($"{nameof(ISendState)} has more than one outgoing {nameof(ISendTransition)}. This is not allowed.");
            }

            INode? sendTaskNode = sendTaskNodes.FirstOrDefault();

            // if a (already converted) send transition exsists, change all edges to originate from the correseponding (send task) node
            if (sendTaskNode != null)
            {
                foreach (IEdge outgoingEdge in outgoingEdges)
                {
                    // ignore the edge that connects send state node to the send task node, as this would result in an edge that has the send task node for both its source and target
                    if (outgoingEdge.Target == sendTaskNode)
                    {
                        RemoveElement(outgoingEdge);
                        continue;
                    }

                    outgoingEdge.Source = sendTaskNode;
                }

                foreach (IEdge incomingEdge in GetIncomingEdges(node))
                {
                    incomingEdge.Target = sendTaskNode;
                }

                RemoveElement(node);
            }
            // TODO: testing
            else
            {
                node.FlowNode = BpmnUtility.CreateEventBasedGateway();
            }
        }
    }

    // Rule: Process Receive States
    private void ProcessReceiveStates()
    {
        foreach (INode node in Query<INode>())
        {
            if (!(node.State is IReceiveState receiveState))
                continue;

            node.FlowNode = BpmnUtility.CreateEventBasedGateway();
        }
    }

    // Rule: Process Boundary Events
    private void ProcessBoundaryEvents()
    {
        foreach (INode node in Query<INode>())
        {
            if (!(node.FlowNode is IIntermediateCatchEvent intermediateCatchEvent))
                continue;

            ICollection<IEdge> incomingEdges = GetIncomingEdges(node);
            if (incomingEdges.Count > 1)
                throw new InvalidOperationException();

            if (incomingEdges.First().Source.FlowNode is not ITask sourceTask)
                continue;

            IEventDefinition? eventDefinition = intermediateCatchEvent.EventDefinitions.FirstOrDefault();
            if (!(eventDefinition is not ITimerEventDefinition
                || eventDefinition is not IConditionalEventDefinition
                || eventDefinition is not IErrorEventDefinition))
                continue;

            node.FlowNode = BpmnUtility.CreateBoundaryEvent(sourceTask, eventDefinition);

            foreach (IEdge incomingEdge in incomingEdges)
            {
                RemoveElement(incomingEdge);
            }
        }
    }

    // Rule: Process Edges
    private void ProcessEdges()
    {
        foreach (IEdge edge in Query<IEdge>())
        {
            if (edge.Source.FlowNode == null || edge.Target.FlowNode == null)
            {
                edge.SequenceFlow = null;
                continue;
            }

            ISequenceFlow? sequenceFlow = edge.SequenceFlow;

            if (sequenceFlow == null)
            {
                sequenceFlow = BpmnUtility.CreateSequenceFlow(edge.Source.FlowNode, edge.Target.FlowNode);
                edge.SequenceFlow = sequenceFlow;
            }
            else
            {
                sequenceFlow.SourceRef = edge.Source.FlowNode;
                sequenceFlow.TargetRef = edge.Target.FlowNode;
            }

            edge.Source.FlowNode.Outgoing.Add(sequenceFlow);
            edge.Target.FlowNode.Incoming.Add(sequenceFlow);
        }
    }

    // Rule: Remove Redundant Gateways
    private void ProcessRedundantGateways<T>() where T : IGateway
    {
        foreach (INode node in Query<INode>())
        {
            if (!(node.FlowNode is T))
                continue;

            ICollection<IEdge> incomingEdges = GetIncomingEdges(node);
            ICollection<IEdge> outgoingEdges = GetOutgoingEdges(node);

            // only remove redundant ones
            if (incomingEdges.Count > 1 || outgoingEdges.Count > 1)
                continue;

            IEdge? incomingEdge = incomingEdges.FirstOrDefault();
            IEdge? outgoingEdge = outgoingEdges.FirstOrDefault();

            if (incomingEdge != null && outgoingEdge != null)
            {
                outgoingEdge.Source = incomingEdge.Source;
                RemoveElement(node);
                RemoveElement(incomingEdge);
            }
            else
            {
                if (incomingEdge != null)
                {
                    RemoveElement(incomingEdge);
                }
                if (outgoingEdge != null)
                {
                    RemoveElement(outgoingEdge);
                }
                RemoveElement(node);
            }
        }
    }

    // Rule: Export Flow Nodes
    // Rule: Export Sequence Flows
    private void ExportFlowElements()
    {
        foreach ((INode node, IGraph graph) in QueryWithGraph<INode>())
        {
            if (node.FlowNode == null)
            {
                if (node is not IGraph)
                    Console.WriteLine($"Warning: {nameof(INode)} does not have an assigned {nameof(IFlowNode)}. Cannot export {nameof(INode)} to BPMN.");

                continue;
            }

            if (graph.BpmnElement is not IFlowElementsContainer flowElementsContainer)
                continue;

            flowElementsContainer.FlowElements.Add(node.FlowNode);
        }

        foreach ((IEdge edge, IGraph graph) in QueryWithGraph<IEdge>())
        {
            if (edge.SequenceFlow == null)
            {
                Console.WriteLine($"Warning: {nameof(IEdge)} does not have an assigned {nameof(ISequenceFlow)}. Cannot export {nameof(IEdge)} to BPMN.");
                continue;
            }

            if (graph.BpmnElement is not IFlowElementsContainer flowElementsContainer)
                continue;

            flowElementsContainer.FlowElements.Add(edge.SequenceFlow);
        }
    }

    #region Helper Methods

    private IDefinitions? GetDefinitions()
    {
        return (_graph?.BpmnElement as IBpmnModel)?.Definitions;
    }

    private ICollaboration? GetCollaboration()
    {
        return GetDefinitions()?.RootElements.OfType<ICollaboration>().FirstOrDefault();
    }

    private ISignal GetOrCreateStartSignal()
    {
        if (_startSignal == null)
        {
            _startSignal = BpmnUtility.CreateSignal(name: "Start Signal");
            GetDefinitions()?.RootElements.Add(_startSignal);
        }

        return _startSignal;
    }

    private IMessage? GetOrCreateMessage(IMessageSpecification? messageSpecification)
    {
        if (messageSpecification == null)
            return null;

        if (!_messages.TryGetValue(messageSpecification, out IMessage? message))
        {
            message = BpmnUtility.CreateMessage(name: PassUtility.GetElementName(messageSpecification));
            _messages[messageSpecification] = message;

            GetDefinitions()?.RootElements.Add(message);
        }

        return message;
    }

    private IEscalation GetOrCreateMacroCallEscalation(IMacroBehavior macroBehavior)
    {
        if (!_macroCallEscalations.TryGetValue(macroBehavior, out IEscalation? escalation))
        {
            escalation = BpmnUtility.CreateEscalation();
            _macroCallEscalations[macroBehavior] = escalation;

            GetDefinitions()?.RootElements.Add(escalation);
        }

        return escalation;
    }

    private ISignal GetOrCreateMacroReturnToOriginSignal(IMacroBehavior macroBehavior)
    {
        if (!_macroReturnToOriginSignals.TryGetValue(macroBehavior, out ISignal? signal))
        {
            // TODO: add name?
            signal = BpmnUtility.CreateSignal(name: $"Return to origin");
            _macroReturnToOriginSignals[macroBehavior] = signal;

            GetDefinitions()?.RootElements.Add(signal);
        }

        return signal;
    }

    private bool IsBaseBehavior(ISubjectBehavior subjectBehavior)
    {
        return subjectBehavior.getSubject() is IFullySpecifiedSubject fullySpecifiedSubject && subjectBehavior == fullySpecifiedSubject.getSubjectBaseBehavior();
    }

    // TODO: fix in alps.net.api
    private IEnumerable<IBehaviorDescribingComponent> GetBehaviorDescribingComponents(ISubjectBehavior subjectBehavior)
    {
        IEnumerable<IBehaviorDescribingComponent> behaviorDescribingComponents = subjectBehavior.getBehaviorDescribingComponents().Values;

        if (subjectBehavior is IGuardBehavior guardBehavior)
        {
            // the elements of the guarded behaviors are also contained in the behavior describing components of the buard behavior.
            // therefore, we have to filter them out first.
            // not sure if this is intended or a bug of alps.net.api.
            IEnumerable<IBehaviorDescribingComponent> guardedBehaviorsBehaviorDescribingComponents = guardBehavior.getGuardedBehaviors().Values.SelectMany(behavior => behavior.getBehaviorDescribingComponents().Values);
            behaviorDescribingComponents = behaviorDescribingComponents.Except(guardedBehaviorsBehaviorDescribingComponents);
        }

        return behaviorDescribingComponents;
    }

    private ICollection<T> Query<T>(Func<T, bool>? selector = null) where T : IElement
    {
        if (_graph == null)
            return [];

        HashSet<T> elements = new HashSet<T>();

        Stack<IElement> stack = new Stack<IElement>([_graph]);

        while (stack.TryPop(out IElement? element))
        {
            if (element is T specificElement && (selector == null || selector.Invoke(specificElement)))
                elements.Add(specificElement);

            if (element is IGraph graph)
            {
                foreach (IElement childElement in graph.Elements)
                {
                    stack.Push(childElement);
                }
            }
        }

        return elements;
    }

    private ICollection<(TElement, IGraph)> QueryWithGraph<TElement>(Func<TElement, bool>? selector = null) where TElement : IElement
    {
        if (_graph == null)
            return [];

        HashSet<(TElement, IGraph)> elements = new HashSet<(TElement, IGraph)>();

        Stack<IGraph> stack = new Stack<IGraph>([_graph]);

        while (stack.TryPop(out IGraph? graph))
        {
            foreach (IElement element in graph.Elements)
            {
                if (element is TElement specificElement && (selector == null || selector.Invoke(specificElement)))
                {
                    elements.Add((specificElement, graph));
                }

                if (element is IGraph childGraph)
                {
                    stack.Push(childGraph);
                }
            }

        }

        return elements;
    }

    private T? FindElementInGraph<T>(Func<T, bool> selector, IGraph graph) where T : IElement
    {
        foreach (IElement element in graph.Elements)
        {
            if (element is T specificElement && selector.Invoke(specificElement))
                return specificElement;
        }

        return default;
    }

    private T? FindElementInGraphRecursive<T>(Func<T, bool> selector) where T : IElement
    {
        if (_graph == null)
            return default;

        Stack<IElement> stack = new Stack<IElement>([_graph]);

        while (stack.TryPop(out IElement? element))
        {
            if (element is T specificElement && (selector == null || selector.Invoke(specificElement)))
            {
                return specificElement;
            }

            if (element is IGraph graph)
            {
                foreach (IElement childElement in graph.Elements)
                {
                    stack.Push(childElement);
                }
            }
        }

        return default;
    }

    // TODO: this is horribly inefficient
    private IGraph? GetGraph(IElement element)
    {
        return FindElementInGraphRecursive<IGraph>(graph => graph.Elements.Contains(element));
    }

    // TODO: this is horribly inefficient. perhaps add incoming and outgoing edge collections to INode or limit the search to a specified graph.
    public ICollection<IEdge> GetIncomingEdges(INode node)
    {
        return Query<IEdge>(edge => edge.Target == node);
    }

    public ICollection<IEdge> GetOutgoingEdges(INode node)
    {
        return Query<IEdge>(edge => edge.Source == node);
    }

    private void InsertNodeBefore(INode node, IEdge edge)
    {
        IGraph? graph = GetGraph(edge);

        if (graph == null)
            throw new InvalidOperationException($"{nameof(INode)} is not part of any graph.");

        graph.Elements.Add(node);

        IEdge edgeBefore = new Edge()
        {
            Source = edge.Source,
            Target = node,
        };
        graph.Elements.Add(edgeBefore);

        edge.Source = node;
    }

    private void InsertNodeAfter(INode node, IEdge edge)
    {
        IGraph? graph = GetGraph(edge);

        if (graph == null)
            throw new InvalidOperationException($"{nameof(INode)} is not part of any graph.");

        graph.Elements.Add(node);

        IEdge edgeBefore = new Edge()
        {
            Source = node,
            Target = edge.Target,
        };
        graph.Elements.Add(edgeBefore);

        edge.Target = node;
    }

    private void InsertNodeBefore(INode node, INode target)
    {
        ICollection<IEdge> incomingEdges = GetIncomingEdges(target);

        PrependNode(node, target);

        foreach (IEdge edge in incomingEdges)
        {
            edge.Target = node;
        }
    }

    private void InsertNodeAfter(INode node, INode target)
    {
        ICollection<IEdge> outgoingEdges = GetOutgoingEdges(target);

        AppendNode(node, target);

        foreach (IEdge edge in outgoingEdges)
        {
            edge.Source = node;
        }
    }

    private void PrependNode(INode node, INode target)
    {
        IGraph? graph = GetGraph(target);

        if (graph == null)
            throw new InvalidOperationException($"{nameof(INode)} is not part of any graph.");

        graph.Elements.Add(node);

        IEdge edge = new Edge()
        {
            Source = node,
            Target = target,
        };
        graph.Elements.Add(edge);
    }

    private void AppendNode(INode node, INode source)
    {
        IGraph? graph = GetGraph(source);

        if (graph == null)
            throw new InvalidOperationException($"{nameof(INode)} is not part of any graph.");

        graph.Elements.Add(node);

        IEdge edge = new Edge()
        {
            Source = source,
            Target = node,
        };
        graph.Elements.Add(edge);
    }

    private void AddElementToGraph(IElement element, IGraph graph)
    {
        graph.Elements.Add(element);
    }

    private void RemoveElement(IElement element)
    {
        IGraph? graph = GetGraph(element);

        if (graph == null)
            throw new InvalidOperationException($"{nameof(IElement)} is not part of any graph.");

        graph.Elements.Remove(element);
    }

    #endregion
}