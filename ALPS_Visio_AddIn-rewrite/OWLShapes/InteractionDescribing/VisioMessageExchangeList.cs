using System.Collections.Generic;
using System.Linq;
using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioMessageExchangeList : MessageExchangeList, IVisioExportableWithShape
    {
        private const string type = ALPSConstants.alpsSIDMasterStandardMessageConnector;
        private readonly IShapeExport export;
        private Visio.Shape messageBoxShape;

        public VisioMessageExchangeList(IModelLayer layer) : base(layer)
        {
            export = new MessageExchangeListExport(this);
        }

        protected VisioMessageExchangeList()
        {
            export = new MessageExchangeListExport(this);
        }

        public void exportToVisio(Visio.Page currentPage)
        {
            if (getShape() != null) return;

            export.export(VisioHelper.ShapeType.SID, currentPage, type, new List<ISimple2DVisualizationPoint>(getElementsWithUnspecifiedRelation().Values.OfType<ISimple2DVisualizationPoint>()), this);

            //if (getMessageExchanges().Values.First() is IVisioExportable firstExportable) firstExportable.exportToVisio(currentPage);

            //foreach (Visio.Shape pageShape in currentPage.Shapes)
            //{
            //    if (pageShape.CellExistsU["User.idOnPage", 0] != 0)
            //    {
            //        if (pageShape.CellsU["User.idOnPage"].Result[""] == firstExportable.getShape().CellsU["User.idOfCorrespondingShape"].Result[""])
            //        {
            //            messageBoxShape = pageShape;
            //            break;
            //        }
            //    }
            //}

            // message specifications
            foreach (IMessageExchange messageExchange in getMessageExchanges().Values)
            {
                if (messageExchange.getMessageType() is IVisioExportable exportable) exportable.exportToVisio(currentPage);
            }
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioMessageExchangeList();
        }

        public Visio.Shape getShape()
        {
            return export.getShape();
        }

        public void setShape(Visio.Shape shape)
        {
            export.setShape(shape);
        }

        public bool prep2DInfo()
        {
            return false;
        }
    }
}