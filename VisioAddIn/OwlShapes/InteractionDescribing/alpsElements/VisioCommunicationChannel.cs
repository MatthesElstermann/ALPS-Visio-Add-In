using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using alps.net.api.ALPS;
using alps.net.api.parsing;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisioAddIn.OwlShapes
{
    public class VisioCommunicationChannel : CommunicationChannel, IVisioExportableWithShape
    {
        private const string type = ALPSConstants.alpsSIDMasterAbstractCommunicationChannel;
        private readonly IExportFunctionality export;

        protected VisioCommunicationChannel() { export = new PASSProcessModelElementExport(this); }

        public VisioCommunicationChannel(IModelLayer layer) : base(layer) { export = new PASSProcessModelElementExport(this); }

        public void exportToVisio(Visio.Page currentPage, ISimple2DVisualizationBounds bounds = null)
        {
            // Place the shape onto the SID page
            export.export(VisioHelper.ShapeType.SID, currentPage, type,
                                new List<ISimple2DVisualizationPoint>(getElementsWithUnspecifiedRelation().Values.OfType<ISimple2DVisualizationPoint>()));

            // TODO on updated alps.net.api 8.2.6

            // Set the type to BiDirectional
            //getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeBiDirectionalChannel].FormulaU = isUniDirectional() ? "FALSE" : "TRUE";

            //if (getCorrespondentA() != null && getCorrespondentA() is IVisioExportableWithShape exportableSender)
            //    getShape().CellsU["BeginX"].GlueToPos(exportableSender.getShape(), 1, 0.5);
            //if (getCorrespondentB() != null && getCorrespondentB() is IVisioExportableWithShape exportableReceiver)
            //    getShape().CellsU["EndY"].GlueToPos(exportableReceiver.getShape(), 0, 0.5);
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioCommunicationChannel();
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
