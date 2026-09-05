#nullable enable
namespace PassBpmnConverter.Bpmn;

public interface IErrorEventDefinition : IEventDefinition
{
    // TODO: implementation
    // IError? Error { get; set; }
}

[BpmnType("errorEventDefinition", BpmnModelConstants.BpmnNs)]
public class ErrorEventDefinition : EventDefinition, IErrorEventDefinition
{
}