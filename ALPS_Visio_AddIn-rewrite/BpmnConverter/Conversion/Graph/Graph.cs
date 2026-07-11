#nullable enable
using System.Collections.Generic;
using alps.net.api.StandardPASS;
using PassBpmnConverter.Bpmn;

namespace PassBpmnConverter.Conversion;

public interface IGraph : IElement
{
    IList<IElement> Elements { get; set; }

    IPASSProcessModelElement? PassElement { get; set; }
    IBaseElement? BpmnElement { get; set; }
}

public class Graph : Element, IGraph
{
    public IList<IElement> Elements { get; set; } = new List<IElement>();

    public IPASSProcessModelElement? PassElement { get; set; }
    public IBaseElement? BpmnElement { get; set; }
}