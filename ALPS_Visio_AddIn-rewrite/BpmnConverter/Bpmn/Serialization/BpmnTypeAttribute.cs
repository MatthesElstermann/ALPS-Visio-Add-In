#nullable enable
using System;
namespace PassBpmnConverter.Bpmn;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
public sealed class BpmnTypeAttribute : Attribute
{
    public string Name { get; }
    public string? Namespace { get; }

    public BpmnTypeAttribute(string name, string? @namespace = null)
    {
        Name = name;
        Namespace = @namespace;
    }
}