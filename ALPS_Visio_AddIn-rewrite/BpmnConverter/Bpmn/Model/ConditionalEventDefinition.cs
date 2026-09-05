#nullable enable
namespace PassBpmnConverter.Bpmn;

public interface IConditionalEventDefinition : IEventDefinition
{
    Expression Condition { get; set; }
}

[BpmnType("conditionalEventDefinition", BpmnModelConstants.BpmnNs)]
public class ConditionalEventDefinition : EventDefinition, IConditionalEventDefinition
{
    [BpmnElement("condition", BpmnModelConstants.BpmnNs)]
    public required Expression Condition { get; set; }
}