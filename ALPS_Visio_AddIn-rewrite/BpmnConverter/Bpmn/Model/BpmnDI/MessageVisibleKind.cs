#nullable enable
namespace PassBpmnConverter.Bpmn.BpmnDI;

[BpmnType("MessageVisibleKind", BpmnModelConstants.BpmnDiNs)]
public enum MessageVisibleKind
{
    initiating,
    non_initiating
}