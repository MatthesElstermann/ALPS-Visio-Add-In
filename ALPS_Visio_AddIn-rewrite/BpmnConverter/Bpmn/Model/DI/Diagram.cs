#nullable enable
namespace PassBpmnConverter.Bpmn.DI;

public interface IDiagram
{
    string? Name { get; set; }

    double? Resolution { get; set; }

    string? Id { get; set; }
}

[BpmnType("Diagram", BpmnModelConstants.OmgDiNs)]
public abstract class Diagram : IDiagram
{
    [BpmnAttribute("name")]
    public string? Name { get; set; }

    [BpmnAttribute("resolution")]
    public double? Resolution { get; set; }

    [BpmnAttribute("id")]
    public string? Id { get; set; }
}