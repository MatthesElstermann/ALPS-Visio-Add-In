#nullable enable
namespace PassBpmnConverter.Bpmn.DC;

public interface IBounds
{
    double X { get; set; }

    double Y { get; set; }

    double Width { get; set; }

    double Height { get; set; }
}

[BpmnType("Bounds", BpmnModelConstants.OmgDcNs)]
public class Bounds : IBounds
{
    [BpmnAttribute("x")]
    public required double X { get; set; }

    [BpmnAttribute("y")]
    public required double Y { get; set; }

    [BpmnAttribute("width")]
    public required double Width { get; set; }

    [BpmnAttribute("height")]
    public required double Height { get; set; }
}