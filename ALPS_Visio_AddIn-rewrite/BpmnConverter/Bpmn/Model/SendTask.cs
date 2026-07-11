#nullable enable
namespace PassBpmnConverter.Bpmn;

public interface ISendTask : ITask
{
    IMessage? MessageRef { get; set; }
}

[BpmnType("sendTask", BpmnModelConstants.BpmnNs)]
public class SendTask : Task, ISendTask
{
    [BpmnAttribute("messageRef")]
    public IMessage? MessageRef { get; set; }
}