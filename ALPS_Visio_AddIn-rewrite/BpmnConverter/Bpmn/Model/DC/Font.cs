#nullable enable
namespace PassBpmnConverter.Bpmn.DC;

public interface IFont
{
    string? Name { get; set; }

    double? Size { get; set; }

    bool? IsBold { get; set; }

    bool? IsItalic { get; set; }

    bool? IsUnderline { get; set; }

    bool? IsStrikeThrough { get; set; }
}

[BpmnType("Font", BpmnModelConstants.OmgDcNs)]
public class Font : IFont
{
    [BpmnAttribute("name")]
    public string? Name { get; set; }

    [BpmnAttribute("size")]
    public double? Size { get; set; }

    [BpmnAttribute("isBold")]
    public bool? IsBold { get; set; }

    [BpmnAttribute("isItalic")]
    public bool? IsItalic { get; set; }

    [BpmnAttribute("isUnderline")]
    public bool? IsUnderline { get; set; }

    [BpmnAttribute("isStrikeThrough")]
    public bool? IsStrikeThrough { get; set; }
}