#nullable enable
using System.Collections.Generic;
namespace PassBpmnConverter.Bpmn.DI;

public interface IPlane : INode
{
    IList<IDiagramElement> DiagramElements { get; set; }
}

[BpmnType("Plane", BpmnModelConstants.OmgDiNs)]
public abstract class Plane : Node, IPlane
{
    [BpmnElement]
    public IList<IDiagramElement> DiagramElements { get; set; } = new List<IDiagramElement>();
}