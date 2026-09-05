#nullable enable
using System.Collections.Generic;
using PassBpmnConverter.Bpmn.BpmnDI;

namespace PassBpmnConverter.Bpmn;

public interface IDefinitions : IBaseElement
{
    string? Name { get; set; }

    string TargetNamespace { get; set; }

    string? Exporter { get; set; }

    string? ExporterVersion { get; set; }

    IList<IRootElement> RootElements { get; set; }

    IList<IBpmnDiagram> Diagrams { get; set; }
}

[BpmnType("definitions", BpmnModelConstants.BpmnNs)]
public class Definitions : BaseElement, IDefinitions
{
    [BpmnAttribute("name")]
    public string? Name { get; set; }

    [BpmnAttribute("targetNamespace")]
    public required string TargetNamespace { get; set; }

    [BpmnAttribute("exporter")]
    public string? Exporter { get; set; }

    [BpmnAttribute("exporterVersion")]
    public string? ExporterVersion { get; set; }

    [BpmnElement]
    public IList<IRootElement> RootElements { get; set; } = new List<IRootElement>();

    [BpmnElement]
    public IList<IBpmnDiagram> Diagrams { get; set; } = new List<IBpmnDiagram>();
}