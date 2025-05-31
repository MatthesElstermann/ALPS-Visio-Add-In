using System.Collections.Generic;
using System.Linq;
using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Serilog.Sinks.File;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioModelLayer : ModelLayer, IVisioExportable
    {

        public VisioModelLayer(IPASSProcessModel model, string labelForID = null, string comment = null, string additionalLabel = null,
            IList<IIncompleteTriple> additionalAttribute = null)
            : base(model, labelForID, comment, additionalLabel, additionalAttribute)
        {
            setContainedBy(model);
        }

        protected VisioModelLayer()
        {
        }

        public void exportToVisio(Visio.Page currentPage)
        {
            // test if 2D representation
            // yes: set Coords
            // no: auto arrange

            IList<IPASSProcessModelElement> exportedElements = new List<IPASSProcessModelElement>();

            foreach (IPASSProcessModelElement modelElement in getElements().Values)
            {
                if (!(modelElement is IVisioExportable exportable)) continue;

                if (exportable is ISubject || exportable is IMessageExchange || exportable is IMessageExchangeList)
                {
                    exportable.exportToVisio(currentPage);
                    exportedElements.Add(modelElement);
                }
            }

            foreach (IPASSProcessModelElement modelElement in getElements().Values.Where(el => !exportedElements.Contains(el)))
            {
                if (!(modelElement is IVisioExportable exportable)) continue;

                if (modelElement is ISubjectBehavior) continue;

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