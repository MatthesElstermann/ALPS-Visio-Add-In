#nullable enable
namespace PassBpmnConverter.Bpmn;

public interface IRootElement : IBaseElement
{
}

[BpmnType("rootElement", BpmnModelConstants.BpmnNs)]
public abstract class RootElement : BaseElement, IRootElement
{
}