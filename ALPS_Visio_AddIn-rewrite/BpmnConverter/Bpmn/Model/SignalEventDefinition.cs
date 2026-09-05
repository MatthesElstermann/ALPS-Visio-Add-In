#nullable enable
namespace PassBpmnConverter.Bpmn;

public interface ISignalEventDefinition : IEventDefinition
{
    ISignal? SignalRef { get; set; }
}

[BpmnType("signalEventDefinition", BpmnModelConstants.BpmnNs)]
public class SignalEventDefinition : EventDefinition, ISignalEventDefinition
{
    public ISignal? SignalRef { get; set; }

    [BpmnAttribute("signalRef")]
    public string? SignalId => SignalRef?.ToString();
}