#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace PassBpmnConverter.Bpmn;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class BpmnAttributeAttribute : BpmnAttribute
{
    public string Name { get; }

    public BpmnAttributeAttribute(string name, [CallerLineNumber] int order = 0) : base(order)
    {
        Name = name;
    }
}