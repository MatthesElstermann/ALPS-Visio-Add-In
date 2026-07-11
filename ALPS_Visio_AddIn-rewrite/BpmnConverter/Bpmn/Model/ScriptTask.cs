#nullable enable
namespace PassBpmnConverter.Bpmn;

// TODO: add script property
public interface IScriptTask : ITask
{
}

[BpmnType("scriptTask", BpmnModelConstants.BpmnNs)]
public class ScriptTask : Task, IScriptTask
{
}