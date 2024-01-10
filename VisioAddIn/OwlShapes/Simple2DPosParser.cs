
using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using System;

namespace VisioAddIn.OwlShapes
{
    public class Simple2DPosParser
    {
        private IPASSProcessModelElement element;
        private double posX = double.NaN, posY = double.NaN, width = double.NaN, height = double.NaN;
        private bool posXSet = false, posYSet = false, widthSet = false, heightSet = false;
        bool posParsed = false, boundsParsed = false;
        private string replaceWith = "", replace = "";

        public Simple2DPosParser(IPASSProcessModelElement element)
        {
            this.element = element;
            if (double.Parse("0.5") < 1)
            {
                replace = ",";
                replaceWith = ".";
            }
            else if (double.Parse("0,5") < 1)
            {
                replace = ".";
                replaceWith = ",";
            }
        }

        public bool parseAttribute(string predicate, string objectContent, string lang, string dataType, IParseablePASSProcessModelElement element)
        {
            bool parsed = false;
            
            string possibleDouble = objectContent.Replace(replace,replaceWith);
            if (predicate.Contains("hasRelative2D_PosX"))
            {
                posX = double.Parse(possibleDouble);
                posXSet = true;
                parsed = true;
            }
            if (predicate.Contains("hasRelative2D_PosY"))
            {
                posY = double.Parse(possibleDouble);
                posYSet = true;
                parsed = true;
            }
            if (predicate.Contains("hasRelative2D_Height"))
            {
                height = double.Parse(possibleDouble);
                widthSet = true;
                parsed = true;
            }
            if (predicate.Contains("hasRelative2D_Width"))
            {
                width = double.Parse(possibleDouble);
                heightSet = true;
                parsed = true;
            }
            if (parsed) checkCompleted();
            return parsed;
        }

        private void checkCompleted()
        {
            if (posXSet && posYSet && !posParsed)
            {
                ISimple2DVisualizationPoint point = new Simple2DVisualizationPoint("PosFor" + element.getModelComponentID(), "", "RelativePosition");
                //point.setRelative2DPosX(posX);
                //point.setRelative2DPosY(posY);
                point.setRelative2DPosX(posX);
                point.setRelative2DPosY(posY);
                posParsed = true;
                element.addElementWithUnspecifiedRelation(point);
            }
            if (widthSet && heightSet && !boundsParsed)
            {
                ISimple2DVisualizationPoint point = new Simple2DVisualizationBounds("BoundsFor" + element.getModelComponentID(), "", "Bounds");
                point.setRelative2DPosX(posX);
                point.setRelative2DPosX(posX);
                boundsParsed = true;
                element.addElementWithUnspecifiedRelation(point);
            }
        }

    }
}
