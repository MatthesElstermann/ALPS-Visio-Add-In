using System.Collections.Generic;
using System.Linq;
using alps.net.api;
using alps.net.api.ALPS;
using alps.net.api.StandardPASS;
using static ALPS_Visio_AddIn_rewrite.VisioHelper;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class PASSProcessModelElementExport : IShapeExport
    {
        readonly IPASSProcessModelElement element;
        protected Visio.Shape shape;

        public PASSProcessModelElementExport(IPASSProcessModelElement element)
        {
            this.element = element;
        }

        public virtual void export(ShapeType shapeType, Visio.Page page, string masterType, IList<ISimple2DVisualizationPoint> points = null, IPASSProcessModelElement originalModelElement = null)
        {
            shape = place(shapeType, page, masterType, points, originalModelElement);

            // set ModelComponentID
            shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentID].Formula = "\"" + element.getModelComponentID() + "\"";

            // add labels
            string englishLabel = getEnglishLabel(element.getModelComponentLabels(), out IList<IStringWithExtra> otherLabels);
            if (englishLabel == null)
            {
                englishLabel = otherLabels.FirstOrDefault()?.getContent();
                if (otherLabels.Count > 0) otherLabels.RemoveAt(0);
            }
            shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeLabel].Formula = "\"" + englishLabel + "\"";
            foreach (IStringWithExtra otherLabel in otherLabels)
            {
                string newRowName = "label" + otherLabel.getExtra().ToUpper();
                shape.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, newRowName, (short)Visio.VisRowTags.visTagDefault);
                shape.CellsU["Prop." + newRowName].Formula = "\"" + otherLabel.getContent() + "\"";
            }

            // add comments
            if (element.getComments().Count > 0) shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeComment].Formula = "\"" + string.Join(";", element.getComments()) + "\"";

            // add type
            shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentType].Formula = "\"" + element.GetType() + "\"";
        }

        protected string getEnglishLabel(IList<IStringWithExtra> allLabels, out IList<IStringWithExtra> nonEnglishLabels)
        {
            nonEnglishLabels = new List<IStringWithExtra>();
            IStringWithExtra englishLabel = null;

            foreach (IStringWithExtra label in allLabels)
            {
                if (label.getExtra().ToLower() == "en") englishLabel = label;
                else nonEnglishLabels.Add(label);
            }

            return englishLabel?.getContent();
        }

        public Visio.Shape getShape()
        {
            return shape;
        }

        public void setShape(Visio.Shape shape)
        {
            this.shape = shape;
        }
    }
}
