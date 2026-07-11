#nullable enable
using System.Collections.Generic;
namespace PassBpmnConverter.Bpmn;

public interface ICatchEvent : IEvent
{
    IList<IEventDefinition> EventDefinitions { get; set; }
}

[BpmnType("catchEvent", BpmnModelConstants.BpmnNs)]
public abstract class CatchEvent : Event, ICatchEvent
{
    [BpmnElement]
    public IList<IEventDefinition> EventDefinitions { get; set; } = new List<IEventDefinition>();
}