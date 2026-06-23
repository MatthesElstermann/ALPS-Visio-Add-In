using System.Collections.Generic;
using alps.net.api.ALPS;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    /// <summary>
    /// Shape import generalization
    /// </summary>
    public interface IShapeImport
    {
        /// <summary>
        /// Import given shape onto given page with given bounds.
        /// </summary>
        void Import(string shapeType, Visio.Page page, IList<ISimple2DVisualizationPoint> bounds);

        /// <returns>object shape on page</returns>
        Visio.Shape GetShape();
    }
}