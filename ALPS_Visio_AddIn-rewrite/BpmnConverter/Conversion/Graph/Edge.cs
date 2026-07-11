#nullable enable
using alps.net.api.StandardPASS;
using PassBpmnConverter.Bpmn;

namespace PassBpmnConverter.Conversion;

public interface IEdge : IElement
{
    INode Source { get; set; }
    INode Target { get; set; }
    ITransition? Transition { get; set; }
    ISequenceFlow? SequenceFlow { get; set; }
}

public class Edge : Element, IEdge
{
    public required INode Source { get; set; }
    public required INode Target { get; set; }
    public ITransition? Transition { get; set; }
    public ISequenceFlow? SequenceFlow { get; set; }
}