#nullable enable
using PassBpmnConverter.Bpmn.DC;
using PassBpmnConverter.Bpmn.DI;

namespace PassBpmnConverter.Bpmn.BpmnDI;

public interface IBpmnLabelStyle : IStyle
{
    IFont? Font { get; set; }
}

[BpmnType("BPMNLabelStyle", BpmnModelConstants.BpmnDiNs)]
public class BpmnLabelStyle : Style, IBpmnLabelStyle
{
    [BpmnElement]
    public IFont? Font { get; set; }
}