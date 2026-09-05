using Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite
{
    /// <summary>
    /// A class that stores the main coordinates of a shape which can be obtained from the ShapeSheet
    /// </summary>
    public class ShapeGeometry
    {
        public double width { get; set; }
        public double height { get; set; }
        public double centerX { get; set; }
        public double centerY { get; set; }

        public ShapeGeometry(IVShape shape)
        {
            centerX = shape.CellsU["PinX"].Result[VisUnitCodes.visMillimeters];
            centerY = shape.CellsU["PinY"].Result[VisUnitCodes.visMillimeters];
            width   = shape.CellsU["Width"].Result[VisUnitCodes.visMillimeters];
            height  = shape.CellsU["Height"].Result[VisUnitCodes.visMillimeters];
        }
    }
}
