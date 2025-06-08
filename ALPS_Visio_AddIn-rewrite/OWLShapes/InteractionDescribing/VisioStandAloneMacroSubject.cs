using System.Collections.Generic;
using System.Linq;
using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioStandAloneMacroSubject : StandaloneMacroSubject, IVisioExportableWithShape
    {
        private const string type = ALPSConstants.alpsSIDMasterStandAloneMacro;
        private readonly IShapeExport export;

        public VisioStandAloneMacroSubject(IModelLayer layer, string labelForID = null, ISet<IMessageExchange> incomingMessageExchange = null, IMacroBehavior subjectMacroBehavior = null, ISet<IMessageExchange> outgoingMessageExchange = null, int maxSubjectInstanceRestriction = 1, ISubjectDataDefinition subjectDataDefinition = null, ISet<IInputPoolConstraint> inputPoolConstraints = null, string comment = null, string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null) : base(layer, labelForID, incomingMessageExchange, subjectMacroBehavior, outgoingMessageExchange, maxSubjectInstanceRestriction, comment, additionalLabel, additionalAttribute)
        {
            export = new SubjectExport(this);
        }

        protected VisioStandAloneMacroSubject()
        {
            export = new SubjectExport(this);
        }

        public void exportToVisio(Visio.Page currentPage)
        {
            export.export(VisioHelper.ShapeType.SID, currentPage, type, new List<ISimple2DVisualizationPoint>(getElementsWithUnspecifiedRelation().Values.OfType<ISimple2DVisualizationPoint>()), this);

            Visio.Page currentSBDPage = VisioHelper.CreateSBDPage(currentPage, ("MBD: " + getModelComponentID()), ("" + getModelComponentID()), this.getShape());

            if (getBehavior() is IVisioExportable exportable) exportable.exportToVisio(currentSBDPage);
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioStandAloneMacroSubject();
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
            if (this is IHasSimple2DVisualizationBox bounds)
            {
                Simple2DVisualizationPoint point = new Simple2DVisualizationPoint();
                point.setRelative2DPosX(bounds.getRelative2DPosX());
                point.setRelative2DPosY(bounds.getRelative2DPosY());
                this.addElementWithUnspecifiedRelation(point);
                Simple2DVisualizationPoint bound = new Simple2DVisualizationPoint();
                bound.setRelative2DPosX(bounds.getRelative2DWidth());
                bound.setRelative2DPosY(bounds.getRelative2DHeight());
                this.addElementWithUnspecifiedRelation(bound);
                return true;
            }
            else return false;
        }
    }
}