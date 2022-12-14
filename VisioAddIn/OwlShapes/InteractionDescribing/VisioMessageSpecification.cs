
using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using System.Collections.Generic;
using System.Linq;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisioAddIn.OwlShapes
{
    public class VisioMessageSpecification : MessageSpecification, IVisioExportableWithShape
    {
        private const string type = ALPSConstants.alpsSIDMasterMessage;
        private readonly IExportFunctionality export;

        protected VisioMessageSpecification() { export = new PASSProcessModelElementExport(this); }

        public VisioMessageSpecification(IModelLayer layer) : base(layer) { export = new PASSProcessModelElementExport(this); }

        public void exportToVisio(Visio.Page currentPage, ISimple2DVisualizationBounds bounds = null)
        {
            if (getShape() != null) return;
            export.export(VisioHelper.ShapeType.SID, currentPage, type,
                                new List<ISimple2DVisualizationPoint>(getElementsWithUnspecifiedRelation().Values.OfType<ISimple2DVisualizationPoint>()));

            System.Array gluedShapes = getShape().GluedShapes(Visio.VisGluedShapesFlags.visGluedShapesAll1D, "");

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
    }
}
