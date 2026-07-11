#nullable enable
using System.Collections.Generic;
namespace PassBpmnConverter.Bpmn;

public interface ISubProcess : IActivity, IFlowElementsContainer
{
    bool TriggeredByEvent { get; set; }
}

[BpmnType("subProcess", BpmnModelConstants.BpmnNs)]
public class SubProcess : Activity, ISubProcess
{
    [BpmnAttribute("triggeredByEvent")]
    public bool TriggeredByEvent { get; set; } = false;

    [BpmnElement]
    public IList<IFlowElement> FlowElements { get; set; } = new List<IFlowElement>();
}