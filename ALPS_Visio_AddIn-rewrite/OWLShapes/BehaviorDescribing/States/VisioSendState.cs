using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioSendState : SendState, IVisioImportableWithShape
    {
        private const string shapeType = Constants.SBDMasters.SendState;
        
        private readonly IShapeImport import;
        public VisioSendState(ISubjectBehavior behavior) : base(behavior) { import = new StateImport(this); }
        protected VisioSendState() { import = new StateImport(this); }

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
            return new VisioSendState();
        }

        public Visio.Shape GetShape()
        {
            return import.GetShape();
        }
    }
}