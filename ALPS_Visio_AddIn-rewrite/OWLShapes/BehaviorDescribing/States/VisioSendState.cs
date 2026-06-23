using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioSendState : SendState, IVisioExportableWithShape
    {
        private const string shapeType = Constants.SBDMasters.SendState;
        
        private readonly IShapeExport export;
        public VisioSendState(ISubjectBehavior behavior) : base(behavior) { export = new StateExport(this); }
        protected VisioSendState() { export = new StateExport(this); }

        public void ExportToVisio(Visio.Page page)
        {
            export.Export(shapeType, page, VH.GetBounds(this));
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
            return new VisioSendState();
        }

        public Visio.Shape GetShape()
        {
            return export.GetShape();
        }
    }
}