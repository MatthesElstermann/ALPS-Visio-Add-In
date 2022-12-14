using Visio = Microsoft.Office.Interop.Visio;

namespace VisioAddIn.OwlShapes
{
    public interface IVisioExportable
    {
        void exportToVisio(Visio.Page currentPage, ISimple2DVisualizationBounds bounds = null);

    }
}
