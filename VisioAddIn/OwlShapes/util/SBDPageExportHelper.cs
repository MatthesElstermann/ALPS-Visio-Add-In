
using alps.net.api.ALPS;
using System;
using System.Collections.Generic;
using vis = Microsoft.Office.Interop.Visio;

namespace VisioAddIn.OwlShapes.util
{
    public class SBDPageExportHelper : PageExportHelper
    {
        public SBDPageExportHelper(vis.Page page) : base(page)
        { }

        public override vis.Shape place(string masterType, IList<ISimple2DVisualizationPoint> points = null)
        {
            throw new NotImplementedException();
        }
    }
}
