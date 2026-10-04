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

            // Mit den beiden Subjekten verbinden (frueher auskommentiert — der Verbinder lag
            // unverbunden bei (0,0) neben der Seite).
            VH.GlueConnectorToSubjects(GetShape(), getCorrespondentA(), getCorrespondentB(), getModelComponentID());
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