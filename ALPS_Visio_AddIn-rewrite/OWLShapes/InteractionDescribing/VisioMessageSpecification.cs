using System.Collections.Generic;
using System.Linq;
using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioMessageSpecification : MessageSpecification, IVisioExportableWithShape
    {
        private const string type = ALPSConstants.alpsSIDMasterMessage;
        private readonly IShapeExport export;

        public VisioMessageSpecification(IModelLayer layer) : base(layer)
        {
            export = new MessageSpecificationExport(this);
        }

        protected VisioMessageSpecification()
        {
            export = new MessageSpecificationExport(this);
        }

        public void exportToVisio(Visio.Page currentPage)
        {
            if (getShape() != null) return;

            export.export(VisioHelper.ShapeType.SID, currentPage, type, new List<ISimple2DVisualizationPoint>(getElementsWithUnspecifiedRelation().Values.OfType<ISimple2DVisualizationPoint>()), this);
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioMessageSpecification();
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
            return false;
        }
    }
}