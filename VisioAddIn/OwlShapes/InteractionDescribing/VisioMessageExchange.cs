
using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using System.Collections.Generic;
using System.Linq;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisioAddIn.OwlShapes
{
    public class VisioMessageExchange : MessageExchange, IVisioExportableWithShape
    {
        private const string type = ALPSConstants.alpsSIDMasterStandardMessageConnector;
        private readonly IExportFunctionality export;

        protected VisioMessageExchange() { export = new PASSProcessModelElementExport(this); }

        public VisioMessageExchange(IModelLayer layer) : base(layer) { export = new PASSProcessModelElementExport(this); }

        public void exportToVisio(Visio.Page currentPage, ISimple2DVisualizationBounds bounds = null)
        {
            if (getShape() != null) return;
            export.export(VisioHelper.ShapeType.SID, currentPage, type,
                                new List<ISimple2DVisualizationPoint>(getElementsWithUnspecifiedRelation().Values.OfType<ISimple2DVisualizationPoint>()));

            if (getSender() != null && getSender() is IVisioExportableWithShape exportableSender)
                getShape().CellsU["BeginX"].GlueToPos(exportableSender.getShape(), 1, 0.5);
            if (getReceiver() != null && getReceiver() is IVisioExportableWithShape exportableReceiver)
                getShape().CellsU["EndY"].GlueToPos(exportableReceiver.getShape(), 0, 0.5);

            // Export the MessageSpecification (The message on the connector) to visio
            if (getMessageType() != null && getMessageType() is IVisioExportable exportable)
                exportable.exportToVisio(currentPage);
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioMessageExchange();
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
