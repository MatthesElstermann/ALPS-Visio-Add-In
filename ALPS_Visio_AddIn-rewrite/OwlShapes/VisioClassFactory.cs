using System.Collections.Generic;
using alps.net.api.parsing;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioClassFactory : BasicPASSProcessModelElementFactory
    {
        protected override KeyValuePair<IParseablePASSProcessModelElement, string> decideForElement(IDictionary<IParseablePASSProcessModelElement, string> possibleElements)
        {
            foreach (KeyValuePair<IParseablePASSProcessModelElement, string> pair in possibleElements)
            {
                if (pair.Key is IVisioExportable) return pair;
            }

            return base.decideForElement(possibleElements);
        }
    }
}