using alps.net.api.ALPS;
using System.Collections.Generic;
using vis = Microsoft.Office.Interop.Visio;

namespace VisioAddIn.OwlShapes.util
{
    public class SIDPageExportHelper : PageExportHelper
    {
        public SIDPageExportHelper(vis.Page page) : base(page)
        { }

        private IList<ISimple2DVisualizationPoint> placedPoints = new List<ISimple2DVisualizationPoint>();

        public override vis.Shape place(string masterType, IList<ISimple2DVisualizationPoint> points = null)
        {
            vis.Document shapes = VisioHelper.openStencil(VisioHelper.VisioStencils.SID_STENCIL); ;
            ISimple2DVisualizationBounds bound = null;
            double defaultX = 2, defaultY = 10, simpleXPos = defaultX, simpleYPos = defaultY;

            if (!(points is null) && (points.Count > 0))
                foreach (ISimple2DVisualizationPoint point in points)
                {
                    if (!(point is ISimple2DVisualizationBounds))
                    {
                        simpleXPos = point.getRelative2DPosX();
                        simpleYPos = point.getRelative2DPosX();
                    }
                    else if (point is ISimple2DVisualizationBounds boundObject)
                    {
                        bound = boundObject;
                    }
                }
            else 
            {
                if (placedPoints.Count > 0)
                {
                    ISimple2DVisualizationPoint lastPoint = placedPoints[placedPoints.Count - 1];
                    simpleXPos = lastPoint.getRelative2DPosX() + 4;
                    simpleYPos = lastPoint.getRelative2DPosY();
                    if (simpleXPos > 10)
                    {
                        simpleXPos = 2;
                        simpleYPos -= 4;
                    }
                }
            }
            if (shapes != null)
            {
                vis.Master sidMaster = shapes.Masters.get_ItemU(masterType);

                // Keep track of all the points shapes have been placed to page
                ISimple2DVisualizationPoint placedPoint = new Simple2DVisualizationPoint();
                placedPoint.setRelative2DPosX(simpleXPos);
                placedPoint.setRelative2DPosY(simpleYPos);
                placedPoints.Add(placedPoint);

                // Drop the shape
                vis.Shape droppedShape = page.Drop(sidMaster, simpleXPos, simpleYPos);
                return droppedShape;
            }

            return null;
        }
    }
}
