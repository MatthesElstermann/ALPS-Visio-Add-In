using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioSubjectGroup : SubjectGroup, IVisioImportableWithShape
    {
        private const string shapeType = Constants.SIDMasters.SubjectGroup;

        private readonly IShapeImport import;
        public VisioSubjectGroup(IModelLayer layer) : base(layer) { import = new SubjectImport(this); }
        protected VisioSubjectGroup() { import = new SubjectImport(this); }

        public void ImportToVisio(Visio.Page page)
        {
            import.Import(shapeType, page, VH.GetBounds(this));
        }

        public bool PrepareDimensions()
        {
            return VisualizationBounds.Prepare(this);
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioSubjectGroup();
        }

        public Visio.Shape GetShape()
        {
            return import.GetShape();
        }
    }
}
