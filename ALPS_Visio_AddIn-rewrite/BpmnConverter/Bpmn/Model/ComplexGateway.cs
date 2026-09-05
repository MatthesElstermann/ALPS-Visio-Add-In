#nullable enable
namespace PassBpmnConverter.Bpmn;

// Wird vom PASS-Konverter selbst nicht erzeugt, kommt aber in fremden
// BPMN-Dateien vor (BPMN-Datei-Import/-Anzeige).
public interface IComplexGateway : IGateway
{
}

[BpmnType("complexGateway", BpmnModelConstants.BpmnNs)]
public class ComplexGateway : Gateway, IComplexGateway
{
}
