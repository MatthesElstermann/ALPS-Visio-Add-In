#nullable enable
namespace PassBpmnConverter.Bpmn.DI;

public interface INode : IDiagramElement
{
}

[BpmnType("Node", BpmnModelConstants.OmgDiNs)]
public abstract class Node : DiagramElement, INode
{
}