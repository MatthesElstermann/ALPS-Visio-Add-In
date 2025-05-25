
using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using System;
using System.Diagnostics;

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

            Debug.WriteLine("PREDICATE: " + predicate);
            Debug.WriteLine("  " + objectContent);
            
            string possibleDouble = objectContent.Replace(replace,replaceWith); // elements do not contain 2D data // nvm it does?
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
                heightSet = true;
                parsed = true;
            }
            if (predicate.Contains("hasRelative2D_Width"))
            {
                width = double.Parse(possibleDouble);
                widthSet = true;
                parsed = true;
            }
            if (parsed)
            {
                Debug.WriteLine("  " + parsed);
                checkCompleted();
            }
            return parsed;
        }

        private void checkCompleted() // TODO: LQ: this gets called to set the coordinates
        {
            Debug.WriteLine("checkCompleted() called");
            Debug.WriteLine("Flags: (" + posXSet + ", " + posYSet + ", " + widthSet + ", " + heightSet + ")");
            Debug.WriteLine("Parsed: " + posParsed + ", " + boundsParsed);

            if (posXSet && posYSet && !posParsed)
            {
                Debug.WriteLine("    xy: " + posX + "/" + posY);
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
                Debug.WriteLine("    wh: " + width + "/" + height);
                ISimple2DVisualizationPoint bounds = new Simple2DVisualizationBounds("BoundsFor" + element.getModelComponentID(), "", "Bounds"); // wtf is this
                bounds.setRelative2DPosX(width);
                bounds.setRelative2DPosY(height);
                boundsParsed = true;
                element.addElementWithUnspecifiedRelation(bounds);
            }
        }

    }
}
