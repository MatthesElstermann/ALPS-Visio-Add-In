using System.Collections.Generic;
using alps.net.api.ALPS;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    /// <summary>
    /// Shape export generalization
    /// </summary>
    public interface IShapeExport
    {
        /// <summary>
        /// Export given shape onto given page with given bounds.
        /// </summary>
        void Export(string shapeType, Visio.Page page, IList<ISimple2DVisualizationPoint> bounds);

        /// <returns>object shape on page</returns>
        Visio.Shape GetShape();
    }
}