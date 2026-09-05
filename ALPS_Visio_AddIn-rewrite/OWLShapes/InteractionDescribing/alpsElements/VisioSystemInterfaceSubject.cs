using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioSystemInterfaceSubject : SystemInterfaceSubject, IVisioImportableWithShape
    {
        private const string shapeType = Constants.SIDMasters.SystemInterfaceSubject;

        private readonly IShapeImport import;
        public VisioSystemInterfaceSubject(IModelLayer layer) : base(layer) { import = new SubjectImport(this); }
        protected VisioSystemInterfaceSubject() { import = new SubjectImport(this); }

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
            return new VisioSystemInterfaceSubject();
        }

        public Visio.Shape GetShape()
        {
            return import.GetShape();
        }
    }
}
