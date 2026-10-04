using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioGuardExtension : GuardExtension, IVisioImportableWithShape
    {
        // Eigener Master "Guard Extension" (Kategorie GuardExtension, extensionType "Guard
        // Extension") — frueher wurden alle Extension-Typen als "Subject Extension" gezeichnet.
        // Aeltere Schablonen ohne diesen Master fallen auf ActorExtension zurueck.
        private static string shapeType => VH.MasterOrFallback(Constants.SIDMasters.GuardExtension, Constants.SIDMasters.ActorExtension);

        private readonly IShapeImport import;
        public VisioGuardExtension(IModelLayer layer) : base(layer) { import = new SubjectImport(this); }
        protected VisioGuardExtension() { import = new SubjectImport(this); }

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
            return new VisioGuardExtension();
        }

        public Visio.Shape GetShape()
        {
            return import.GetShape();
        }
    }
}
