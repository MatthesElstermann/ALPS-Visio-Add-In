using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;
using System.Collections.Generic;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioTimeTransition : TimeTransition, IVisioImportableWithShape
    {
        private const string shapeType = Constants.SBDMasters.TimeTransition;
        
        private readonly IShapeImport import;
        public VisioTimeTransition(IState sourceState, IState targetState, string labelForID = null, ITimeTransitionCondition transitionCondition = null, ITransition.TransitionType transitionType = ITransition.TransitionType.Standard, ITimeTransition.TimeTransitionType timeTransitionType = ITimeTransition.TimeTransitionType.DayTimeTimer, string comment = null, string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null) : base(sourceState, targetState, labelForID, transitionCondition, transitionType, timeTransitionType, comment, additionalLabel, additionalAttribute) { import = new TransitionImport(this); }
        protected VisioTimeTransition() { import = new TransitionImport(this); }

        public void ImportToVisio(Visio.Page page)
        {
            import.Import(shapeType, page, VH.GetBounds(this));

            ITimeTransitionCondition condition = this.getTransitionCondition();

            // transition type
            VH.SetPropFormula(import.GetShape(), Constants.Properties.Transition.TimeOutType,
                "INDEX(" + (int)condition.getTimeTransitionType() + ", Prop." + Constants.Properties.Transition.TimeOutType + ".Format)");

            // timeout
            bool isReminder = condition.getTimeTransitionType() == ITimeTransitionCondition.TimeTransitionConditionType.TimeBasedReminder || condition.getTimeTransitionType() == ITimeTransitionCondition.TimeTransitionConditionType.CalendarBasedReminder;
            if (isReminder) VH.SetProp(import.GetShape(), Constants.Properties.Transition.TimeOutDate, condition.getTimeValue());
            else VH.SetProp(import.GetShape(), Constants.Properties.Transition.TimeOutTime, condition.getTimeValue());
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
            return import.GetShape();
        }
    }
}