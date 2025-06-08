using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Microsoft.Office.Interop.Visio;
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
            setPageBounds(currentPage);

            // todo: auto arrange (prep2DInfo returns boolean)

            foreach (IPASSProcessModelElement modelElement in getElements().Values)
            {
                if (!(modelElement is IVisioExportable exportable)) continue;

                if (exportable is IVisioExportableWithShape shapeExportable) shapeExportable.prep2DInfo();

                if (exportable is ISubject || exportable is IMessageExchange || exportable is IMessageExchangeList) exportable.exportToVisio(currentPage);
            }
        }

        private void setPageBounds(Visio.Page currentPage)
        {
            // Now, calculate the average of getRelative2DWidth and getRelative2DHeight values
            double sumWidth = 0;
            double sumHeight = 0;
            int subjectCount = 0;
            double pageRatio = 1;

            foreach (ISubject modelElement in getElements().Select(x => x.Value).OfType<ISubject>())
            {
                if ((modelElement is IFullySpecifiedSubject || modelElement is IInterfaceSubject) && !(modelElement is ISystemInterfaceSubject))
                {
                    double width = modelElement.getRelative2DWidth();
                    double height = modelElement.getRelative2DHeight();
                    pageRatio = modelElement.get2DPageRatio();


                    if (width > 0)
                    {
                        sumWidth += width;
                        subjectCount++;
                    }

                    if (height > 0)
                    {
                        sumHeight += height;
                    }
                }
            }

            double averageWidth = sumWidth / subjectCount;
            double averageHeight = sumHeight / subjectCount;

            double heightMod = 1 / pageRatio;
            double averageSubjectRatio = averageWidth / (averageHeight * heightMod);

            double expectedDefaultValue = 0.638297872;

            double ratioMod = averageSubjectRatio - expectedDefaultValue;

            double newPageWidth = (32.00 + (32.00 * ratioMod)) / averageWidth + 1;
            double newPageHeight = newPageWidth * heightMod;

            currentPage.PageSheet.CellsU["PageWidth"].FormulaU = newPageWidth.ToString(CultureInfo.InvariantCulture) + " mm";
            currentPage.PageSheet.CellsU["PageHeight"].FormulaU = newPageHeight.ToString(CultureInfo.InvariantCulture) + " mm";

            currentPage.AutoSize = false;
            if (currentPage.PageSheet.CellExistsU["User.OWLIMPORTINFORATIOMOD", 0] == 0)
            {
                currentPage.PageSheet.AddNamedRow((short)VisSectionIndices.visSectionUser, "OWLIMPORTINFORATIOMOD", 0);
            }
            currentPage.PageSheet.CellsU["User.OWLIMPORTINFORATIOMOD"].FormulaU = "=" + ratioMod.ToString(CultureInfo.InvariantCulture);
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioModelLayer();
        }
    }
}