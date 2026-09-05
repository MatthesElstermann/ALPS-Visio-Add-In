#nullable enable
namespace PassBpmnConverter.Bpmn;

public interface IStartEvent : ICatchEvent
{
    bool IsInterrupting { get; set; }
}

[BpmnType("startEvent", BpmnModelConstants.BpmnNs)]
public class StartEvent : CatchEvent, IStartEvent
{
    [BpmnAttribute("isInterrupting")]
    public bool IsInterrupting { get; set; } = true;
}