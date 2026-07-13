#nullable enable
using System;
using System.Text;
namespace PassBpmnConverter.Bpmn;

public static class BpmnUtility
{
    // TODO: use element prefix
    public static string GenerateUniqueIdentifier()
    {
        return '_' + Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Macht aus freiem Text eine gueltige XML-ID (NCName): BPMN-ids muessen mit
    /// Buchstabe/Unterstrich beginnen und duerfen nur Buchstaben, Ziffern, '.', '-'
    /// und '_' enthalten. Der Modellname landet als Definitions-id in der Datei —
    /// Namen wie "[Test]_Escaping_Quotes" liessen bpmn.io sonst mit
    /// "illegal ID" beim Parsen aussteigen.
    /// </summary>
    public static string SanitizeNcName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return GenerateUniqueIdentifier();

        var sb = new StringBuilder(value!.Length + 1);
        if (!(char.IsLetter(value[0]) || value[0] == '_'))
            sb.Append('_');
        foreach (char c in value)
            sb.Append(char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.' ? c : '_');
        return sb.ToString();
    }

    public static IBpmnModel CreateModel(string? id = null)
    {
        IBpmnModel bpmnModel = new BpmnModel()
        {
            Id = id
        };
        return bpmnModel;
    }

    public static IDefinitions CreateDefinitions(string? id = null)
    {
        IDefinitions definitions = new Definitions()
        {
            Id = id == null ? GenerateUniqueIdentifier() : SanitizeNcName(id),
            // TODO: change TargetNamespace to something useful
            TargetNamespace = "PassBpmnConverter",
        };
        return definitions;
    }

    public static ICollaboration CreateCollaboration(string? id = null)
    {
        ICollaboration collaboration = new Collaboration()
        {
            Id = id ?? GenerateUniqueIdentifier()
        };
        return collaboration;
    }

    public static IParticipant CreateParticipant(string? id = null, string? name = null)
    {
        IParticipant participant = new Participant()
        {
            Id = id ?? GenerateUniqueIdentifier(),
            Name = name
        };
        return participant;
    }

    public static IProcess CreateProcess(string? id = null, string? name = null)
    {
        IProcess process = new Process()
        {
            Id = id ?? GenerateUniqueIdentifier(),
            Name = name
        };
        return process;
    }

    public static ISubProcess CreateSubProcess(string? id = null, string? name = null, bool triggeredByEvent = false)
    {
        ISubProcess subProcess = new SubProcess()
        {
            Id = id ?? GenerateUniqueIdentifier(),
            Name = name,
            TriggeredByEvent = triggeredByEvent
        };
        return subProcess;
    }

    public static ISequenceFlow CreateSequenceFlow(IFlowNode source, IFlowNode target, string? id = null, string? name = null)
    {
        ISequenceFlow sequenceFlow = new SequenceFlow()
        {
            Id = id ?? GenerateUniqueIdentifier(),
            Name = name,
            SourceRef = source,
            TargetRef = target,
        };
        return sequenceFlow;
    }

    public static ITask CreateAbstractTask(string? id = null, string? name = null)
    {
        ITask abstractTask = new Task()
        {
            Id = id ?? GenerateUniqueIdentifier(),
            Name = name
        };
        return abstractTask;
    }

    public static ISendTask CreateSendTask(string? id = null, string? name = null, IMessage? message = null)
    {
        ISendTask sendTask = new SendTask()
        {
            Id = id ?? GenerateUniqueIdentifier(),
            Name = name,
            MessageRef = message
        };
        return sendTask;
    }

    public static IReceiveTask CreateReceiveTask(string? id = null, string? name = null, IMessage? message = null)
    {
        IReceiveTask receiveTask = new ReceiveTask()
        {
            Id = id ?? GenerateUniqueIdentifier(),
            Name = name,
            MessageRef = message
        };
        return receiveTask;
    }

    public static IScriptTask CreateScripTask(string? id = null, string? name = null)
    {
        IScriptTask scriptTask = new ScriptTask()
        {
            Id = id ?? GenerateUniqueIdentifier(),
            Name = name
        };
        return scriptTask;
    }

    public static IStartEvent CreateStartEvent(IEventDefinition? eventDefinition = null, string? id = null, string? name = null, bool isInterrupting = true)
    {
        IStartEvent startEvent = new StartEvent()
        {
            Id = id ?? GenerateUniqueIdentifier(),
            Name = name,
            IsInterrupting = isInterrupting
        };

        if (eventDefinition != null)
        {
            startEvent.EventDefinitions.Add(eventDefinition);
        }

        return startEvent;
    }

    public static IEndEvent CreateEndEvent(IEventDefinition? eventDefinition = null, string? id = null, string? name = null)
    {
        IEndEvent endEvent = new EndEvent()
        {
            Id = id ?? GenerateUniqueIdentifier(),
            Name = name
        };

        if (eventDefinition != null)
        {
            endEvent.EventDefinitions.Add(eventDefinition);
        }

        return endEvent;
    }

    public static IIntermediateCatchEvent CreateIntermediateCatchEvent(IEventDefinition? eventDefinition = null, string? id = null, string? name = null)
    {
        IIntermediateCatchEvent intermediateCatchEvent = new IntermediateCatchEvent()
        {
            Id = id ?? GenerateUniqueIdentifier(),
            Name = name
        };

        if (eventDefinition != null)
        {
            intermediateCatchEvent.EventDefinitions.Add(eventDefinition);
        }

        return intermediateCatchEvent;
    }

    public static IBoundaryEvent CreateBoundaryEvent(IActivity attachTo, IEventDefinition? eventDefinition = null, string? id = null, string? name = null)
    {
        IBoundaryEvent boundaryEvent = new BoundaryEvent()
        {
            Id = id ?? GenerateUniqueIdentifier(),
            Name = name,
            AttachedToRef = attachTo,
            CancelActivity = true,
        };

        if (eventDefinition != null)
        {
            boundaryEvent.EventDefinitions.Add(eventDefinition);
        }

        return boundaryEvent;
    }

    public static IIntermediateThrowEvent CreateIntermediateThrowEvent(IEventDefinition? eventDefinition = null, string? id = null, string? name = null)
    {
        IIntermediateThrowEvent intermediateThrowEvent = new IntermediateThrowEvent()
        {
            Id = id ?? GenerateUniqueIdentifier(),
            Name = name
        };

        if (eventDefinition != null)
        {
            intermediateThrowEvent.EventDefinitions.Add(eventDefinition);
        }

        return intermediateThrowEvent;
    }

    public static IMessageEventDefinition CreateMessageEventDefinition(string? id = null, IMessage? message = null)
    {
        IMessageEventDefinition messageEventDefinition = new MessageEventDefinition()
        {
            Id = id ?? GenerateUniqueIdentifier(),
            MessageRef = message
        };
        return messageEventDefinition;
    }

    public static ISignalEventDefinition CreateSignalEventDefinition(string? id = null, ISignal? signal = null)
    {
        ISignalEventDefinition signalEventDefinition = new SignalEventDefinition()
        {
            Id = id ?? GenerateUniqueIdentifier(),
            SignalRef = signal
        };
        return signalEventDefinition;
    }

    public static IEscalationEventDefinition CreateEscalationEventDefinition(string? id = null, IEscalation? escalation = null)
    {
        IEscalationEventDefinition escalationEventDefinition = new EscalationEventDefinition()
        {
            Id = id ?? GenerateUniqueIdentifier(),
            Escalation = escalation
        };
        return escalationEventDefinition;
    }

    public static IConditionalEventDefinition CreateConditionalEventDefinition(string? id = null)
    {
        IConditionalEventDefinition conditionalEventDefinition = new ConditionalEventDefinition()
        {
            Id = id ?? GenerateUniqueIdentifier(),
            // TODO: implementation
            Condition = new Expression()
        };
        return conditionalEventDefinition;
    }

    public static ITimerEventDefinition CreateTimerEventDefinition(string? id = null)
    {
        ITimerEventDefinition timerEventDefinition = new TimerEventDefinition()
        {
            Id = id ?? GenerateUniqueIdentifier()
            // TODO: implementation
            // TimeDate = new Expression()
            // TimeDuration = new Expression()
            // TimeCycle = new Expression()
        };
        return timerEventDefinition;
    }

    public static IErrorEventDefinition CreateErrorEventDefinition(string? id = null)
    {
        IErrorEventDefinition errorEventDefinition = new ErrorEventDefinition()
        {
            Id = id ?? GenerateUniqueIdentifier()
        };
        return errorEventDefinition;
    }

    public static IExclusiveGateway CreateExclusiveGateway(string? id = null, string? name = null)
    {
        IExclusiveGateway exclusiveGateway = new ExclusiveGateway()
        {
            Id = id ?? GenerateUniqueIdentifier(),
            Name = name
        };
        return exclusiveGateway;
    }

    public static IEventBasedGateway CreateEventBasedGateway(string? id = null, string? name = null)
    {
        IEventBasedGateway eventBasedGateway = new EventBasedGateway()
        {
            Id = id ?? GenerateUniqueIdentifier(),
            Name = name
        };
        return eventBasedGateway;
    }

    public static IEscalation CreateEscalation(string? id = null, string? name = null, string? escalationCode = null)
    {
        IEscalation escalation = new Escalation()
        {
            Id = id ?? GenerateUniqueIdentifier(),
            Name = name ?? GenerateUniqueIdentifier(),
            EscalationCode = escalationCode ?? GenerateUniqueIdentifier()
        };
        return escalation;
    }

    public static ISignal CreateSignal(string? id = null, string? name = null)
    {
        ISignal signal = new Signal()
        {
            Id = id ?? GenerateUniqueIdentifier(),
            Name = name
        };
        return signal;
    }

    public static IMessage CreateMessage(string? id = null, string? name = null)
    {
        IMessage message = new Message()
        {
            Id = id ?? GenerateUniqueIdentifier(),
            Name = name
        };
        return message;
    }
}