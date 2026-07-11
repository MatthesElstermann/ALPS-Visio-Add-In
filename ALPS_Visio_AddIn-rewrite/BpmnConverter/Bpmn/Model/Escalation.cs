#nullable enable
namespace PassBpmnConverter.Bpmn;

public interface IEscalation : IRootElement
{
    string Name { get; set; }
    // TODO: is this really optional?
    string? EscalationCode { get; set; }
}

[BpmnType("escalation", BpmnModelConstants.BpmnNs)]
public class Escalation : RootElement, IEscalation
{
    [BpmnAttribute("name")]
    public required string Name { get; set; }

    [BpmnAttribute("escalationCode")]
    public string? EscalationCode { get; set; }
}