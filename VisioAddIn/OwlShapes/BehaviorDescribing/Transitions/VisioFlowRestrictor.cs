using alps.net.api.ALPS;
using alps.net.api.ALPS.ALPSModelElements.ALPSSBDComponents;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using System.Collections.Generic;
using System.Linq;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisioAddIn.OwlShapes
{
    public class VisioFlowRestrictor : FlowRestrictor, IVisioExportableWithShape
    {
        private const string type = ALPSConstants.alpsSBDMasterFlowRestrictor;
        private readonly IExportFunctionality export;

        public VisioFlowRestrictor() { export = new TransitionExport(this); }

        public VisioFlowRestrictor(IState sourceState, IState targetState, string labelForID = null, ITransitionCondition transitionCondition = null,
            ITransition.TransitionType transitionType = ITransition.TransitionType.Standard, string comment = null,
            string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null)
            : base(sourceState, targetState, labelForID, transitionCondition, transitionType, comment, additionalLabel, additionalAttribute)
        { export = new TransitionExport(this); }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioFlowRestrictor();
        }

        public void exportToVisio(Visio.Page currentPage, ISimple2DVisualizationBounds bounds = null)
        {
            export.export(VisioHelper.ShapeType.SBD, currentPage, type,
                                new List<ISimple2DVisualizationPoint>(getElementsWithUnspecifiedRelation().Values.OfType<ISimple2DVisualizationPoint>()));
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
