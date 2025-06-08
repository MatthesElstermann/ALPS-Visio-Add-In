using System.Collections.Generic;
using System.Linq;
using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
	public class VisioCommunicationRestriction : CommunicationRestriction, IVisioExportableWithShape
	{
		private const string type = ALPSConstants.alpsSIDMasterCommunicationRestriction;
		private readonly IShapeExport export;

		public VisioCommunicationRestriction(IModelLayer layer) : base(layer)
		{
			export = new PASSProcessModelElementExport(this);
		}

		protected VisioCommunicationRestriction()
		{
			export = new PASSProcessModelElementExport(this);
		}

		public void exportToVisio(Visio.Page currentPage)
		{
			export.export(VisioHelper.ShapeType.SID, currentPage, type, new List<ISimple2DVisualizationPoint>(getElementsWithUnspecifiedRelation().Values.OfType<ISimple2DVisualizationPoint>()), this);

            if (getCorrespondentA() != null && getCorrespondentA() is IVisioExportableWithShape exportableSender) getShape().CellsU["BeginX"].GlueToPos(exportableSender.getShape(), 1, 0.5);
            if (getCorrespondentB() != null && getCorrespondentB() is IVisioExportableWithShape exportableReceiver) getShape().CellsU["EndY"].GlueToPos(exportableReceiver.getShape(), 0, 0.5);
        }

		public override IParseablePASSProcessModelElement getParsedInstance()
		{
			return new VisioCommunicationRestriction();
		}

		public Visio.Shape getShape()
		{
			return export.getShape();
		}

		public void setShape(Visio.Shape shape)
		{
			export.setShape(shape);
		}

		public bool prep2DInfo()
		{
			return false;
		}
	}
}