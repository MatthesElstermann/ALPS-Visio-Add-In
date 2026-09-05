using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    /// <summary>
    /// Shaped Visio class generalization
    /// </summary>
    public interface IVisioImportableWithShape : IVisioImportable
    {
        /// <summary>
        /// Read dimensions for this object.
        /// </summary>
        /// <remarks>
        /// The dimensions are stored in
        /// <code>  getElementsWithUnspecifiedRelation().Values.OfType&lt;ISimple2DVisualizationPoint&gt;()</code>
        /// </remarks>
        /// <returns><c>true</c> if this object has dimensions, otherwise <c>false</c></returns>
        bool PrepareDimensions();

        /// <summary>
        /// Passthrough for <c>IShapeImport#GetShape</c>
        /// </summary>
        /// <returns>object shape on page</returns>
        Visio.Shape GetShape();
    }
}
