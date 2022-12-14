using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vis = Microsoft.Office.Interop.Visio;

namespace VisioAddIn.OwlShapes.util
{
    public enum PageType
    {
        SBD, SID
    }

    public static class ExportHelperFactory
    {
        private static readonly IDictionary<vis.Page, IPageExportHelper> helpers = new Dictionary<vis.Page, IPageExportHelper>();

        public static IPageExportHelper getExportHelperForPage(vis.Page page)
        {
            if (!helpers.ContainsKey(page))
            {
                string pagetype = page.PageSheet.CellsU["Prop.pageType"].ResultStrU[""] ;
                if (pagetype.Equals("SubjectInteraction"))
                    helpers.Add(page, new SIDPageExportHelper(page));
                else if (pagetype.Equals("SubjectBehavior"))
                    helpers.Add(page, new SBDPageExportHelper(page));

            }
            return helpers[page];
        }
    }
}
