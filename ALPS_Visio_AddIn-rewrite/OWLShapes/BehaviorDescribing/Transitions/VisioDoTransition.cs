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
    public class VisioDoTransition : DoTransition, IVisioExportableWithShape
    {
        private const string type = ALPSConstants.alpsSBDMasterStandardTransition;
        private readonly IShapeExport export;

        public VisioDoTransition(IState sourceState, IState targetState, string labelForID = null, ITransitionCondition transitionCondition = null, ITransition.TransitionType transitionType = ITransition.TransitionType.Standard, int priorityNumber = 0, string comment = null, string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null) : base(sourceState, targetState, labelForID, transitionCondition, transitionType, priorityNumber, comment, additionalLabel, additionalAttribute)
        {
            export = new TransitionExport(this);
        }

        protected VisioDoTransition()
        {
            export = new TransitionExport(this);
        }

        public void exportToVisio(Page currentPage)
        {
            export.export(VisioHelper.ShapeType.SBD, currentPage, type, new List<ISimple2DVisualizationPoint>(getElementsWithUnspecifiedRelation().Values.OfType<ISimple2DVisualizationPoint>()), this);

            // model component type
            int indexNumber = 0;
            switch (getTransitionType())
            {
                case TransitionType.Standard: indexNumber = 0; break;
                case TransitionType.Trigger: indexNumber = 1; break;
                case TransitionType.Precedence: indexNumber = 2; break;
                case TransitionType.Finalized: indexNumber = 3; break;
                case TransitionType.Advice: indexNumber = 4; break;
            }
            getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentType].FormulaU = "=INDEX(" + indexNumber + ",Prop.modelComponentType.Format)";
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioDoTransition();
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