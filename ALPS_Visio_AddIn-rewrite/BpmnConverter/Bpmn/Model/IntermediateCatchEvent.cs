#nullable enable
namespace PassBpmnConverter.Bpmn;

public interface IIntermediateCatchEvent : ICatchEvent
{
}

[BpmnType("intermediateCatchEvent", BpmnModelConstants.BpmnNs)]
public class IntermediateCatchEvent : CatchEvent, IIntermediateCatchEvent
{
}