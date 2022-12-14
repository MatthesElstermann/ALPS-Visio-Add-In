using alps.net.api;
using alps.net.api.ALPS;
using alps.net.api.StandardPASS;
using System.Collections.Generic;
using static VisioAddIn.VisioHelper;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisioAddIn.OwlShapes
{
    public class PASSProcessModelElementExport : IExportFunctionality
    {
        readonly IPASSProcessModelElement element;
        protected Visio.Shape shape;

        public PASSProcessModelElementExport(IPASSProcessModelElement element)
        {
            this.element = element;
        }


        public virtual void export(ShapeType shapeType, Visio.Page page, string masterType, IList<ISimple2DVisualizationPoint> points = null)
        {
            shape = place(shapeType, page, masterType, points);

            // Set the ModelComponentID
            shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentID].Formula = "\"" + element.getModelComponentID() + "\"";

            // Add the labels to the shape
            string englishLabel = getEnglishLabel(element.getModelComponentLabels(), out IList<IStringWithExtra> otherLabels);
            if (englishLabel != null)
            {
                // Fill in the default label as the english one
                shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeLabel].Formula = "\"" + englishLabel + "\"";
                if (otherLabels.Count > 0) {
                    foreach (IStringWithExtra otherLabel in otherLabels)
                    {
                        // Create own fields for other languages
                        string newRowName = "label" + otherLabel.getExtra().ToUpper();
                        shape.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, newRowName, (short)Visio.VisRowTags.visTagDefault);
                        shape.CellsU["Prop." + newRowName].Formula = "\"" + otherLabel.getContent() + "\"";
                    }
                }
            }
            else
            {
                if (otherLabels.Count > 0)
                {
                    // If no english label exists, fill in the default with the first label given
                    shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeLabel].Formula = "\"" + otherLabels[0].getContent() + "\"";
                    for (int i = 1; i <  otherLabels.Count; i++)
                    {
                        // And create own fields for the others
                        string newRowName = "label" + otherLabels[i].getExtra().ToUpper();
                        shape.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, newRowName, (short)Visio.VisRowTags.visTagDefault);
                        shape.CellsU["Prop." + newRowName].Formula = "\"" + otherLabels[i].getContent() + "\"";
                    }
                }
            }

            // Add the comments (if multiple as one big comment) to the shape
            if (element.getComments().Count > 0)
            {
                string completeComment = string.Join(";", element.getComments());
                shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeComment].Formula = "\"" + completeComment + "\"";
            }

            // Add the type to the shape
            shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentType].Formula = "\"" + element.GetType() + "\"";
        }

        public Visio.Shape getShape()
        {
            return shape;
        }

        public void setShape(Visio.Shape shape)
        {
            this.shape = shape;
        }


        protected string getEnglishLabel(IList<IStringWithExtra> allLabels, out IList<IStringWithExtra> nonEnglishLabels)
        {
            int englishLabel = -1;
            for (int i = 0; i < allLabels.Count; i++)
            {
                if (allLabels[i].getExtra().ToLower().Equals("en"))
                {
                    englishLabel = i;
                }
            }
            nonEnglishLabels = new List<IStringWithExtra>(allLabels);
            if (englishLabel != -1)
            {
                nonEnglishLabels.RemoveAt(englishLabel);
                return allLabels[englishLabel].getContent();
            }
            return null;
        }
    }
}
