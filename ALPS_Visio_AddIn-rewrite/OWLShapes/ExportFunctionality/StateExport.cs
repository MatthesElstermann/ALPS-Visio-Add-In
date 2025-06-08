using System.Collections.Generic;
using System.Globalization;
using alps.net.api.ALPS;
using alps.net.api.StandardPASS;
using static ALPS_Visio_AddIn_rewrite.VisioHelper;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class StateExport : PASSProcessModelElementExport
    {
        readonly IState state;

        public StateExport(IState state) : base(state)
        {
            this.state = state;
        }

        public override void export(ShapeType shapeType, Visio.Page page, string masterType, IList<ISimple2DVisualizationPoint> points = null, IPASSProcessModelElement originalElement = null)
        {
            base.export(shapeType, page, masterType, points, originalElement);

            // set properties
            if (state.isStateType(IState.StateType.Abstract))
                shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeSBDStateIsAbstract].FormulaForceU = "=TRUE";

            if (state.isStateType(IState.StateType.Finalized))
                shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeSBDStateIsFinalized].FormulaForceU = "=TRUE";

            if (state.isStateType(IState.StateType.EndState))
                shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeSBDStateIsEndState].FormulaForceU = "=TRUE";

            if (state.isStateType(IState.StateType.InitialStateOfBehavior))
                shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeSBDStateIsStartState].FormulaForceU = "=TRUE";

            // set dimensions
            double width = points[1].getRelative2DPosX() * page.PageSheet.CellsU["PageWidth"].Result[""];
            double height = points[1].getRelative2DPosY() * page.PageSheet.CellsU["PageHeight"].Result[""];
            shape.CellsU["Width"].FormulaU = width.ToString(CultureInfo.InvariantCulture);
            shape.CellsU["Height"].FormulaU = height.ToString(CultureInfo.InvariantCulture);

            // set position
            double posX = points[0].getRelative2DPosX() * page.PageSheet.CellsU["PageWidth"].Result[""];
            double posY = points[0].getRelative2DPosY() * page.PageSheet.CellsU["PageHeight"].Result[""];
            shape.CellsU["PinX"].FormulaU = posX.ToString(CultureInfo.InvariantCulture);
            shape.CellsU["PinY"].FormulaU = posY.ToString(CultureInfo.InvariantCulture);
        }
    }
}
