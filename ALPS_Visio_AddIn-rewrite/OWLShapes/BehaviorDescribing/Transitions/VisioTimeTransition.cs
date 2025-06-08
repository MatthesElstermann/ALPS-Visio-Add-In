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
    public class VisioTimeTransition : TimeTransition, IVisioExportableWithShape
    {
        private const string type = ALPSConstants.alpsSBDMasterTimeTransition;
        private readonly IShapeExport export;

        public VisioTimeTransition(IState sourceState, IState targetState, string labelForID = null, ITimeTransitionCondition transitionCondition = null, ITransition.TransitionType transitionType = ITransition.TransitionType.Standard, ITimeTransition.TimeTransitionType timeTransitionType = ITimeTransition.TimeTransitionType.DayTimeTimer, string comment = null, string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null) : base(sourceState, targetState, labelForID, transitionCondition, transitionType, timeTransitionType, comment, additionalLabel, additionalAttribute)
        {
            export = new TransitionExport(this);
        }

        protected VisioTimeTransition()
        {
            export = new TransitionExport(this);
        }

        public void exportToVisio(Page currentPage)
        {
            export.export(VisioHelper.ShapeType.SBD, currentPage, type, new List<ISimple2DVisualizationPoint>(getElementsWithUnspecifiedRelation().Values.OfType<ISimple2DVisualizationPoint>()), this);

            ITimeTransitionCondition condition = getTransitionCondition();

            // transition type
            getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeTimeOutType].FormulaU = "INDEX(" + (int)condition.getTimeTransitionType() + ", Prop." + ALPSConstants.alpsPropertieTypeTimeOutType + ".Format)";

            bool isReminder = condition.getTimeTransitionType() == ITimeTransitionCondition.TimeTransitionConditionType.TimeBasedReminder || condition.getTimeTransitionType() == ITimeTransitionCondition.TimeTransitionConditionType.CalendarBasedReminder;

            // timeout
            getShape().CellsU["Prop." + (isReminder ? ALPSConstants.alpsPropertieTypeTimeOutDate : ALPSConstants.alpsPropertieTypeTimeOutTime)].Formula = "\"" + condition.getTimeValue() + "\"";
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioTimeTransition();
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