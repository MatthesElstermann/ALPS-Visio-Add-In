using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioGenericReturnToOriginReference : GenericReturnToOriginReference, IVisioImportableWithShape
    {
        private const string shapeType = Constants.SBDMasters.GenericReturnToOriginReference;

        private readonly IShapeImport import;
        public VisioGenericReturnToOriginReference(ISubjectBehavior behavior) : base(behavior) { import = new StateImport(this); }
        protected VisioGenericReturnToOriginReference() { import = new StateImport(this); }

        public void ImportToVisio(Visio.Page page)
        {
            import.Import(shapeType, page, VH.GetBounds(this));

            // maybe implement default State
        }

        public bool PrepareDimensions()
        {
            if (this is IHasSimple2DVisualizationBox bounds && bounds.getRelative2DWidth() > 0)
            {
                Simple2DVisualizationPoint point = new Simple2DVisualizationPoint();
                point.setRelative2DPosX(bounds.getRelative2DPosX());
                point.setRelative2DPosY(bounds.getRelative2DPosY());
                this.addElementWithUnspecifiedRelation(point);

                Simple2DVisualizationPoint bound = new Simple2DVisualizationPoint();
                bound.setRelative2DPosX(bounds.getRelative2DWidth());
                bound.setRelative2DPosY(bounds.getRelative2DHeight());
                this.addElementWithUnspecifiedRelation(bound);

                return true;
            }
            else return false;
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioGenericReturnToOriginReference();
        }

        public Visio.Shape GetShape()
        {
            return import.GetShape();
        }
    }
}