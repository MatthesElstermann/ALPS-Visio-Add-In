using alps.net.api.ALPS;
using System.Collections.Generic;
using vis = Microsoft.Office.Interop.Visio;

namespace VisioAddIn.OwlShapes.util
{
    

    public interface IPageExportHelper
    {
        vis.Shape place(string masterType, IList<ISimple2DVisualizationPoint> points = null);
    }
}
