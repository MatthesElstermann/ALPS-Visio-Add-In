#nullable enable

using System.Collections.Generic;
namespace PassBpmnConverter.Bpmn;

// TODO: add additional attributes/elements (e.g., isClosed, isExecutable)
public interface IProcess : ICallableElement, IFlowElementsContainer
{
}

[BpmnType("process", BpmnModelConstants.BpmnNs)]
public class Process : CallableElement, IProcess
{
    [BpmnElement]
    public IList<IFlowElement> FlowElements { get; set; } = new List<IFlowElement>();
}