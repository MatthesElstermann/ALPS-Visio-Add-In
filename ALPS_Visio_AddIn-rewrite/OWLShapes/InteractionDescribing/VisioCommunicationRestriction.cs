using alps.net.api.ALPS;
using alps.net.api.parsing;
using Visio = Microsoft.Office.Interop.Visio;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
	public class VisioCommunicationRestriction : CommunicationRestriction, IVisioExportableWithShape
	{
		private const string shapeType = Constants.SIDMasters.CommunicationRestriction;

		private readonly IShapeExport export;
		public VisioCommunicationRestriction(IModelLayer layer) : base(layer) { export = new PASSProcessModelElementExport(this); }
		protected VisioCommunicationRestriction() { export = new PASSProcessModelElementExport(this); }

        public void ExportToVisio(Visio.Page page)
        {
            export.Export(shapeType, page, VH.GetBounds(this));

            // TODO

            //if (getCorrespondentA() != null && getCorrespondentA() is IVisioExportableWithShape exportableSender) GetShape().CellsU["BeginX"].GlueToPos(exportableSender.GetShape(), 1, 0.5);
            //if (getCorrespondentB() != null && getCorrespondentB() is IVisioExportableWithShape exportableReceiver) GetShape().CellsU["EndY"].GlueToPos(exportableReceiver.GetShape(), 0, 0.5);
        }

        public bool PrepareDimensions() // TODO: prepare dimensions
        {
            return false;
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
		{
			return new VisioCommunicationRestriction();
        }

        public Visio.Shape GetShape()
        {
            return export.GetShape();
        }
    }
}