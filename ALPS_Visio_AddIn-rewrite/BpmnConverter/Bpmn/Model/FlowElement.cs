#nullable enable
namespace PassBpmnConverter.Bpmn;

public interface IFlowElement : IBaseElement
{
    string? Name { get; set; }
}

[BpmnType("flowElement", BpmnModelConstants.BpmnNs)]
public abstract class FlowElement : BaseElement, IFlowElement
{
    [BpmnAttribute("name")]
    public string? Name { get; set; }
}