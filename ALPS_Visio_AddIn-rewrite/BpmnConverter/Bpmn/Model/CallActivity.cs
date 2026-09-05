#nullable enable
namespace PassBpmnConverter.Bpmn;

public interface ICallActivity : IActivity
{
    string? CalledElement { get; set; }
}

[BpmnType("callActivity", BpmnModelConstants.BpmnNs)]
public class CallActivity : Activity, ICallActivity
{
    [BpmnAttribute("calledElement")]
    public string? CalledElement { get; set; }
}