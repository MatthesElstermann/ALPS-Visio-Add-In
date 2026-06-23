using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using System.Collections.Generic;
using System.Linq;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioPASSProcessModel : PASSProcessModel, IVisioImportable
    {
        public VisioPASSProcessModel(string baseURI, string labelForID = null, ISet<IMessageExchange> messageExchanges = null, ISet<ISubject> relationsToModelComponent = null, ISet<ISubject> startSubject = null, string comment = null, string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null) : base(baseURI, labelForID, messageExchanges, relationsToModelComponent, startSubject, comment, additionalLabel, additionalAttribute) { }
        protected VisioPASSProcessModel() { }

        public void ImportToVisio(Visio.Page page)
        {
            // TODO: this.layered -> ALPS model

            foreach (IModelLayer modelLayer in this.getAllElements().Values.OfType<IModelLayer>())
            {
                Visio.Page SIDPage = VH.CreateSIDPage(modelLayer.getModelComponentID(), " ", modelLayer.getUriModelComponentID(), " ", " ", " "); // TODO: SID page creation

                // TODO: ExtensionLayer, GuardLayer, MacroLayer
                if (modelLayer is IVisioImportable importable) importable.ImportToVisio(SIDPage);
            }
        }
        
        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioPASSProcessModel();
        }
    }
}
