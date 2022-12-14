using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using alps.net.api.parsing;

namespace VisioAddIn.OwlShapes
{
    class VisioClassFactory : BasicPASSProcessModelElementFactory
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="possibleElements"></param>
        /// <returns></returns>
        protected override KeyValuePair<IParseablePASSProcessModelElement, string> decideForElement
            (IDictionary<IParseablePASSProcessModelElement, string> possibleElements)
        {
            // Search if one of the possible values if of type IVisioExportable
            foreach (KeyValuePair<IParseablePASSProcessModelElement, string> pair in possibleElements)
            {
                if (pair.Key is IVisioExportable) return pair;
            }
            // If not, pass the decision of the base implementation
            return base.decideForElement(possibleElements);
        }
    }
}
