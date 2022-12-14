using alps.net.api.StandardPASS;
using System.Collections.Generic;
using Visio = Microsoft.Office.Interop.Visio;
using static VisioAddIn.VisioHelper;
using alps.net.api.ALPS;

namespace VisioAddIn.OwlShapes
{
    public class SubjectExport : PASSProcessModelElementExport
    {
        readonly ISubject subject;

        public SubjectExport(ISubject subject) : base(subject)
        {
            this.subject = subject;
        }

        public override void export(ShapeType shapeType, Visio.Page page, string masterType, IList<ISimple2DVisualizationPoint> points = null)
        {
            base.export(shapeType, page, masterType, points);

            shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeMaximumNumberOfInstantiation].Formula = "\"" + subject.getInstanceRestriction().ToString() + "\"";
        }
    }
}
