using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioMacroExtension : MacroExtension, IVisioImportableWithShape
    {
        // Uses the generic ActorExtension master: a dedicated "MakroExtension" master
        // does not exist in the SID stencil (the legacy add-in used ActorExtension for all
        // extension types). Dropping a non-existent master throws a COMException.
        private const string shapeType = Constants.SIDMasters.ActorExtension;

        private readonly IShapeImport import;
        public VisioMacroExtension(IModelLayer layer) : base(layer) { import = new SubjectImport(this); }
        protected VisioMacroExtension() { import = new SubjectImport(this); }

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
            return new VisioMacroExtension();
        }

        public Visio.Shape GetShape()
        {
            return import.GetShape();
        }
    }
}
