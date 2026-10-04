using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioCommunicationChannel : CommunicationChannel, IVisioImportableWithShape
    {
        private const string shapeType = Constants.SIDMasters.AbstractCommunicationChannel;

        private readonly IShapeImport import;
        public VisioCommunicationChannel(IModelLayer layer) : base(layer) { import = new PASSProcessModelElementImport(this); }
        protected VisioCommunicationChannel() { import = new PASSProcessModelElementImport(this); }

        public void ImportToVisio(Visio.Page page)
        {
            import.Import(shapeType, page, VH.GetBounds(this));

            // Mit den beiden Subjekten verbinden (frueher TODO — der Verbinder lag unverbunden
            // bei (0,0) neben der Seite). TODO: Richtungs-Flag (isUniDirectional) ins Shape.
            VH.GlueConnectorToSubjects(GetShape(), getCorrespondentA(), getCorrespondentB(), getModelComponentID());
        }

        public bool PrepareDimensions()
        {
            return false; // routing follow points
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioCommunicationChannel();
        }

        public Visio.Shape GetShape()
        {
            return import.GetShape();
        }
    }
}
