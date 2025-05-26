using Visio = Microsoft.Office.Interop.Visio;

namespace VisioAddIn.OwlShapes
{
    public interface IVisioExportableWithShape : IVisioExportable
    {

        Visio.Shape getShape();

        void setShape(Visio.Shape shape);
    }
}
