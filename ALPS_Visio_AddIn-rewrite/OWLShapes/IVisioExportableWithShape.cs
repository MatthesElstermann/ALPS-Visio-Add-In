using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public interface IVisioExportableWithShape : IVisioExportable
    {
        Visio.Shape getShape();

        void setShape(Visio.Shape shape);
    }
}
