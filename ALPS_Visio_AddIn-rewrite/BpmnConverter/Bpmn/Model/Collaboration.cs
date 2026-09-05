#nullable enable
using System.Collections.Generic;
namespace PassBpmnConverter.Bpmn;

public interface ICollaboration : IRootElement
{
    string? Name { get; set; }

    // TODO: can this be optional?
    bool IsClosed { get; set; }

    IList<IParticipant> Participants { get; set; }

    IList<IMessageFlow> MessageFlows { get; set; }
}

[BpmnType("collaboration", BpmnModelConstants.BpmnNs)]
public class Collaboration : RootElement, ICollaboration
{
    [BpmnAttribute("name")]
    public string? Name { get; set; }

    [BpmnAttribute("isClosed")]
    public bool IsClosed { get; set; } = false;

    [BpmnElement]
    public IList<IParticipant> Participants { get; set; } = new List<IParticipant>();

    [BpmnElement]
    public IList<IMessageFlow> MessageFlows { get; set; } = new List<IMessageFlow>();
}