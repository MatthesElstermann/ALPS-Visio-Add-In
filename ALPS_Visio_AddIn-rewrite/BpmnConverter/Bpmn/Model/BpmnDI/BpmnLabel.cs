#nullable enable
using PassBpmnConverter.Bpmn.DI;

namespace PassBpmnConverter.Bpmn.BpmnDI;

public interface IBpmnLabel : ILabel
{
    IBpmnLabelStyle? LabelStyle { get; set; }
}

[BpmnType("BPMNLabel", BpmnModelConstants.BpmnDiNs)]
public class BpmnLabel : Label, IBpmnLabel
{
    [BpmnAttribute("labelStyle")]
    public IBpmnLabelStyle? LabelStyle { get; set; }
}