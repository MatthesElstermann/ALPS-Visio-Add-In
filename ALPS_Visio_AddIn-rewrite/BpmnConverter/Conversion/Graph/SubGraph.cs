#nullable enable
using System.Collections.Generic;
using alps.net.api.StandardPASS;
using PassBpmnConverter.Bpmn;

namespace PassBpmnConverter.Conversion;

public interface ISubGraph : INode, IGraph
{
}

public class SubGraph : Node, ISubGraph
{
    public IList<IElement> Elements { get; set; } = new List<IElement>();

    public IPASSProcessModelElement? PassElement { get; set; }
    public IBaseElement? BpmnElement { get; set; }
}