#nullable enable
namespace PassBpmnConverter.Bpmn;

public interface IReceiveTask : ITask
{
    IMessage? MessageRef { get; set; }
}

[BpmnType("receiveTask", BpmnModelConstants.BpmnNs)]
public class ReceiveTask : Task, IReceiveTask
{
    [BpmnAttribute("messageRef")]
    public IMessage? MessageRef { get; set; }
}