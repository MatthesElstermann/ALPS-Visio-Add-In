#nullable enable
namespace PassBpmnConverter.Bpmn;

public interface ISignal : IRootElement
{
    public string? Name { get; set; }
}

[BpmnType("signal", BpmnModelConstants.BpmnNs)]
public class Signal : RootElement, ISignal
{
    [BpmnAttribute("name")]
    public string? Name { get; set; }
}