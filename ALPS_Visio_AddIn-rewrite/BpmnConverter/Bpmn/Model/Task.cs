#nullable enable
namespace PassBpmnConverter.Bpmn;

public interface ITask : IActivity
{
}

[BpmnType("task", BpmnModelConstants.BpmnNs)]
public class Task : Activity, ITask
{
}