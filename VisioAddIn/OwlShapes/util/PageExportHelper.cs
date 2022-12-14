using alps.net.api.ALPS;
using System.Collections.Generic;
using vis = Microsoft.Office.Interop.Visio;

namespace VisioAddIn.OwlShapes.util
{
    public abstract class PageExportHelper : IPageExportHelper
    {
        protected vis.Page page;

        public PageExportHelper(vis.Page page)
        {
            this.page = page;
        }

        public abstract vis.Shape place(string masterType, IList<ISimple2DVisualizationPoint> points = null);
    }
}
