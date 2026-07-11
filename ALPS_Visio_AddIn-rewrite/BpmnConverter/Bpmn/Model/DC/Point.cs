#nullable enable
namespace PassBpmnConverter.Bpmn.DC;

public interface IPoint
{
    double X { get; set; }
    double Y { get; set; }
}

[BpmnType("Point", BpmnModelConstants.OmgDcNs)]
public class Point : IPoint
{
    [BpmnAttribute("x")]
    public required double X { get; set; }

    [BpmnAttribute("y")]
    public required double Y { get; set; }
}