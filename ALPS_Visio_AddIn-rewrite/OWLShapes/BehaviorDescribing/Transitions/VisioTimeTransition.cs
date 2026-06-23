using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;
using System.Collections.Generic;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioTimeTransition : TimeTransition, IVisioExportableWithShape
    {
        private const string shapeType = Constants.SBDMasters.TimeTransition;
        
        private readonly IShapeExport export;
        public VisioTimeTransition(IState sourceState, IState targetState, string labelForID = null, ITimeTransitionCondition transitionCondition = null, ITransition.TransitionType transitionType = ITransition.TransitionType.Standard, ITimeTransition.TimeTransitionType timeTransitionType = ITimeTransition.TimeTransitionType.DayTimeTimer, string comment = null, string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null) : base(sourceState, targetState, labelForID, transitionCondition, transitionType, timeTransitionType, comment, additionalLabel, additionalAttribute) { export = new TransitionExport(this); }
        protected VisioTimeTransition() { export = new TransitionExport(this); }

        public void ExportToVisio(Visio.Page page)
        {
            export.Export(shapeType, page, VH.GetBounds(this));

            ITimeTransitionCondition condition = this.getTransitionCondition();

            // transition type
            VH.SetPropFormula(export.GetShape(), Constants.Properties.Transition.TimeOutType,
                "INDEX(" + (int)condition.getTimeTransitionType() + ", Prop." + Constants.Properties.Transition.TimeOutType + ".Format)");

            // timeout
            bool isReminder = condition.getTimeTransitionType() == ITimeTransitionCondition.TimeTransitionConditionType.TimeBasedReminder || condition.getTimeTransitionType() == ITimeTransitionCondition.TimeTransitionConditionType.CalendarBasedReminder;
            if (isReminder) VH.SetProp(export.GetShape(), Constants.Properties.Transition.TimeOutDate, condition.getTimeValue());
            else VH.SetProp(export.GetShape(), Constants.Properties.Transition.TimeOutTime, condition.getTimeValue());
        }

        public bool PrepareDimensions() // TODO: prepare dimensions
        {
            return false;
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioTimeTransition();
        }

        public Visio.Shape GetShape()
        {
            return export.GetShape();
        }
    }
}