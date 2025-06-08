using alps.net.api.ALPS;
using System.Collections.Generic;
using System.Linq;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using Microsoft.Office.Interop.Visio;
using alps.net.api.util;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioSendState : SendState, IVisioExportableWithShape
    {
        private const string type = ALPSConstants.alpsSBDMasterSendState;
        private readonly IShapeExport export;

        public VisioSendState(ISubjectBehavior behavior) : base(behavior)
        {
            export = new StateExport(this);
        }

        protected VisioSendState()
        {
            export = new StateExport(this);
        }

        public void exportToVisio(Page currentPage)
        {
            export.export(VisioHelper.ShapeType.SBD, currentPage, type, new List<ISimple2DVisualizationPoint>(getElementsWithUnspecifiedRelation().Values.OfType<ISimple2DVisualizationPoint>()), this);
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioSendState();
        }

        public Shape getShape()
        {
            return export.getShape();
        }

        public void setShape(Shape shape)
        {
            export.setShape(shape);
        }

        public bool prep2DInfo()
        {
            if (this is IHasSimple2DVisualizationBox bounds)
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
    }
}