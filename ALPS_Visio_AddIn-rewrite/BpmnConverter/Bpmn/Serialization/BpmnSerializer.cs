#nullable enable
using System;
using System.Linq;
using System.Collections;
using System.Reflection;
using System.Xml.Linq;

namespace PassBpmnConverter.Bpmn;

public class BpmnSerializer
{
    private XDocument? _document;

    public static void Serialize(IBpmnModel bpmnModel, string outputFilePath)
    {
        new BpmnSerializer().SerializeModel(bpmnModel, outputFilePath);
    }

    private void SerializeModel(IBpmnModel bpmnModel, string outputFilePath)
    {
        if (bpmnModel == null)
        {
            throw new NullReferenceException($"{nameof(bpmnModel)} cannot be null");
        }

        if (bpmnModel.Definitions == null)
        {
            throw new NullReferenceException($"{nameof(bpmnModel.Definitions)} cannot be null");
        }

        _document = new XDocument(
            SerializeElement(bpmnModel.Definitions)
        );

        if (_document.Root != null)
        {
            // TODO: only include used namespaces
            _document.Root.Add(
                new XAttribute("xmlns", BpmnModelConstants.BpmnNs),
                new XAttribute(XNamespace.Xmlns + "bpmndi", BpmnModelConstants.BpmnDiNs),
                new XAttribute(XNamespace.Xmlns + "omgdc", BpmnModelConstants.OmgDcNs),
                new XAttribute(XNamespace.Xmlns + "omgdi", BpmnModelConstants.OmgDiNs),
                new XAttribute(XNamespace.Xmlns + "xsi", BpmnModelConstants.Xsi)
            );
        }

        _document.Save(outputFilePath);
    }

    private XElement SerializeElement(object obj, XName? name = null)
    {
        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        Type type = obj.GetType();
        bool isSimpleType = IsSimpleType(type);

        XElement element;

        if (isSimpleType)
        {
            if (name == null)
            {
                throw new ArgumentNullException(null, $"Property {nameof(BpmnTypeAttribute.Name)} of {nameof(BpmnTypeAttribute)} cannot be null for simple types.");
            }

            element = new XElement(name);

            string? value = obj.ToString();
            if (value != null)
            {
                element.Value = value;
            }
        }
        else
        {
            BpmnTypeAttribute? bpmnType = type.GetCustomAttribute<BpmnTypeAttribute>();
            if (bpmnType == null)
            {
                // TODO: implement MissingAttributeException
                throw new InvalidOperationException($"The type {type.Name} is missing the required {nameof(BpmnTypeAttribute)}.");
            }

            element = new XElement(name ?? CreateXName(bpmnType.Name, bpmnType.Namespace));

            PropertyInfo[] properties = GetProperties(type);

            foreach (PropertyInfo property in properties)
            {
                // TODO: set inherit to true?
                BpmnAttributeAttribute? bpmnAttribute = property.GetCustomAttribute<BpmnAttributeAttribute>();

                if (bpmnAttribute != null)
                {
                    // TODO: null checks?
                    element.SetAttributeValue(bpmnAttribute.Name, property.GetValue(obj));
                }

                BpmnElementAttribute? bpmnElement = property.GetCustomAttribute<BpmnElementAttribute>();

                if (bpmnElement != null)
                {
                    object? value = property.GetValue(obj);

                    if (value != null)
                    {
                        XName? elementName = bpmnElement.Name != null ? CreateXName(bpmnElement.Name, bpmnElement.Namespace) : null;

                        if (value is IEnumerable enumerable && obj is not string)
                        {
                            foreach (object item in enumerable)
                            {
                                element.Add(SerializeElement(item, elementName));
                            }
                        }
                        else
                        {
                            element.Add(SerializeElement(value, elementName));
                        }
                    }
                }
            }
        }

        return element;
    }

    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive || type.IsEnum || type.Equals(typeof(string));
    }

    private static XName CreateXName(string localName, string? @namespace = null)
    {
        if (localName == null)
        {
            throw new ArgumentNullException(nameof(localName));
        }

        if (@namespace != null)
        {
            return XName.Get(localName, @namespace);
        }
        else
        {
            return localName;
        }
    }

    // adapted from https://stackoverflow.com/a/71504013
    private static PropertyInfo[] GetProperties(Type type)
    {
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

        Array.Sort(
            properties,
            (a, b) =>
            {
                if (a.DeclaringType == null || b.DeclaringType == null)
                {
                    return 0;
                }
                else if (a.DeclaringType.IsSubclassOf(b.DeclaringType))
                {
                    return 1;
                }
                else if (b.DeclaringType.IsSubclassOf(a.DeclaringType))
                {
                    return -1;
                }
                else
                {
                    int orderA = a.GetCustomAttributes<BpmnAttribute>().FirstOrDefault()?.Order ?? 0;
                    int orderB = b.GetCustomAttributes<BpmnAttribute>().FirstOrDefault()?.Order ?? 0;
                    return orderA - orderB;
                }
            }
        );

        return properties;
    }
}