#nullable enable
namespace PassBpmnConverter.Bpmn;

public interface IEndEvent : IThrowEvent
{
}

[BpmnType("endEvent", BpmnModelConstants.BpmnNs)]
public class EndEvent : ThrowEvent, IEndEvent
{
}