using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioMessageExchange : MessageExchange, IVisioExportableWithShape
    {
        private const string type = ALPSConstants.alpsSIDMasterStandardMessageConnector;
        private readonly IShapeExport export;

        public VisioMessageExchange(IModelLayer layer) : base(layer)
        {
            export = new MessageExchangeExport(this);
        }

        protected VisioMessageExchange()
        {
            export = new MessageExchangeExport(this);
        }

        public void exportToVisio(Visio.Page currentPage)
        {
            if (getShape() != null) return;

            export.export(VisioHelper.ShapeType.SID, currentPage, type, new List<ISimple2DVisualizationPoint>(getElementsWithUnspecifiedRelation().Values.OfType<ISimple2DVisualizationPoint>()), this);

            if (getMessageType() is IVisioExportable exportable)
            {
                exportable.exportToVisio(currentPage);
                //messagebox.getShape().ContainerProperties.InsertListMember(exportable, 0); // TODO: messagebox
            }
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

        public bool prep2DInfo()
        {
            return false;
        }
    }
}