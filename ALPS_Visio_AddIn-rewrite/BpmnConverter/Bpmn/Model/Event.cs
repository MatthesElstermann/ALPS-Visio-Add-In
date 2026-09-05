#nullable enable
namespace PassBpmnConverter.Bpmn;

public interface IEvent : IFlowNode, IInteractionNode
{
}

[BpmnType("event", BpmnModelConstants.BpmnNs)]
public abstract class Event : FlowNode, IEvent
{
}