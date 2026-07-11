#nullable enable
using System.Collections.Generic;
namespace PassBpmnConverter.Bpmn;

public interface IThrowEvent : IEvent
{
    IList<IEventDefinition> EventDefinitions { get; set; }
}

[BpmnType("throwEvent", BpmnModelConstants.BpmnNs)]
public abstract class ThrowEvent : Event, IThrowEvent
{
    [BpmnElement]
    public IList<IEventDefinition> EventDefinitions { get; set; } = new List<IEventDefinition>();
}