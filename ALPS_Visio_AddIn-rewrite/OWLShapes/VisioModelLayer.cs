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
    public class VisioModelLayer : ModelLayer, IVisioExportable
    {
        public VisioModelLayer(IPASSProcessModel model, string labelForID = null, string comment = null, string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null) : base(model, labelForID, comment, additionalLabel, additionalAttribute) { }
        protected VisioModelLayer() { }

        public void ExportToVisio(Visio.Page page)
        {
            SetPageDimensions(page);

            // hasPriorityNumber
            VH.SetProp(page.PageSheet, Constants.Properties.PriorityOrderNumber, this.priorityNumber.ToString());

            foreach (IPASSProcessModelElement modelElement in this.getElements().Values.OrderBy(el => el is IMessageExchangeList))
            {
                if (!(modelElement is IVisioExportable exportable)) continue;

                if (exportable is IVisioExportableWithShape shapeExportable) shapeExportable.PrepareDimensions(); // TODO: if not: auto arrange

                if (exportable is ISubject || exportable is IMessageExchangeList) exportable.ExportToVisio(page);
            }
        }

        /// <summary>
        /// Calculate dimensions for this model and apply to given page.
        /// 
        /// Note: This is inconsistent, it would be great to add some size to the standard.
        /// </summary>
        private void SetPageDimensions(Visio.Page page)
        {
            double pageRatio = 1;
            double sumWidth = 0;
            int subjectCount = 0;
            foreach (ISubject modelElement in this.getElements().Select(x => x.Value).OfType<ISubject>())
            {
                if (modelElement is ISystemInterfaceSubject) continue;

                if ((modelElement is IFullySpecifiedSubject || modelElement is IInterfaceSubject))
                {
                    pageRatio = modelElement.get2DPageRatio();
                    double width = modelElement.getRelative2DWidth();
                    sumWidth += width;
                    if (width > 0) subjectCount++;
                }
            }
            double averageWidth = sumWidth / subjectCount;

            // the average subject is 32 mm wide
            double newPageWidth = 32 / averageWidth + 1;
            double newPageHeight = newPageWidth / pageRatio;

            // FEAT: round to nearest A4 page

            VH.SetCellMM(page.PageSheet, "PageWidth", newPageWidth);
            VH.SetCellMM(page.PageSheet, "PageHeight", newPageHeight);
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioModelLayer();
        }
    }
}