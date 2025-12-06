using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;
using System.Collections.Generic;
using alps.net.api.ALPS;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioFlowRestrictor : FlowRestrictor, IVisioExportableWithShape
    {
        private const string shapeType = Constants.SBDMasters.FlowRestrictor;

        private readonly IShapeExport export;
        public VisioFlowRestrictor(IState sourceState, IState targetState, string labelForID = null, ITransitionCondition transitionCondition = null, ITransition.TransitionType transitionType = ITransition.TransitionType.Standard, string comment = null, string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null) : base(sourceState, targetState, labelForID, transitionCondition, transitionType, comment, additionalLabel, additionalAttribute) { export = new TransitionExport(this); }
        protected VisioFlowRestrictor() { export = new TransitionExport(this); }

        public void ExportToVisio(Visio.Page page)
        {
            export.Export(shapeType, page, VH.GetBounds(this));
        }

        public bool PrepareDimensions() // TODO: prepare dimensions
        {
            return false;
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioFlowRestrictor();
        }

        public Visio.Shape GetShape()
        {
            return export.GetShape();
        }
    }
}