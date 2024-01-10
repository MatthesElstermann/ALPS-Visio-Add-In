using alps.net.api.StandardPASS;
using System.Collections.Generic;
using Visio = Microsoft.Office.Interop.Visio;
using static VisioAddIn.VisioHelper;
using alps.net.api.ALPS;
using System.Diagnostics;

namespace VisioAddIn.OwlShapes
{
    public class SubjectExport : PASSProcessModelElementExport
    {
        readonly ISubject subject;

        public SubjectExport(ISubject subject) : base(subject)
        {
            //Debug.Print("Constructor start: SubjectExport()");
            this.subject = subject;
            //Debug.WriteLine("---Constructor done: SubjectExport()");
        }

        public override void export(ShapeType shapeType, Visio.Page page, string masterType, IList<ISimple2DVisualizationPoint> points = null, IPASSProcessModelElement originalElement = null)
        {
            base.export(shapeType, page, masterType, points, originalElement );
            

            shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeMaximumNumberOfInstantiation].Formula = "\"" + subject.getInstanceRestriction().ToString() + "\"";
        }
    }
}
