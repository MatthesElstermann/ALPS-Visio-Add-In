#nullable enable
namespace PassBpmnConverter.Bpmn;

public interface IActivity : IFlowNode, IInteractionNode
{
}

[BpmnType("activity", BpmnModelConstants.BpmnNs)]
public abstract class Activity : FlowNode, IActivity
{
}