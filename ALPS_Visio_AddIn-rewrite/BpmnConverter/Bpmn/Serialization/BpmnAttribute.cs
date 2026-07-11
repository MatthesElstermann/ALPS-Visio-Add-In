#nullable enable
using System;
namespace PassBpmnConverter.Bpmn;

public abstract class BpmnAttribute : Attribute
{
    public int Order;

    public BpmnAttribute(int order)
    {
        Order = order;
    }
}