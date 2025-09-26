using System.Collections.Generic;
using System.Globalization;
using alps.net.api.ALPS;
using alps.net.api.StandardPASS;
using static ALPS_Visio_AddIn_rewrite.VisioHelper;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class MessageExchangeExport : PASSProcessModelElementExport
    {
        readonly IMessageExchange messageExchange;

        public MessageExchangeExport(IMessageExchange messageExchange) : base(messageExchange)
        {
            this.messageExchange = messageExchange;
        }

        public override void export(ShapeType shapeType, Visio.Page page, string masterType, IList<ISimple2DVisualizationPoint> points = null, IPASSProcessModelElement originalElement = null)
        {
            base.export(shapeType, page, masterType, points, originalElement);

            // set dimensions
            if (messageExchange.getSender() is IVisioExportableWithShape exportableSender)
                shape.CellsU["BeginX"].GlueToPos(exportableSender.getShape(), 1, 0.5);

            if (messageExchange.getReceiver() is IVisioExportableWithShape exportableReceiver)
                shape.CellsU["EndY"].GlueToPos(exportableReceiver.getShape(), 0, 0.5);

            // TEMP: center between begin and end
            shape.CellsU["User.globalX"].FormulaU = "\"" + (shape.CellsU["BeginX"].Result[""] + shape.CellsU["EndX"].Result[""])/2.0 + "\"";
            shape.CellsU["User.globalY"].FormulaU = "\"" + (shape.CellsU["BeginY"].Result[""] + shape.CellsU["EndY"].Result[""]) / 2.0 + "\"";

            // center message box on connector
            shape.CellsU["Actions.Row_1.Action"].Trigger();
        }
    }
}
