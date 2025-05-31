using System.Collections.Generic;
using System.Linq;
using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioFullySpecifiedSubject : FullySpecifiedSubject, IVisioExportableWithShape
    {
        private const string type = ALPSConstants.alpsSIDMasterStandardActor;
        private readonly IShapeExport export;

        public VisioFullySpecifiedSubject(IModelLayer layer, string labelForID = null, ISet<IMessageExchange> incomingMessageExchange = null, ISubjectBaseBehavior subjectBaseBehavior = null, ISet<ISubjectBehavior> subjectBehaviors = null, ISet<IMessageExchange> outgoingMessageExchange = null, int maxSubjectInstanceRestriction = 1, ISubjectDataDefinition subjectDataDefinition = null, ISet<IInputPoolConstraint> inputPoolConstraints = null, string comment = null, string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null) : base(layer, labelForID, incomingMessageExchange, subjectBaseBehavior, subjectBehaviors, outgoingMessageExchange, maxSubjectInstanceRestriction, subjectDataDefinition, inputPoolConstraints, comment, additionalLabel, additionalAttribute) 
        {
            export = new SubjectExport(this);
        }

        protected VisioFullySpecifiedSubject()
        {
            export = new SubjectExport(this);
        }

        public void exportToVisio(Visio.Page currentPage)
        {
            export.export(VisioHelper.ShapeType.SID, currentPage, type, new List<ISimple2DVisualizationPoint>(getElementsWithUnspecifiedRelation().Values.OfType<ISimple2DVisualizationPoint>()), this);

            Visio.Page currentSBDPage = VisioHelper.CreateSBDPage(currentPage, ("SBD: " + getModelComponentID()), ("" + getModelComponentID()), this.getShape());

            if (getSubjectBaseBehavior() is IVisioExportable exportable) exportable.exportToVisio(currentSBDPage);
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioFullySpecifiedSubject();
        }

        public Visio.Shape getShape()
        {
            return export.getShape();
        }

        public void setShape(Visio.Shape shape)
        {
            export.setShape(shape);
        }
    }
}