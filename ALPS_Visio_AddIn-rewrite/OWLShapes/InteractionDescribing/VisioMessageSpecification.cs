using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using Visio = Microsoft.Office.Interop.Visio;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;
using System.Diagnostics;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioMessageSpecification : MessageSpecification, IVisioExportableWithShape
    {
        private const string shapeType = Constants.SIDMasters.Message;

        private readonly IShapeExport export;
        public VisioMessageSpecification(IModelLayer layer) : base(layer) { export = new PASSProcessModelElementExport(this); }
        protected VisioMessageSpecification() { export = new PASSProcessModelElementExport(this); }

        public void ExportToVisio(Visio.Page page)
        {
            if (this.GetShape() != null) return;

            export.Export(shapeType, page, VH.GetBounds(this));

            // TODO: containsPayloadDescription
        }

        public bool PrepareDimensions() // TODO: prepare dimensions
        {
            return false;
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioMessageSpecification();
        }

        public Visio.Shape GetShape()
        {
            return export.GetShape();
        }
    }
}