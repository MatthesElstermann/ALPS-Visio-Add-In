#nullable enable
namespace PassBpmnConverter.Bpmn;

public interface IMessage : IRootElement
{
    public string? Name { get; set; }
}

[BpmnType("message", BpmnModelConstants.BpmnNs)]
public class Message : RootElement, IMessage
{
    [BpmnAttribute("name")]
    public string? Name { get; set; }
}