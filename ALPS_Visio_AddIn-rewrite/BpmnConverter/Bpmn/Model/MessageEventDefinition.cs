#nullable enable
namespace PassBpmnConverter.Bpmn;

// TODO: add message property
public interface IMessageEventDefinition : IEventDefinition
{
    IMessage? MessageRef { get; set; }
}

[BpmnType("messageEventDefinition", BpmnModelConstants.BpmnNs)]
public class MessageEventDefinition : EventDefinition, IMessageEventDefinition
{
    public IMessage? MessageRef { get; set; }

    [BpmnAttribute("messageRef")]
    public string? SignalId => MessageRef?.ToString();

}