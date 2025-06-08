using alps.net.api.ALPS;
using System.Collections.Generic;
using System.Linq;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using Microsoft.Office.Interop.Visio;
using alps.net.api.util;
using static alps.net.api.StandardPASS.ITransition;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioFlowRestrictor : FlowRestrictor, IVisioExportableWithShape
    {
        private const string type = ALPSConstants.alpsSBDMasterFlowRestrictor;
        private readonly IShapeExport export;

        public VisioFlowRestrictor(IState sourceState, IState targetState, string labelForID = null, ITransitionCondition transitionCondition = null, TransitionType transitionType = TransitionType.Standard, string comment = null, string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null) : base(sourceState, targetState, labelForID, transitionCondition, transitionType, comment, additionalLabel, additionalAttribute)
        {
            export = new TransitionExport(this);
        }

        protected VisioFlowRestrictor()
        {
            export = new TransitionExport(this);
        }

        public void exportToVisio(Page currentPage)
        {
            export.export(VisioHelper.ShapeType.SBD, currentPage, type, new List<ISimple2DVisualizationPoint>(getElementsWithUnspecifiedRelation().Values.OfType<ISimple2DVisualizationPoint>()), this);
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioFlowRestrictor();
        }

        public Shape getShape()
        {
            return export.getShape();
        }

        public void setShape(Shape shape)
        {
            export.setShape(shape);
        }

        public bool prep2DInfo()
        {
            return false;
        }
    }
}