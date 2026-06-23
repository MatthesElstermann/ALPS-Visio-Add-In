using Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite
{
    public class ShapeEdges
    {
        public double left { get; set; }
        public double right { get; set; }
        public double bottom { get; set; }
        public double top { get; set; }

        public ShapeEdges(ShapeGeometry geometry)
        {
            left   = geometry.centerX - (0.5 * geometry.width);
            right  = geometry.centerX + (0.5 * geometry.width);
            bottom = geometry.centerY - (0.5 * geometry.height);
            top    = geometry.centerY + (0.5 * geometry.height);
        }

        public ShapeEdges(IVShape shape) : this(new ShapeGeometry(shape)) { }
    }
}
