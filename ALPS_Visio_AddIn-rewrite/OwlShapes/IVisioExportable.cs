using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public interface IVisioExportable
    {
        void exportToVisio(Visio.Page currentPage, ISimple2DVisualizationBounds bounds = null);

    }
}
