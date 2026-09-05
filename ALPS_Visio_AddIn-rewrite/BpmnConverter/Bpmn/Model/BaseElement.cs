#nullable enable
namespace PassBpmnConverter.Bpmn;

public interface IBaseElement
{
    // TODO: consider making the id required
    string? Id { get; set; }
}

[BpmnType("baseElement", BpmnModelConstants.BpmnNs)]
public abstract class BaseElement : IBaseElement
{
    [BpmnAttribute("id")]
    public string? Id { get; set; }

    public override string ToString()
    {
        return Id?.ToString() ?? string.Empty;
    }
}