#nullable enable
using System.Collections.Generic;
using PassBpmnConverter.Bpmn.DI;

namespace PassBpmnConverter.Bpmn.BpmnDI;

public interface IBpmnDiagram : IDiagram
{
    IBpmnPlane BpmnPlane { get; set; }

    IList<IBpmnLabelStyle> BpmnLabelStyles { get; set; }
}

[BpmnType("BPMNDiagram", BpmnModelConstants.BpmnDiNs)]
public class BpmnDiagram : Diagram, IBpmnDiagram
{
    [BpmnElement]
    public required IBpmnPlane BpmnPlane { get; set; }

    [BpmnElement]
    public IList<IBpmnLabelStyle> BpmnLabelStyles { get; set; } = new List<IBpmnLabelStyle>();
}