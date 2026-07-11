#nullable enable
namespace PassBpmnConverter.Bpmn;

public interface IEventBasedGateway : IGateway
{
    bool Instantiate { get; set; }
    EventBasedGatewayType EventBasedGatewayType { get; set; }
}

[BpmnType("eventBasedGateway", BpmnModelConstants.BpmnNs)]
public class EventBasedGateway : Gateway, IEventBasedGateway
{
    [BpmnAttribute("instantiate")]
    public bool Instantiate { get; set; } = false;

    [BpmnAttribute("eventGatewayType")]
    public EventBasedGatewayType EventBasedGatewayType { get; set; } = EventBasedGatewayType.Exclusive;
}