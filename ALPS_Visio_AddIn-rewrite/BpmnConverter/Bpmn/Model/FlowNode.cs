#nullable enable
using System.Collections.Generic;
using System.Linq;
namespace PassBpmnConverter.Bpmn;

public interface IFlowNode : IFlowElement
{
    IList<ISequenceFlow> Incoming { get; set; }

    IList<ISequenceFlow> Outgoing { get; set; }
}

[BpmnType("flowNode", BpmnModelConstants.BpmnNs)]
public abstract class FlowNode : FlowElement, IFlowNode
{
    public IList<ISequenceFlow> Incoming { get; set; } = new List<ISequenceFlow>();

    public IList<ISequenceFlow> Outgoing { get; set; } = new List<ISequenceFlow>();

    // TODO: improve null/error handling
    [BpmnElement("incoming", BpmnModelConstants.BpmnNs)]
    public IEnumerable<string> IncomingIds => Incoming.Select(sequenceFlow => sequenceFlow?.Id);

    [BpmnElement("outgoing", BpmnModelConstants.BpmnNs)]
    public IEnumerable<string> OutgoingIds => Outgoing.Select(sequenceFlow => sequenceFlow?.Id);
}