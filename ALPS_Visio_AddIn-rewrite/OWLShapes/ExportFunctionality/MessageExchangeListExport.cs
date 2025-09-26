using System.Collections.Generic;
using System.Globalization;
using alps.net.api.ALPS;
using alps.net.api.StandardPASS;
using static ALPS_Visio_AddIn_rewrite.VisioHelper;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class MessageExchangeListExport : PASSProcessModelElementExport
    {
        readonly IMessageExchangeList messageExchangeList;

        public MessageExchangeListExport(IMessageExchangeList messageSpecification) : base(messageSpecification)
        {
            this.messageExchangeList = messageSpecification;
        }

        public override void export(ShapeType shapeType, Visio.Page page, string masterType, IList<ISimple2DVisualizationPoint> points = null, IPASSProcessModelElement originalElement = null)
        {
            base.export(shapeType, page, masterType, points, originalElement);

            // TODO
        }
    }
}
