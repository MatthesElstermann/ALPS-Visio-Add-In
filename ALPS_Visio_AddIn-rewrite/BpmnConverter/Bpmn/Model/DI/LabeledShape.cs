#nullable enable
namespace PassBpmnConverter.Bpmn.DI;

public interface ILabeledShape : IShape
{
}

[BpmnType("LabeledShape", BpmnModelConstants.OmgDiNs)]
public abstract class LabeledShape : Shape, ILabeledShape
{
}