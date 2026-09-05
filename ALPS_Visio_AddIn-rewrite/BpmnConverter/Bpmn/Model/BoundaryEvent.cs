#nullable enable
namespace PassBpmnConverter.Bpmn;

public interface IBoundaryEvent : ICatchEvent
{
    bool CancelActivity { get; set; }

    IActivity AttachedToRef { get; set; }
}

[BpmnType("boundaryEvent", BpmnModelConstants.BpmnNs)]
public class BoundaryEvent : CatchEvent, IBoundaryEvent
{
    [BpmnAttribute("cancelActivity")]
    public bool CancelActivity { get; set; } = true;

    [BpmnAttribute("attachedToRef")]
    public required IActivity AttachedToRef { get; set; }
}