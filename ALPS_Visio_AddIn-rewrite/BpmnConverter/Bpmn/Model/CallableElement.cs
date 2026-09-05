#nullable enable
namespace PassBpmnConverter.Bpmn;

public interface ICallableElement : IRootElement
{
    string? Name { get; set; }
}

[BpmnType("callableElement", BpmnModelConstants.BpmnNs)]
public abstract class CallableElement : RootElement, ICallableElement
{
    [BpmnAttribute("name")]
    public string? Name { get; set; }
}