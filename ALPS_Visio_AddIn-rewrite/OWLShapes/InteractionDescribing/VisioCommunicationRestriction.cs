using alps.net.api.ALPS;
using alps.net.api.parsing;
using Visio = Microsoft.Office.Interop.Visio;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
	public class VisioCommunicationRestriction : CommunicationRestriction, IVisioImportableWithShape
	{
		private const string shapeType = Constants.SIDMasters.CommunicationRestriction;

		private readonly IShapeImport import;
		public VisioCommunicationRestriction(IModelLayer layer) : base(layer) { import = new PASSProcessModelElementImport(this); }
		protected VisioCommunicationRestriction() { import = new PASSProcessModelElementImport(this); }

        public void ImportToVisio(Visio.Page page)
        {
            import.Import(shapeType, page, VH.GetBounds(this));

            // TODO

            //if (getCorrespondentA() != null && getCorrespondentA() is IVisioImportableWithShape importableSender) GetShape().CellsU["BeginX"].GlueToPos(importableSender.GetShape(), 1, 0.5);
            //if (getCorrespondentB() != null && getCorrespondentB() is IVisioImportableWithShape importableReceiver) GetShape().CellsU["EndY"].GlueToPos(importableReceiver.GetShape(), 0, 0.5);
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
            return import.GetShape();
        }
    }
}