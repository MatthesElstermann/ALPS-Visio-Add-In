
using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using System.Linq;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisioAddIn.OwlShapes
{
    public class VisioMessageExchangeList : MessageExchangeList, IVisioExportableWithShape
    {
        Visio.Shape messageBoxShape;
        Visio.Shape connector;

        protected VisioMessageExchangeList() { }

        public VisioMessageExchangeList(IModelLayer layer) : base(layer) { }

        public void exportToVisio(Visio.Page currentPage, ISimple2DVisualizationBounds bounds = null)
        {
            if (messageBoxShape != null) return;
            Visio.Document sidShapes = VisioHelper.openStencil(VisioHelper.VisioStencils.SID_STENCIL); ;
            Visio.Master sidMaster = sidShapes.Masters.get_ItemU(ALPSConstants.alpsSIDMasterStandardMessageConnector);
            connector = currentPage.Drop(sidMaster, 4.25, 5.5);


            // Connect connector to the subjects
            if (messageExchanges.Values.Count > 0)
            {
                IMessageExchange firstExchange = getMessageExchanges().Values.First();
                if (firstExchange.getSender() is IVisioExportableWithShape exportableSender)
                    connector.CellsU["BeginX"].GlueToPos(exportableSender.getShape(), 1, 0.5);

                if (firstExchange.getReceiver() is IVisioExportableWithShape exportableReceiver)
                    connector.CellsU["EndY"].GlueToPos(exportableReceiver.getShape(), 0, 0.5);
            }

            // Get corresponding message box
            foreach (Visio.Shape shapeOnPage in currentPage.Shapes)
            {
                if (shapeOnPage.CellExistsU["User.idOnPage", 0] != 0)
                {
                    int result = (int)shapeOnPage.CellsU["User.idOnPage"].Result[""];
                    if (result == (int)connector.CellsU["User.idOfCorrespondingShape"].Result[""])
                    {
                        messageBoxShape = shapeOnPage;
                        break;
                    }
                }
            }

            // Get automatically created message shape and delete it
            System.Array listMembers = messageBoxShape.ContainerProperties.GetListMembers();
            if (listMembers.Length == 1)
            {
                int id = (int)listMembers.GetValue(0);
                foreach (Visio.Shape wrongShape in currentPage.Shapes)
                {
                    int shapeID = wrongShape.ID;
                    if (shapeID == id)
                    {
                        wrongShape.Delete();
                        break;
                    }
                }
            }

            // Trigger "Center Message Box on Connector" Action
            if (connector.CellExistsU["Actions.Row_1.Action", 0] != 0)
            {
                connector.CellsU["Actions.Row_1.Action"].Trigger();
            }



            messageBoxShape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentID].Formula = "\"" + getModelComponentID() + "\"";
            messageBoxShape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeLabel].Formula = "\"" + getModelComponentLabelsAsStrings()[0] + "\"";
            System.Array gluedShapes = messageBoxShape.GluedShapes(Visio.VisGluedShapesFlags.visGluedShapesAll1D, "");
            foreach (IMessageExchange exchange in getMessageExchanges().Values)
            {
                if (exchange.getMessageType() is IVisioExportable exportable)
                {
                    exportable.exportToVisio(currentPage);
                    if (exportable is IVisioExportableWithShape exportableWShape)
                    {
                        Visio.Shape exportedShape = exportableWShape.getShape();
                        messageBoxShape.ContainerProperties.InsertListMember(exportedShape, 0);
                        if (exchange is IVisioExportableWithShape exportableWShapeExchange) exportableWShapeExchange.setShape(connector);
                    }
                }

            }

        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioMessageExchangeList();
        }

        public Visio.Shape getShape()
        {
            return messageBoxShape;
        }

        public void setShape(Visio.Shape shape)
        {
            this.messageBoxShape = shape;
        }
    }
}
