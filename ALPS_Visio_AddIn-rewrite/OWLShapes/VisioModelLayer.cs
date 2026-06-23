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

        // SID auto-layout constants — values in mm
        private const double SIDSubjectWidthMM = 32.0;
        private const double SIDSubjectSpacingMM = 20.0;
        private const double SIDMarginMM = 25.0;

        public void ExportToVisio(Visio.Page page)
        {
            SetPageDimensions(page);

            // hasPriorityNumber
            VH.SetProp(page.PageSheet, Constants.Properties.PriorityOrderNumber, this.priorityNumber.ToString());

            bool anyHadCoordinates = false;
            var exportedSubjects = new List<ISubject>();
            var messageExchangeLists = new List<IVisioExportable>();

            // First pass: export subjects (message lists are deferred — they glue connectors
            // to the subject shapes and must run after the subjects have their final position).
            foreach (IPASSProcessModelElement modelElement in this.getElements().Values)
            {
                if (!(modelElement is IVisioExportable exportable)) continue;

                if (exportable is IVisioExportableWithShape shapeExportable)
                    if (shapeExportable.PrepareDimensions()) anyHadCoordinates = true;

                if (modelElement is ISubject subject)
                {
                    exportable.ExportToVisio(page);
                    exportedSubjects.Add(subject);
                }
                else if (modelElement is IMessageExchangeList)
                {
                    messageExchangeLists.Add(exportable);
                }
            }

            // Position the subjects before drawing the message connectors. Otherwise the
            // message box is centered on the connector while the subjects still sit at their
            // drop position; moving them afterwards drags the (glued) connector along but
            // leaves the box behind at (0,0).
            if (!anyHadCoordinates && exportedSubjects.Count > 0)
                ApplyHorizontalLayout(exportedSubjects, page);

            // Second pass: now that subjects are placed, draw the message exchange lists.
            foreach (IVisioExportable messageExchangeList in messageExchangeLists)
                messageExchangeList.ExportToVisio(page);
        }

        /// <summary>
        /// Arranges SID subjects in a horizontal row when the OWL file has no coordinates.
        /// Subjects are evenly spaced left-to-right, centered vertically on the page.
        /// </summary>
        private void ApplyHorizontalLayout(IList<ISubject> subjects, Visio.Page page)
        {
            double pageHeightMM = page.PageSheet.CellsU["PageHeight"].Result["mm"];
            double y = pageHeightMM / 2.0;
            double x = SIDMarginMM + SIDSubjectWidthMM / 2.0;

            foreach (ISubject subject in subjects)
            {
                if (!(subject is IVisioExportableWithShape exportable)) continue;
                Visio.Shape shape = exportable.GetShape();
                if (shape != null)
                {
                    VH.SetCellMM(shape, Constants.ShapeCells.PinX, x);
                    VH.SetCellMM(shape, Constants.ShapeCells.PinY, y);
                }
                x += SIDSubjectWidthMM + SIDSubjectSpacingMM;
            }
        }

        /// <summary>
        /// Calculates SID page dimensions from subject coordinate data in the OWL model.
        /// Returns <c>true</c> when at least one subject had valid size data and the page
        /// was resized; <c>false</c> when no coordinate data exists and defaults are kept.
        /// </summary>
        /// <remarks>
        /// This is an approximation — the standard does not mandate page-size information.
        /// </remarks>
        private bool SetPageDimensions(Visio.Page page)
        {
            double pageRatio = 1;
            double sumWidth = 0;
            int subjectCount = 0;
            foreach (ISubject modelElement in this.getElements().Select(x => x.Value).OfType<ISubject>())
            {
                if (modelElement is ISystemInterfaceSubject) continue;

                if (modelElement is IFullySpecifiedSubject || modelElement is IInterfaceSubject)
                {
                    pageRatio = modelElement.get2DPageRatio();
                    double width = modelElement.getRelative2DWidth();
                    sumWidth += width;
                    if (width > 0) subjectCount++;
                }
            }

            if (subjectCount == 0) return false;

            double averageWidth = sumWidth / subjectCount;
            if (averageWidth <= 0) return false;

            // The average subject is ~32 mm wide
            double newPageWidth = 32.0 / averageWidth + 1.0;
            double newPageHeight = pageRatio > 0 ? newPageWidth / pageRatio : newPageWidth;

            // FEAT: round to nearest A4 page

            VH.SetCellMM(page.PageSheet, "PageWidth", newPageWidth);
            VH.SetCellMM(page.PageSheet, "PageHeight", newPageHeight);
            return true;
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioModelLayer();
        }
    }
}