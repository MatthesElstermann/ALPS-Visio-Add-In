using alps.net.api;
using alps.net.api.ALPS;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class PASSProcessModelElementImport : IShapeImport
    {
        private readonly IPASSProcessModelElement element;

        /// <summary>
        /// Base shape import for elements
        /// </summary>
        public PASSProcessModelElementImport(IPASSProcessModelElement element)
        {
            this.element = element;
        }

        /// <summary>
        /// Imported object shape on page.
        /// </summary>
        protected Visio.Shape shape;
        public virtual void Import(string shapeType, Visio.Page page, IList<ISimple2DVisualizationPoint> bounds)
        {
            this.shape = VH.Place(shapeType, page);

            // hasModelComponentID
            VH.SetProp(shape, Constants.Properties.ID, element.getModelComponentID());
            // hasModelComponentLabel
            VH.SetProp(shape, Constants.Properties.Label, this.GetEnglishLabel(out IList<IStringWithExtra> otherLabels));
            foreach (IStringWithExtra otherLabel in otherLabels)
                VH.SetProp(shape, Constants.Properties.Label + otherLabel.getExtra().ToUpper(), otherLabel.getContent());
            // TODO: hasAdditionalAttribute into new Fields
            // some of element.getElementsWithUnspecifiedRelation()

            VH.SetProp(shape, Constants.Properties.Comment, string.Join(";", element.getComments()));

            // maybe extract positioning
            if (this.element is IHasSimple2DVisualizationBox
                && bounds != null && bounds.Count >= 2 && bounds[1].getRelative2DPosX() > 0)
            {
                // Seitenmasse einmal lesen — vorher vier COM-Aufrufe pro Shape
                double pageWidth = VH.GetCell(page.PageSheet, "PageWidth");
                double pageHeight = VH.GetCell(page.PageSheet, "PageHeight");

                // set position
                VH.SetCell(shape, "PinX", bounds[0].getRelative2DPosX() * pageWidth);
                VH.SetCell(shape, "PinY", bounds[0].getRelative2DPosY() * pageHeight);

                // set dimensions
                VH.SetCell(shape, "Width", bounds[1].getRelative2DPosX() * pageWidth);
                VH.SetCell(shape, "Height", bounds[1].getRelative2DPosY() * pageHeight);
            }
        }

        /// <summary>
        /// Separate english and non-english labels.
        /// </summary>
        /// <remarks>The non-english labels are stored in out-parameter <c>nonEnglishLabels</c>.</remarks>
        /// <returns>english label</returns>
        private string GetEnglishLabel(out IList<IStringWithExtra> nonEnglishLabels)
        {
            nonEnglishLabels = new List<IStringWithExtra>();
            IStringWithExtra englishLabel = null;

            foreach (IStringWithExtra label in element.getModelComponentLabels())
            {
                if (label.getExtra().ToLower() == "en") englishLabel = label;
                else nonEnglishLabels.Add(label);
            }

            if (englishLabel ==  null && nonEnglishLabels.Count > 0)
            {
                englishLabel = nonEnglishLabels[0];
                nonEnglishLabels.RemoveAt(0);
            }

            return englishLabel?.getContent();
        }

        public Visio.Shape GetShape()
        {
            return shape;
        }
    }
}
