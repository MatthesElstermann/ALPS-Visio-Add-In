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
    public class PASSProcessModelElementExport : IShapeExport
    {
        private readonly IPASSProcessModelElement element;

        /// <summary>
        /// Base shape export for elements
        /// </summary>
        public PASSProcessModelElementExport(IPASSProcessModelElement element)
        {
            this.element = element;
        }

        /// <summary>
        /// Exported object shape on page.
        /// </summary>
        protected Visio.Shape shape;
        public virtual void Export(string shapeType, Visio.Page page, IList<ISimple2DVisualizationPoint> bounds)
        {
            this.shape = VH.Place(shapeType, page);

            // hasModelComponentID
            VH.SetProperty(shape, Constants.Properties.ID, element.getModelComponentID());
            // hasModelComponentLabel
            VH.SetProperty(shape, Constants.Properties.Label, this.GetEnglishLabel(out IList<IStringWithExtra> otherLabels));
            foreach (IStringWithExtra otherLabel in otherLabels)
                VH.SetProperty(shape, Constants.Properties.Label + otherLabel.getExtra().ToUpper(), otherLabel.getContent());
            // TODO: hasAdditionalAttribute into new Fields
            // some of element.getElementsWithUnspecifiedRelation()

            VH.SetProperty(shape, Constants.Properties.Comment, string.Join(";", element.getComments()));

            // maybe extract positioning
            if (this is IHasSimple2DVisualizationBox)
            {
                // set position
                VH.SetSize(shape, "PinX", bounds[0].getRelative2DPosX() * VH.GetSize(page.PageSheet, "PageWidth"));
                VH.SetSize(shape, "PinY", bounds[0].getRelative2DPosY() * VH.GetSize(page.PageSheet, "PageHeight"));

                // set dimensions
                VH.SetSize(shape, "Width", bounds[1].getRelative2DPosX() * VH.GetSize(page.PageSheet, "PageWidth"));
                VH.SetSize(shape, "Height", bounds[1].getRelative2DPosY() * VH.GetSize(page.PageSheet, "PageHeight"));
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
