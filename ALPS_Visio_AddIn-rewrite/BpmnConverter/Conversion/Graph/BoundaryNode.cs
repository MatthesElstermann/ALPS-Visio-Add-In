#nullable enable
namespace PassBpmnConverter.Conversion;

public interface IBoundaryNode : INode
{
}

public class BoundaryNode : Node, IBoundaryNode
{
}