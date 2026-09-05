#nullable enable
namespace PassBpmnConverter.Bpmn;

// TODO: add messageRef if necessary
public interface IMessageFlow : IBaseElement
{
    string? Name { get; set; }

    IInteractionNode SourceRef { get; set; }

    IInteractionNode TargetRef { get; set; }
}

[BpmnType("messageFlow", BpmnModelConstants.BpmnNs)]
public class MessageFlow : BaseElement, IMessageFlow
{
    [BpmnAttribute("name")]
    public string? Name { get; set; }

    [BpmnAttribute("sourceRef")]
    public required IInteractionNode SourceRef { get; set; }

    [BpmnAttribute("targetRef")]
    public required IInteractionNode TargetRef { get; set; }
}