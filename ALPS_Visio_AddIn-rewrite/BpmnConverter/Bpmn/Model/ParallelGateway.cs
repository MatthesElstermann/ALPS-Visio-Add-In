#nullable enable
namespace PassBpmnConverter.Bpmn;

// Wird vom PASS-Konverter selbst nicht erzeugt, kommt aber in fremden
// BPMN-Dateien vor (BPMN-Datei-Import/-Anzeige).
public interface IParallelGateway : IGateway
{
}

[BpmnType("parallelGateway", BpmnModelConstants.BpmnNs)]
public class ParallelGateway : Gateway, IParallelGateway
{
}
