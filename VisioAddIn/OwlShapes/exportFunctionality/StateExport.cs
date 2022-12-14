using alps.net.api.ALPS;
using alps.net.api.StandardPASS;
using System.Collections.Generic;
using static VisioAddIn.VisioHelper;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisioAddIn.OwlShapes
{
    public class StateExport : PASSProcessModelElementExport
    {
        readonly IState state;

        public StateExport(IState state) : base(state)
        {
            this.state = state;
        }

        public override void export(ShapeType shapeType, Visio.Page page, string masterType, IList<ISimple2DVisualizationPoint> points = null)
        {
            base.export(shapeType, page, masterType, points);
            if (state.isStateType(IState.StateType.Abstract))
                shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeSBDStateIsAbstract].Formula = "TRUE";

            if (state.isStateType(IState.StateType.Finalized))
                shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeSBDStateIsFinalized].Formula = "TRUE";

            if (state.isStateType(IState.StateType.EndState))
                shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeSBDStateIsEndState].Formula = "TRUE";

            if (state.isStateType(IState.StateType.InitialStateOfBehavior))
                shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeSBDStateIsStartState].Formula = "TRUE";
        }
    }
}
