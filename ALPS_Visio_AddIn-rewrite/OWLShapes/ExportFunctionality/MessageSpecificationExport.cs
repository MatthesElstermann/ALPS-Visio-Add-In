using System.Collections.Generic;
using System.Globalization;
using alps.net.api.ALPS;
using alps.net.api.StandardPASS;
using static ALPS_Visio_AddIn_rewrite.VisioHelper;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class MessageSpecificationExport : PASSProcessModelElementExport
    {
        readonly IMessageSpecification messageSpecification;

        public MessageSpecificationExport(IMessageSpecification messageSpecification) : base(messageSpecification)
        {
            this.messageSpecification = messageSpecification;
        }

        public override void export(ShapeType shapeType, Visio.Page page, string masterType, IList<ISimple2DVisualizationPoint> points = null, IPASSProcessModelElement originalElement = null)
        {
            base.export(shapeType, page, masterType, points, originalElement);

            // TODO
        }
    }
}
