#nullable enable
using PassBpmnConverter.Bpmn.DC;

namespace PassBpmnConverter.Bpmn.DI;

public interface ILabel : INode
{
    IBounds? Bounds { get; set; }
}

[BpmnType("Label", BpmnModelConstants.OmgDiNs)]
public abstract class Label : Node, ILabel
{
    [BpmnElement]
    public IBounds? Bounds { get; set; }
}