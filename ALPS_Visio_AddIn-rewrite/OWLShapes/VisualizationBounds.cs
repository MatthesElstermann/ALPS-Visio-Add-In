using alps.net.api.ALPS;
using alps.net.api.StandardPASS;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    /// <summary>
    /// Shared implementation of <see cref="IVisioImportableWithShape.PrepareDimensions"/> for all
    /// elements whose coordinates come from a <see cref="IHasSimple2DVisualizationBox"/> (subjects
    /// and states). Converts the box into the two <see cref="Simple2DVisualizationPoint"/>s
    /// (position + extent) that the import helpers read back via
    /// <c>getElementsWithUnspecifiedRelation()</c>.
    /// </summary>
    public static class VisualizationBounds
    {
        /// <summary>
        /// Prepares the element's dimension points from its visualization box.
        /// </summary>
        /// <returns><c>true</c> if the element carries coordinates, otherwise <c>false</c>
        /// (the caller then falls back to the auto-layout)</returns>
        public static bool Prepare(PASSProcessModelElement element)
        {
            if (element is IHasSimple2DVisualizationBox bounds && bounds.getRelative2DWidth() > 0)
            {
                Simple2DVisualizationPoint point = new Simple2DVisualizationPoint();
                point.setRelative2DPosX(bounds.getRelative2DPosX());
                point.setRelative2DPosY(bounds.getRelative2DPosY());
                element.addElementWithUnspecifiedRelation(point);

                Simple2DVisualizationPoint bound = new Simple2DVisualizationPoint();
                bound.setRelative2DPosX(bounds.getRelative2DWidth());
                bound.setRelative2DPosY(bounds.getRelative2DHeight());
                element.addElementWithUnspecifiedRelation(bound);

                return true;
            }
            return false;
        }
    }
}
