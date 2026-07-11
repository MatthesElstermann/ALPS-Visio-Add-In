#nullable enable
using System.Collections.Generic;
namespace PassBpmnConverter.Bpmn;

public interface IFlowElementsContainer : IBaseElement
{
    IList<IFlowElement> FlowElements { get; set; }
}

[BpmnType("flowElementsContainer", BpmnModelConstants.BpmnNs)]
public abstract class FlowElementsContainer : BaseElement, IFlowElementsContainer
{
    public IList<IFlowElement> FlowElements { get; set; } = new List<IFlowElement>();
}