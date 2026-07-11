#nullable enable
using PassBpmnConverter.Bpmn.DI;

namespace PassBpmnConverter.Bpmn.BpmnDI;

public interface IBpmnPlane : IPlane
{
    IBaseElement? BpmnElement { get; set; }
}

[BpmnType("BPMNPlane", BpmnModelConstants.BpmnDiNs)]
public class BpmnPlane : Plane, IBpmnPlane
{
    [BpmnAttribute("bpmnElement")]
    public IBaseElement? BpmnElement { get; set; }
}