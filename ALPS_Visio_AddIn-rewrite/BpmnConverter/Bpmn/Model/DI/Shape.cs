#nullable enable
using PassBpmnConverter.Bpmn.DC;

namespace PassBpmnConverter.Bpmn.DI;

public interface IShape : INode
{
    IBounds? Bounds { get; set; }
}

[BpmnType("Shape", BpmnModelConstants.OmgDiNs)]
public abstract class Shape : Node, IShape
{
    [BpmnElement]
    public IBounds? Bounds { get; set; }
}