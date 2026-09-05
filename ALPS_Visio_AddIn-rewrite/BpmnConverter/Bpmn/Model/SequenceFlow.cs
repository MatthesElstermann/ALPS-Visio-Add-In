#nullable enable
namespace PassBpmnConverter.Bpmn;

public interface ISequenceFlow : IFlowElement
{
    IExpression? Expression { get; set; }
    IFlowNode SourceRef { get; set; }
    IFlowNode TargetRef { get; set; }
}

[BpmnType("sequenceFlow", BpmnModelConstants.BpmnNs)]
public class SequenceFlow : FlowElement, ISequenceFlow
{
    [BpmnElement("conditionalExpression", BpmnModelConstants.BpmnNs)]
    public IExpression? Expression { get; set; }

    [BpmnAttribute("sourceRef")]
    public required IFlowNode SourceRef { get; set; }

    [BpmnAttribute("targetRef")]
    public required IFlowNode TargetRef { get; set; }
}