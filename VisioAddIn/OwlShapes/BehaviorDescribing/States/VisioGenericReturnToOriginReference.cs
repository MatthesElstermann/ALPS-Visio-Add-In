using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using System.Collections.Generic;
using System.Linq;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisioAddIn.OwlShapes
{
    public class VisioGenericReturnToOriginReference : GenericReturnToOriginReference, IVisioExportableWithShape
    {
        private string type = ALPSConstants.alpsSBDMasterGenericReturnToOriginReference;
        private readonly IExportFunctionality export;

        protected VisioGenericReturnToOriginReference()
        {
            export = new StateExport(this);
        }

        public VisioGenericReturnToOriginReference(ISubjectBehavior behavior) : base(behavior) { export = new StateExport(this); }

        public void exportToVisio(Visio.Page currentPage, ISimple2DVisualizationBounds bounds = null)
        {
            export.export(VisioHelper.ShapeType.SBD, currentPage, type,
                                new List<ISimple2DVisualizationPoint>(getElementsWithUnspecifiedRelation().Values.OfType<ISimple2DVisualizationPoint>()));
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioGenericReturnToOriginReference();
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
