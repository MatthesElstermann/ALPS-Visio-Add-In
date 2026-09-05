using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioReceiveState : ReceiveState, IVisioImportableWithShape
    {
        private const string shapeType = Constants.SBDMasters.ReceiveState;

        private readonly IShapeImport import;
        public VisioReceiveState(ISubjectBehavior behavior) : base(behavior) { import = new StateImport(this); }
        protected VisioReceiveState() { import = new StateImport(this); }

        public void ImportToVisio(Visio.Page page)
        {
            import.Import(shapeType, page, VH.GetBounds(this));
        }

        public bool PrepareDimensions()
        {
            return VisualizationBounds.Prepare(this);
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioReceiveState();
        }

        public Visio.Shape GetShape()
        {
            return import.GetShape();
        }
    }
}