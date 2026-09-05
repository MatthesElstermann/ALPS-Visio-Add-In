#nullable enable
namespace PassBpmnConverter.Bpmn.DI;

public interface IStyle
{
    string? Id { get; set; }
}

[BpmnType("Style", BpmnModelConstants.OmgDiNs)]
public abstract class Style : IStyle
{
    [BpmnAttribute("id")]
    public string? Id { get; set; }
}