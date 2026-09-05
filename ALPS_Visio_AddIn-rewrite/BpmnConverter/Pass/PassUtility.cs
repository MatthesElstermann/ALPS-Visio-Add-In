#nullable enable
using System.Collections.Generic;
using alps.net.api.StandardPASS;

namespace PassBpmnConverter.Pass;

public static class PassUtility
{
    public static string GetElementId(IPASSProcessModelElement passElement)
    {
        return passElement.getModelComponentID();
    }

    public static string GetElementName(IPASSProcessModelElement passElement)
    {
        if (passElement == null)
            return string.Empty;

        IList<string> labels = passElement.getModelComponentLabelsAsStrings();

        if (labels.Count < 1)
            return string.Empty;

        return labels[0] ?? string.Empty;
    }
}