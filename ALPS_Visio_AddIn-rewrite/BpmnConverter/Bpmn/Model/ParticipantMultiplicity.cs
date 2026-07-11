#nullable enable
namespace PassBpmnConverter.Bpmn;

public interface IParticipantMultiplicity : IBaseElement
{
    int Minimum { get; set; }

    int? Maximum { get; set; }
}

[BpmnType("participantMultiplicity", BpmnModelConstants.BpmnNs)]
public class ParticipantMultiplicity : BaseElement, IParticipantMultiplicity
{
    [BpmnAttribute("minimum")]
    public int Minimum { get; set; } = 0;

    // TODO: ensure Maximum is always >= 1 and >= Minimum
    // TODO: add support for unbound/inf
    [BpmnAttribute("maximum")]
    public int? Maximum { get; set; } = 1;
}