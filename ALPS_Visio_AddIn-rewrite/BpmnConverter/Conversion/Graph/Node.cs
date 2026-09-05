#nullable enable
using alps.net.api.StandardPASS;
using PassBpmnConverter.Bpmn;

namespace PassBpmnConverter.Conversion;

public interface INode : IElement
{
    IState? State { get; set; }
    IFlowNode? FlowNode { get; set; }
}

public class Node : Element, INode
{
    public IState? State { get; set; }
    public IFlowNode? FlowNode { get; set; }
}