#nullable enable
namespace PassBpmnConverter.Bpmn;

public interface IParticipant : IBaseElement, IInteractionNode
{
    string? Name { get; set; }

    IParticipantMultiplicity? ParticipantMultiplicity { get; set; }

    IProcess? ProcessRef { get; set; }
}

[BpmnType("participant", BpmnModelConstants.BpmnNs)]
public class Participant : BaseElement, IParticipant
{
    [BpmnAttribute("name")]
    public string? Name { get; set; }

    [BpmnElement]
    public IParticipantMultiplicity? ParticipantMultiplicity { get; set; }

    [BpmnAttribute("processRef")]
    public IProcess? ProcessRef { get; set; }
}