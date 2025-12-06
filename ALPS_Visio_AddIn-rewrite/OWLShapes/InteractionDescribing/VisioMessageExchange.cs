using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioMessageExchange : MessageExchange, IVisioExportableWithShape
    {
        private const string shapeType = Constants.SIDMasters.StandardMessageConnector;

        private readonly IShapeExport export;
        public VisioMessageExchange(IModelLayer layer) : base(layer) { export = new PASSProcessModelElementExport(this); }
        protected VisioMessageExchange() { export = new PASSProcessModelElementExport(this); }
        public void ExportToVisio(Visio.Page page)
        {
            export.Export(shapeType, page, VH.GetBounds(this));

            // set path (auto arrange)
            if (this.getSender() is IVisioExportableWithShape exportableSender)
                this.GetShape().CellsU["BeginX"].GlueToPos(exportableSender.GetShape(), 1, 0.5);
            if (this.getReceiver() is IVisioExportableWithShape exportableReceiver)
                this.GetShape().CellsU["EndY"].GlueToPos(exportableReceiver.GetShape(), 0, 0.5);

            // TODO: AbstractMessageExchange
            // TODO: FinalizedMessageExchange -> alps.net.api
        }

        public bool PrepareDimensions() // TODO: prepare dimensions
        {
            return false; // routing follow points
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioMessageExchange();
        }

        public Visio.Shape GetShape()
        {
            return export.GetShape();
        }
    }
}