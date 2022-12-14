using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Visio;

namespace VisioAddIn.util
{
    /// <summary>
    /// A utility class that stores the corner coordinates of a shape as vectors.
    /// </summary>
    public class ShapeCorners
    {
        public Vector2D shapeTLC { get; set; }
        public Vector2D shapeBLC { get; set; }
        public Vector2D shapeTRC { get; set; }
        public Vector2D shapeBRC { get; set; }

        public ShapeCorners(ShapeEdges edges)
        {
            shapeTLC = new Vector2D(edges.left, edges.top);
            shapeBLC = new Vector2D(edges.left, edges.bottom);
            shapeTRC = new Vector2D(edges.right, edges.top);
            shapeBRC = new Vector2D(edges.right, edges.bottom);
        }
        public ShapeCorners(IVShape shape) : this(new ShapeEdges(shape))
        { }

        /// <summary>
        /// Checks whether at least one of the own corner vectors is close to one of the passed corner vectors
        /// </summary>
        public bool isCloseToAtLeastOneOtherCorner(ShapeCorners vectors)
        {
            return shapeTLC.isNearTo(vectors.shapeTLC)
                   || shapeBLC.isNearTo(vectors.shapeBLC)
                   || shapeTRC.isNearTo(vectors.shapeTRC)
                   || shapeBRC.isNearTo(vectors.shapeBRC);
        }
    }
}
