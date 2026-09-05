using alps.net.api.parsing;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    /// <summary>
    /// Visio class generalization
    /// </summary>
    public interface IVisioImportable
    {
        /// <summary>
        /// Import this object onto given page.
        /// </summary>
        void ImportToVisio(Visio.Page page);
    }
}
