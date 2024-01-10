
using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using System.Collections.Generic;
using System.Linq;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisioAddIn.OwlShapes
{
    public class VisioSubjectGroup : SubjectGroup, IVisioExportableWithShape
    {
        private const string type = ALPSConstants.alpsSIDMasterSubjectGroup;
        private readonly IExportFunctionality export;

        protected VisioSubjectGroup() { export = new SubjectExport(this); }

        public VisioSubjectGroup(IModelLayer layer) : base(layer) { export = new SubjectExport(this); }

        public void exportToVisio(Visio.Page currentPage, ISimple2DVisualizationBounds bounds = null)
        {
            // Place the shape onto the SID page
            export.export(VisioHelper.ShapeType.SID, currentPage, type,
                                new List<ISimple2DVisualizationPoint>(getElementsWithUnspecifiedRelation().Values.OfType<ISimple2DVisualizationPoint>()), this  );


        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioSubjectGroup();
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
