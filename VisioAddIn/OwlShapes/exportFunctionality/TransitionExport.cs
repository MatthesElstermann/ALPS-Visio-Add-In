using alps.net.api.StandardPASS;
using System;
using System.Collections.Generic;
using Visio = Microsoft.Office.Interop.Visio;
using static VisioAddIn.VisioHelper;
using alps.net.api.ALPS;
using System.Linq;

namespace VisioAddIn.OwlShapes
{
    /// <summary>
    /// Contains all functionality to modify exported Transitions.
    /// </summary>
    public class TransitionExport : PASSProcessModelElementExport
    {
        readonly ITransition transition;
        public TransitionExport(ITransition transition) : base(transition)
        {
            this.transition = transition;
        }

        public override void export(ShapeType shapeType, Visio.Page page, string masterType, IList<ISimple2DVisualizationPoint> points = null, IPASSProcessModelElement originalElement = null)
        {
            base.export(shapeType, page, masterType, points);

            // Fix the transition tag box onto the transition connector
            shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeBoxCanBeMovedFreely].Formula = "\"FALSE\"";

            // Glue the connector to the sending state
            if (transition.getSourceState() is IVisioExportableWithShape exportableSender)
                shape.CellsU["BeginX"].GlueToPos(exportableSender.getShape(), 1, 0.5);

            // Glue the connector to the receiving state
            if (transition.getTargetState() is IVisioExportableWithShape exportableReceiver)
                shape.CellsU["EndY"].GlueToPos(exportableReceiver.getShape(), 0, 0.5);

            // Add the implemented interfaces
            string allImplements = string.Join(";", transition.getImplementedInterfaces().Keys);
            if (transition.getImplementedInterfaces().Count > 0)
                shape.CellsU["Prop." + ALPSConstants.alpsPropertyTypeImplements].Formula = "\"" + allImplements + "\"";
        }
    }
}
