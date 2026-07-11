#nullable enable
namespace PassBpmnConverter.Bpmn;

public interface IExclusiveGateway : IGateway
{
    ISequenceFlow? Default { get; set; }
}

[BpmnType("exclusiveGateway", BpmnModelConstants.BpmnNs)]
public class ExclusiveGateway : Gateway, IExclusiveGateway
{
    [BpmnAttribute("default")]
    public ISequenceFlow? Default { get; set; }
}