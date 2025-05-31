using System.Collections.Generic;
using alps.net.api.ALPS;
using alps.net.api.StandardPASS;
using static ALPS_Visio_AddIn_rewrite.VisioHelper;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
	public class SubjectExport : PASSProcessModelElementExport
	{
		readonly ISubject subject;

		public SubjectExport(ISubject subject) : base(subject)
		{
			this.subject = subject;
		}

		public override void export(ShapeType shapeType, Visio.Page page, string masterType, IList<ISimple2DVisualizationPoint> points = null, IPASSProcessModelElement originalElement = null)
		{
			base.export(shapeType, page, masterType, points, originalElement);

			shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeMaximumNumberOfInstantiation].Formula = "\"" + subject.getInstanceRestriction().ToString() + "\"";
		}
	}
}
