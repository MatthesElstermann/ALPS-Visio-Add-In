#nullable enable
namespace PassBpmnConverter.Bpmn;

// TODO: add sources and target property
public interface ILinkEventDefinition : IEventDefinition
{
    // TODO: can this be optional?
    string Name { get; set; }
}

[BpmnType("linkEventDefinition", BpmnModelConstants.BpmnNs)]
public class LinkEventDefinition : EventDefinition, ILinkEventDefinition
{
    [BpmnAttribute("name")]
    public required string Name { get; set; }
}