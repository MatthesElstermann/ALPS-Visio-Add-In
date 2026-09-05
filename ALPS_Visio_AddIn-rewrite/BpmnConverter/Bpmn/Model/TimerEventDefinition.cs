#nullable enable
namespace PassBpmnConverter.Bpmn;

public interface ITimerEventDefinition : IEventDefinition
{
    Expression? TimeDate { get; set; }

    Expression? TimeDuration { get; set; }

    Expression? TimeCycle { get; set; }
}

[BpmnType("timerEventDefinition", BpmnModelConstants.BpmnNs)]
public class TimerEventDefinition : EventDefinition, ITimerEventDefinition
{
    // TODO: ensure only one of the following expressions is ever set
    [BpmnElement("timeDate", BpmnModelConstants.BpmnNs)]
    public Expression? TimeDate { get; set; }

    [BpmnElement("timeDuration", BpmnModelConstants.BpmnNs)]
    public Expression? TimeDuration { get; set; }

    [BpmnElement("timeCycle", BpmnModelConstants.BpmnNs)]
    public Expression? TimeCycle { get; set; }
}