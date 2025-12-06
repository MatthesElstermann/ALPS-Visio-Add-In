using alps.net.api.parsing;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    /// <summary>
    /// Visio class generalization
    /// </summary>
    public interface IVisioExportable // TODO: rename Import
    {
        /// <summary>
        /// Export this object onto given page.
        /// </summary>
        void ExportToVisio(Visio.Page page);
    }
}
