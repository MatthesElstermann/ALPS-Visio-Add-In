#nullable enable
using PassBpmnConverter.Bpmn.DI;

namespace PassBpmnConverter.Bpmn.BpmnDI;

public interface IBpmnShape : ILabeledShape
{
    IBpmnLabel? BpmnLabel { get; set; }

    IBaseElement? BpmnElement { get; set; }

    bool? IsHorizontal { get; set; }

    bool? IsExpanded { get; set; }

    bool? IsMarkerVisible { get; set; }
}

[BpmnType("BPMNShape", BpmnModelConstants.BpmnDiNs)]
public class BpmnShape : LabeledShape, IBpmnShape
{
    [BpmnElement]
    public IBpmnLabel? BpmnLabel { get; set; }

    [BpmnAttribute("bpmnElement")]
    public IBaseElement? BpmnElement { get; set; }

    [BpmnAttribute("isHorizontal")]
    public bool? IsHorizontal { get; set; }

    [BpmnAttribute("isExpanded")]
    public bool? IsExpanded { get; set; }

    [BpmnAttribute("isMarkerVisible")]
    public bool? IsMarkerVisible { get; set; }
}