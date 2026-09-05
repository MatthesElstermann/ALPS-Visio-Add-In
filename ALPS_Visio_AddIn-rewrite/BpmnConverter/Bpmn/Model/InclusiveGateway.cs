#nullable enable
namespace PassBpmnConverter.Bpmn;

// Wird vom PASS-Konverter selbst nicht erzeugt, kommt aber in fremden
// BPMN-Dateien vor (BPMN-Datei-Import/-Anzeige).
public interface IInclusiveGateway : IGateway
{
}

[BpmnType("inclusiveGateway", BpmnModelConstants.BpmnNs)]
public class InclusiveGateway : Gateway, IInclusiveGateway
{
}
