
using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using System.Collections.Generic;
using System.Linq;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisioAddIn.OwlShapes
{
    public class VisioPASSProcessModel : PASSProcessModel, IVisioExportable
    {
        private IList<Visio.Page> visioPages = new List<Visio.Page>();

        public VisioPASSProcessModel(string baseURI, string labelForID = null, ISet<IMessageExchange> messageExchanges = null, ISet<ISubject> relationsToModelComponent = null,
            ISet<ISubject> startSubject = null, string comment = null, string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null)
            : base(baseURI, labelForID, messageExchanges, relationsToModelComponent, startSubject, comment, additionalLabel, additionalAttribute) { }

        protected VisioPASSProcessModel() { }


        public void exportToVisio(Visio.Page currentExportingPage, ISimple2DVisualizationBounds bounds = null)
        {
            bool first = true;
            foreach (IModelLayer modelLayer in getAllElements().Values.OfType<IModelLayer>())
            {
                Visio.Page currentPage = currentExportingPage;

                if (!first)
                {
                    // Create a new page for every model layer but the first
                    currentPage = VisioHelper.CreateSIDPage(modelLayer.getModelComponentID(), " ", modelLayer.getUriModelComponentID(), " ", " ", " ");
                    visioPages.Add(currentPage);

                }
                else
                {
                    first = false;
                    if (currentPage.PageSheet.CellExistsU["Prop." + ALPSConstants.alpsPropertieTypePageType,0] == 0)
                    {
                        currentPage = VisioHelper.CreateSIDPage(modelLayer.getModelComponentID(), " ", modelLayer.getUriModelComponentID(), " ", " ", " ");
                        visioPages.Add(currentPage);
                    }
                }


                //TODO: rezise all Pages  if values available

                if (modelLayer is IVisioExportable exportable) exportable.exportToVisio(currentPage);
            }


            // auto layout visio pages
            //TODO this.LayoutPages(Page.LayoutType.Visio);

        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioPASSProcessModel();
        }

        public IList<Visio.Page> getVisioElement()
        {
            return visioPages;
        }
    }
}
