using System.Collections.Generic;
using alps.net.api.ALPS;
using alps.net.api.StandardPASS;
using static ALPS_Visio_AddIn_rewrite.VisioHelper;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public interface IShapeExport
    {
        void export(ShapeType shapeType, Visio.Page page, string masterType, IList<ISimple2DVisualizationPoint> points = null, IPASSProcessModelElement originalElement = null);

        Visio.Shape getShape();

        void setShape(Visio.Shape shape);
    }
}