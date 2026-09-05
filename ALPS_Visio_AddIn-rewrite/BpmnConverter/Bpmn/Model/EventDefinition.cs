#nullable enable
namespace PassBpmnConverter.Bpmn;

public interface IEventDefinition : IRootElement
{
}

[BpmnType("eventDefinition", BpmnModelConstants.BpmnNs)]
public abstract class EventDefinition : RootElement, IEventDefinition
{
}