using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using System.Collections.Generic;
using System.Linq;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisioAddIn.OwlShapes
{
    public class VisioTimeTransition : TimeTransition, IVisioExportableWithShape
    {
        private const string type = ALPSConstants.alpsSBDMasterTimeTransition;
        private readonly IExportFunctionality export;

        public VisioTimeTransition() { export = new TransitionExport(this); }

        public VisioTimeTransition(IState sourceState, IState targetState, string labelForID = null, ITimeTransitionCondition transitionCondition = null,
            ITransition.TransitionType transitionType = ITransition.TransitionType.Standard,
            ITimeTransition.TimeTransitionType timeTransitionType = ITimeTransition.TimeTransitionType.DayTimeTimer,
            string comment = null,
            string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null)
            : base(sourceState, targetState, labelForID, transitionCondition, transitionType, timeTransitionType, comment, additionalLabel, additionalAttribute)
        { export = new TransitionExport(this); }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioTimeTransition();
        }

        public void exportToVisio(Visio.Page currentPage, ISimple2DVisualizationBounds bounds = null)
        {
            export.export(VisioHelper.ShapeType.SBD, currentPage, type,
                                new List<ISimple2DVisualizationPoint>(getElementsWithUnspecifiedRelation().Values.OfType<ISimple2DVisualizationPoint>()));

            ITimeTransitionCondition condition = getTransitionCondition();
            // Set the type
            getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeTimeOutType].FormulaU =
                "INDEX(" + (int)condition.getTimeTransitionType() + ", Prop." + ALPSConstants.alpsPropertieTypeTimeOutType + ".Format)";

            bool isReminder = condition.getTimeTransitionType() == ITimeTransitionCondition.TimeTransitionConditionType.TimeBasedReminder ||
                condition.getTimeTransitionType() == ITimeTransitionCondition.TimeTransitionConditionType.CalendarBasedReminder;

            // If it is a reminder and not a normal timer, use the date field
            // If it is a normal timer, use the time field
            getShape().CellsU["Prop." + (isReminder ? ALPSConstants.alpsPropertieTypeTimeOutDate : ALPSConstants.alpsPropertieTypeTimeOutTime)].Formula =
                    "\"" + condition.getTimeValue() + "\"";
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
