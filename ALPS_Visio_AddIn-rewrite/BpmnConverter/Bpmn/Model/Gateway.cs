#nullable enable
namespace PassBpmnConverter.Bpmn;

public interface IGateway : IFlowNode
{
    GatewayDirection GatewayDirection { get; set; }
}

[BpmnType("gateway", BpmnModelConstants.BpmnNs)]
public abstract class Gateway : FlowNode, IGateway
{
    [BpmnAttribute("gatewayDirection")]
    public GatewayDirection GatewayDirection { get; set; } = GatewayDirection.Unspecified;
}