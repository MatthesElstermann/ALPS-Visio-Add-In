#nullable enable
namespace PassBpmnConverter.Bpmn.DI;

public interface IDiagramElement
{
    string? Id { get; set; }
}

[BpmnType("DiagramElement", BpmnModelConstants.OmgDiNs)]
public abstract class DiagramElement : IDiagramElement
{
    [BpmnAttribute("id")]
    public string? Id { get; set; }

    public override string ToString()
    {
        return Id?.ToString() ?? string.Empty;
    }
}