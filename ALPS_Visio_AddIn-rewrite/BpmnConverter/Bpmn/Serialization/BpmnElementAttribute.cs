#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace PassBpmnConverter.Bpmn;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class BpmnElementAttribute : BpmnAttribute
{
    public string? Name { get; }
    public string? Namespace { get; }

    public BpmnElementAttribute([CallerLineNumber] int order = 0) : base(order)
    {
    }

    public BpmnElementAttribute(string name, string @namespace, [CallerLineNumber] int order = 0) : base(order)
    {
        Name = name;
        Namespace = @namespace;
        Order = order;
    }
}