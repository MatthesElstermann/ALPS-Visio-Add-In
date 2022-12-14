using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Visio;

namespace VisioAddIn.util
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
            centerX = shape.CellsU[ALPSConstants.shapeCellShapeTransformPinX].Result[VisUnitCodes.visMillimeters];
            centerY = shape.CellsU[ALPSConstants.shapeCellShapeTransformPinY].Result[VisUnitCodes.visMillimeters];
            width = shape.CellsU[ALPSConstants.shapeCellShapeTransformWidth].Result[VisUnitCodes.visMillimeters];
            height = shape.CellsU[ALPSConstants.shapeCellShapeTransformHeight].Result[VisUnitCodes.visMillimeters];
        }
    }
}
