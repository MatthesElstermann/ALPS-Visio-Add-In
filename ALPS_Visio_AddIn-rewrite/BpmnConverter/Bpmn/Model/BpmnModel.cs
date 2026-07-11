#nullable enable
namespace PassBpmnConverter.Bpmn;

// technically this is not supposed to inherit from IBaseElement, however, this simplifies handling
public interface IBpmnModel : IBaseElement
{
    // TODO: is this supposed to be nullable?
    IDefinitions? Definitions { get; set; }
}

public class BpmnModel : IBpmnModel
{
    public string? Id { get; set; }

    [BpmnElement]
    public IDefinitions? Definitions { get; set; }
}