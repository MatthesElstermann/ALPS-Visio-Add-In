#nullable enable
namespace PassBpmnConverter.Bpmn.DI;

public interface ILabeledEdge : IEdge
{
}

[BpmnType("LabeledEdge", BpmnModelConstants.OmgDiNs)]
public abstract class LabeledEdge : Edge, ILabeledEdge
{
}