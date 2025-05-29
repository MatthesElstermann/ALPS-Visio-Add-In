using System.Collections.Generic;
using alps.net.api.ALPS;
using System.Linq;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;
using alps.net.api.parsing;
using System.Diagnostics;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes.elements
{
    public class VisioPASSProcessModel : PASSProcessModel, IVisioExportable
    {

        public VisioPASSProcessModel(string baseURI, string labelForID = null, ISet<IMessageExchange> messageExchanges = null, ISet<ISubject> relationsToModelComponent = null,
            ISet<ISubject> startSubject = null, string comment = null, string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null)
            : base(baseURI, labelForID, messageExchanges, relationsToModelComponent, startSubject, comment, additionalLabel, additionalAttribute) { }

        protected VisioPASSProcessModel() { }

        public void exportToVisio(Visio.Page currentPage, ISimple2DVisualizationBounds bounds = null)
        {
            foreach (IModelLayer modelLayer in getAllElements().Values.OfType<IModelLayer>())
            {
                Visio.Page page = VisioHelper.CreateSIDPage(modelLayer.getModelComponentID(), " ", modelLayer.getUriModelComponentID(), " ", " ", " ");

                if (modelLayer is IVisioExportable exportable) exportable.exportToVisio(page);
            }
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioPASSProcessModel();
        }
    }
}
