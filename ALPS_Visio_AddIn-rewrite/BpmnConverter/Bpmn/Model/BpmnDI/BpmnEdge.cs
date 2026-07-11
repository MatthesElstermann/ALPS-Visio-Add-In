#nullable enable
using PassBpmnConverter.Bpmn.DI;

namespace PassBpmnConverter.Bpmn.BpmnDI;

public interface IBpmnEdge : ILabeledEdge
{
    IBpmnLabel? BpmnLabel { get; set; }

    IBaseElement? BpmnElement { get; set; }

    IDiagramElement? SourceElement { get; set; }

    IDiagramElement? TargetElement { get; set; }

    MessageVisibleKind? MessageVisibleKind { get; set; }
}

[BpmnType("BPMNEdge", BpmnModelConstants.BpmnDiNs)]
public class BpmnEdge : LabeledEdge, IBpmnEdge
{
    [BpmnElement]
    public IBpmnLabel? BpmnLabel { get; set; }

    [BpmnAttribute("bpmnElement")]
    // TODO: make this required
    public IBaseElement? BpmnElement { get; set; }

    [BpmnAttribute("sourceElement")]
    public IDiagramElement? SourceElement { get; set; }

    [BpmnAttribute("targetElement")]
    public IDiagramElement? TargetElement { get; set; }

    [BpmnAttribute("messageVisibleKind")]
    public MessageVisibleKind? MessageVisibleKind { get; set; }
}