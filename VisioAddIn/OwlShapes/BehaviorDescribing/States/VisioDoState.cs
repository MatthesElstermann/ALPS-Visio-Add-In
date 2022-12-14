
using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using System.Collections.Generic;
using System.Linq;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisioAddIn.OwlShapes
{
    public class VisioDoState : DoState, IVisioExportableWithShape
    {
        private string type = ALPSConstants.alpsSBDMasterDoState;
        private readonly IExportFunctionality export;

        protected VisioDoState()
        {
            export = new StateExport(this);
        }

        public VisioDoState(ISubjectBehavior behavior) : base(behavior) { export = new StateExport(this); }

        public void exportToVisio(Visio.Page currentPage, ISimple2DVisualizationBounds bounds = null)
        {
            export.export(VisioHelper.ShapeType.SBD, currentPage, type,
                                new List<ISimple2DVisualizationPoint>(getElementsWithUnspecifiedRelation().Values.OfType<ISimple2DVisualizationPoint>()));
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioDoState();
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
