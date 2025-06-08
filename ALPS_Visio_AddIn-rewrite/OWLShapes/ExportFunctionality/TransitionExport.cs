using System.Collections.Generic;
using System.Globalization;
using alps.net.api.ALPS;
using alps.net.api.StandardPASS;
using static ALPS_Visio_AddIn_rewrite.VisioHelper;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class TransitionExport : PASSProcessModelElementExport
    {
        readonly ITransition transition;

        public TransitionExport(ITransition transition) : base(transition)
        {
            this.transition = transition;
        }

        public override void export(ShapeType shapeType, Visio.Page page, string masterType, IList<ISimple2DVisualizationPoint> points = null, IPASSProcessModelElement originalElement = null)
        {
            base.export(shapeType, page, masterType, points, originalElement);

            // set properties
            shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeBoxCanBeMovedFreely].Formula = "\"FALSE\"";

            return; // TODO

            if (transition.getSourceState() is IVisioExportableWithShape exportableSender)
                shape.CellsU["BeginX"].GlueToPos(exportableSender.getShape(), 1, 0.5);

            if (transition.getTargetState() is IVisioExportableWithShape exportableReceiver)
                shape.CellsU["EndY"].GlueToPos(exportableReceiver.getShape(), 0, 0.5);

            string allImplements = string.Join(";", transition.getImplementedInterfaces().Keys);
            if (transition.getImplementedInterfaces().Count > 0)
                shape.CellsU["Prop." + ALPSConstants.alpsPropertyTypeImplements].Formula = "\"" + allImplements + "\"";
        }
    }
}
