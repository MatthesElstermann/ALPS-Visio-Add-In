using System.Collections.Generic;
using System.Linq;
using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioInterfaceSubject : InterfaceSubject, IVisioExportableWithShape
    {
        private const string type = ALPSConstants.alpsSIDMasterInterfaceActor;
        private readonly IShapeExport export;

        public VisioInterfaceSubject(IModelLayer layer) : base(layer)
        {
            export = new SubjectExport(this);
        }

        protected VisioInterfaceSubject()
        {
            export = new SubjectExport(this);
        }

        public void exportToVisio(Visio.Page currentPage)
        {
            export.export(VisioHelper.ShapeType.SID, currentPage, type, new List<ISimple2DVisualizationPoint>(getElementsWithUnspecifiedRelation().Values.OfType<ISimple2DVisualizationPoint>()), this);
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioInterfaceSubject();
        }

        public Visio.Shape getShape()
        {
            return export.getShape();
        }

        public void setShape(Visio.Shape shape)
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