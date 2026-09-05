#nullable enable
using System.Collections.Generic;
using PassBpmnConverter.Bpmn.DC;

namespace PassBpmnConverter.Bpmn.DI;

public interface IEdge : IDiagramElement
{
    IList<IPoint> Waypoints { get; set; }
}

[BpmnType("Edge", BpmnModelConstants.OmgDiNs)]
public abstract class Edge : DiagramElement, IEdge
{
    [BpmnElement("waypoint", BpmnModelConstants.OmgDiNs)]
    public IList<IPoint> Waypoints { get; set; } = new List<IPoint>();
}