
using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using System.Collections.Generic;
using System.Linq;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisioAddIn.OwlShapes
{
    public class VisioModelLayer : ModelLayer, IVisioExportable
    {

        public VisioModelLayer(IPASSProcessModel model, string labelForID = null, string comment = null, string additionalLabel = null,
            IList<IIncompleteTriple> additionalAttribute = null)
            : base(model, labelForID, comment, additionalLabel, additionalAttribute) { setContainedBy(model); }

        protected VisioModelLayer() { }


        public void exportToVisio(Visio.Page currentPage, ISimple2DVisualizationBounds bounds = null)
        {
            IList<IPASSProcessModelElement> exportedElements = new List<IPASSProcessModelElement>();
            // Go through each model element on the layer
            foreach (ISubject modelElement in getElements().Select(x => x.Value).OfType<ISubject>())
            {
                if (!(modelElement is IVisioExportable exportableSubj)) continue;
                exportableSubj.exportToVisio(currentPage);
                exportedElements.Add(modelElement);
            }

            foreach (IMessageExchangeList modelElement in getElements().Select(x => x.Value).OfType<IMessageExchangeList>())
            {
                if (!(modelElement is IVisioExportable exportable)) continue;
                exportable.exportToVisio(currentPage);
                exportedElements.Add(modelElement);
            }
            foreach (IMessageExchange modelElement in getElements().Select(x => x.Value).OfType<IMessageExchange>())
            {
                if (!(modelElement is IVisioExportable exportable)) continue;
                exportable.exportToVisio(currentPage);
                exportedElements.Add(modelElement);
            }
            foreach (var modelElement in getElements().Select(x => x.Value).Where(x => !exportedElements.Contains(x)))
            {
                if (modelElement is ISubjectBehavior || !(modelElement is IVisioExportable exportable))
                    continue;
                exportable.exportToVisio(currentPage);
                exportedElements.Add(modelElement);

            }
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioModelLayer();
        }

        
    }
}
