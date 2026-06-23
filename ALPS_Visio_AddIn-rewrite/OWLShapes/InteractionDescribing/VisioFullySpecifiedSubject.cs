using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;
using System.Collections.Generic;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioFullySpecifiedSubject : FullySpecifiedSubject, IVisioImportableWithShape
    {
        private readonly IShapeImport import;
        public VisioFullySpecifiedSubject(IModelLayer layer, string labelForID = null, ISet<IMessageExchange> incomingMessageExchange = null, ISubjectBaseBehavior subjectBaseBehavior = null, ISet<ISubjectBehavior> subjectBehaviors = null, ISet<IMessageExchange> outgoingMessageExchange = null, int maxSubjectInstanceRestriction = 1, ISubjectDataDefinition subjectDataDefinition = null, ISet<IInputPoolConstraint> inputPoolConstraints = null, string comment = null, string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null) : base(layer, labelForID, incomingMessageExchange, subjectBaseBehavior, subjectBehaviors, outgoingMessageExchange, maxSubjectInstanceRestriction, subjectDataDefinition, inputPoolConstraints, comment, additionalLabel, additionalAttribute) { this.import = new SubjectImport(this); }
        protected VisioFullySpecifiedSubject() { this.import = new SubjectImport(this); }

        public void ImportToVisio(Visio.Page page)
        {
            import.Import(Constants.SIDMasters.StandardActor, page, VH.GetBounds(this));
        }

        public bool PrepareDimensions()
        {
            if (this is IHasSimple2DVisualizationBox bounds && bounds.getRelative2DWidth() > 0)
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

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioFullySpecifiedSubject();
        }

        public Visio.Shape GetShape()
        {
            return import.GetShape();
        }
    }
}