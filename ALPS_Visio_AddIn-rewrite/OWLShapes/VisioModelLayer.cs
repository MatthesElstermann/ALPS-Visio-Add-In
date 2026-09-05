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
    public class VisioModelLayer : ModelLayer, IVisioImportable
    {
        public VisioModelLayer(IPASSProcessModel model, string labelForID = null, string comment = null, string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null) : base(model, labelForID, comment, additionalLabel, additionalAttribute) { }
        protected VisioModelLayer() { }

        // SID auto-layout constants — values in mm. The spacing is the gap between subjects;
        // it must be wide enough for the message connector's box (message list) to sit
        // between two subjects without covering their labels.
        private const double SIDSubjectWidthMM = 32.0;
        private const double SIDSubjectSpacingMM = 55.0;
        private const double SIDMarginMM = 25.0;

        public void ImportToVisio(Visio.Page page)
        {
            SetPageDimensions(page);

            // hasPriorityNumber
            VH.SetProp(page.PageSheet, Constants.Properties.PriorityOrderNumber, this.priorityNumber.ToString());

            bool anyHadCoordinates = false;
            var importedSubjects = new List<ISubject>();
            var messageExchangeLists = new List<IVisioImportable>();
            var otherDrawables = new List<IVisioImportable>();

            // First pass: import subjects (message lists and remaining SID-level drawables are
            // deferred — they glue connectors to the subject shapes and must run after the
            // subjects have their final position).
            foreach (IPASSProcessModelElement modelElement in this.getElements().Values)
            {
                if (!(modelElement is IVisioImportable importable)) continue;

                // PrepareDimensions() separat absichern: es laeuft VOR dem gekapselten
                // ImportToVisio und fuer jedes Shape-Element (auch Message-Exchanges) —
                // ein Fehler hier wuerde sonst den ganzen Import ungebremst abbrechen.
                if (importable is IVisioImportableWithShape shapeImportable)
                {
                    try
                    {
                        if (shapeImportable.PrepareDimensions()) anyHadCoordinates = true;
                    }
                    catch (System.Exception ex)
                    {
                        string id = modelElement.getModelComponentID();
                        System.Diagnostics.Debug.WriteLine("PrepareDimensions von \"" + id + "\" fehlgeschlagen: " + ex);
                    }
                }

                if (modelElement is ISubject subject)
                {
                    if (SafeImportToVisio(importable, page))
                        importedSubjects.Add(subject);
                }
                else if (modelElement is IMessageExchangeList)
                {
                    messageExchangeLists.Add(importable);
                }
                // Remaining SID-level drawables (e.g. communication channels/restrictions).
                // States, transitions, behaviours, message exchanges and message specifications
                // are drawn by their own containers, so they are skipped here.
                else if (!(modelElement is IMessageExchange) && !(modelElement is IMessageSpecification)
                    && !(modelElement is ISubjectBehavior) && !(modelElement is IState)
                    && !(modelElement is ITransition))
                {
                    otherDrawables.Add(importable);
                }
            }

            // Position the subjects before drawing the message connectors. Otherwise the
            // message box is centered on the connector while the subjects still sit at their
            // drop position; moving them afterwards drags the (glued) connector along but
            // leaves the box behind at (0,0).
            if (!anyHadCoordinates && importedSubjects.Count > 0)
                ApplyHorizontalLayout(importedSubjects, page);

            // Second pass: now that subjects are placed, draw the message exchange lists
            // and the remaining SID-level drawables (their connectors glue to the subjects).
            foreach (IVisioImportable messageExchangeList in messageExchangeLists)
                SafeImportToVisio(messageExchangeList, page);

            foreach (IVisioImportable drawable in otherDrawables)
                SafeImportToVisio(drawable, page);
        }

        /// <summary>
        /// Zeichnet ein Element und faengt Fehler ab, damit ein einzelnes problematisches
        /// Element (z. B. ein Message-Connector, dessen Stencil-VBA nicht laeuft) nicht den
        /// gesamten Import abbricht. Die Log-Zeile nennt das schuldige Element samt Fehler,
        /// sodass die Ursache ohne Debugger sichtbar wird. Rueckgabe: true bei Erfolg.
        /// </summary>
        private static bool SafeImportToVisio(IVisioImportable importable, Visio.Page page)
        {
            try
            {
                importable.ImportToVisio(page);
                return true;
            }
            catch (System.Exception ex)
            {
                string id = importable is IPASSProcessModelElement element ? element.getModelComponentID() : importable.GetType().Name;
                System.Diagnostics.Debug.WriteLine("Import des Elements \"" + id + "\" fehlgeschlagen: " + ex);
                return false;
            }
        }

        /// <summary>
        /// Arranges SID subjects in a horizontal row when the OWL file has no coordinates.
        /// Subjects are evenly spaced left-to-right, centered vertically on the page.
        /// </summary>
        private void ApplyHorizontalLayout(IList<ISubject> subjects, Visio.Page page)
        {
            // Grow the page so the row of subjects (and the message boxes between them) fits.
            double rowWidth = (subjects.Count - 1) * (SIDSubjectWidthMM + SIDSubjectSpacingMM);
            double pageWidth = rowWidth + SIDSubjectWidthMM + 2 * SIDMarginMM;
            pageWidth = System.Math.Max(pageWidth, page.PageSheet.CellsU["PageWidth"].Result["mm"]);
            VH.SetCellMM(page.PageSheet, "PageWidth", pageWidth);

            double pageHeightMM = page.PageSheet.CellsU["PageHeight"].Result["mm"];
            double y = pageHeightMM / 2.0;
            double x = SIDMarginMM + SIDSubjectWidthMM / 2.0;

            foreach (ISubject subject in subjects)
            {
                if (!(subject is IVisioImportableWithShape importable)) continue;
                Visio.Shape shape = importable.GetShape();
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