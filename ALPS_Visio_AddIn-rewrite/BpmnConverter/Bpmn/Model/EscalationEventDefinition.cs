#nullable enable
namespace PassBpmnConverter.Bpmn;

public interface IEscalationEventDefinition : IEventDefinition
{
    IEscalation? Escalation { get; set; }
}

[BpmnType("escalationEventDefinition", BpmnModelConstants.BpmnNs)]
public class EscalationEventDefinition : EventDefinition, IEscalationEventDefinition
{
    public IEscalation? Escalation { get; set; }

    [BpmnAttribute("escalationRef")]
    public string? EscalationRef => Escalation?.Id;
}