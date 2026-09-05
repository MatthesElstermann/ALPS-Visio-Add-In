using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioMessageExchange : MessageExchange, IVisioImportableWithShape
    {
        private const string shapeType = Constants.SIDMasters.StandardMessageConnector;

        private readonly IShapeImport import;
        public VisioMessageExchange(IModelLayer layer) : base(layer) { import = new PASSProcessModelElementImport(this); }
        protected VisioMessageExchange() { import = new PASSProcessModelElementImport(this); }
        public void ImportToVisio(Visio.Page page)
        {
            import.Import(shapeType, page, VH.GetBounds(this));

            // set path (auto arrange)
            if (this.getSender() is IVisioImportableWithShape importableSender)
                this.GetShape().CellsU["BeginX"].GlueToPos(importableSender.GetShape(), 1, 0.5);
            if (this.getReceiver() is IVisioImportableWithShape importableReceiver)
                this.GetShape().CellsU["EndY"].GlueToPos(importableReceiver.GetShape(), 0, 0.5);

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
            return import.GetShape();
        }
    }
}