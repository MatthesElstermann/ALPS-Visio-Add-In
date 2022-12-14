using alps.net.api.ALPS;
using alps.net.api.parsing;
using System.Collections.Generic;
using System.Linq;
using Visio = Microsoft.Office.Interop.Visio;
namespace VisioAddIn.OwlShapes
{
    public class VisioCommunicationRestriction : CommunicationRestriction, IVisioExportableWithShape
    {
        private const string type = ALPSConstants.alpsSIDMasterCommunicationRestriction;
        private readonly IExportFunctionality export;

        protected VisioCommunicationRestriction() { export = new PASSProcessModelElementExport(this); }

        public VisioCommunicationRestriction(IModelLayer layer) : base(layer) { export = new PASSProcessModelElementExport(this); }

        public void exportToVisio(Visio.Page currentPage, ISimple2DVisualizationBounds bounds = null)
        {
            if (getShape() != null) return;
            export.export(VisioHelper.ShapeType.SID, currentPage, type,
                                new List<ISimple2DVisualizationPoint>(getElementsWithUnspecifiedRelation().Values.OfType<ISimple2DVisualizationPoint>()));

            if (getCorrespondentA() != null && getCorrespondentA() is IVisioExportableWithShape exportableSender)
                getShape().CellsU["BeginX"].GlueToPos(exportableSender.getShape(), 1, 0.5);
            if (getCorrespondentB() != null && getCorrespondentB() is IVisioExportableWithShape exportableReceiver)
                getShape().CellsU["EndY"].GlueToPos(exportableReceiver.getShape(), 0, 0.5);


        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioCommunicationRestriction();
        }

        public Visio.Shape getShape()
        {
            return export.getShape();
        }

        public void setShape(Visio.Shape shape)
        {
            export.setShape(shape);
        }
        // 
    }
}
