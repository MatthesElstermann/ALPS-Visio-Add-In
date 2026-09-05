#nullable enable
namespace PassBpmnConverter.Bpmn;

public interface IIntermediateThrowEvent : IThrowEvent
{
}

[BpmnType("intermediateThrowEvent", BpmnModelConstants.BpmnNs)]
public class IntermediateThrowEvent : ThrowEvent, IIntermediateThrowEvent
{
}