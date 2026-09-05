using System.Collections.Generic;
using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioStandAloneMacroSubject : StandaloneMacroSubject, IVisioImportableWithShape
    {
        private const string shapeType = Constants.SIDMasters.StandAloneMacro;

        private readonly IShapeImport import;
        public VisioStandAloneMacroSubject(IModelLayer layer, string labelForID = null, ISet<IMessageExchange> incomingMessageExchange = null, IMacroBehavior subjectMacroBehavior = null, ISet<IMessageExchange> outgoingMessageExchange = null, int maxSubjectInstanceRestriction = 1, ISubjectDataDefinition subjectDataDefinition = null, ISet<IInputPoolConstraint> inputPoolConstraints = null, string comment = null, string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null) : base(layer, labelForID, incomingMessageExchange, subjectMacroBehavior, outgoingMessageExchange, maxSubjectInstanceRestriction, comment, additionalLabel, additionalAttribute) { import = new SubjectImport(this); }
        protected VisioStandAloneMacroSubject() { import = new SubjectImport(this); }

        public void ImportToVisio(Visio.Page page)
        {
            import.Import(shapeType, page, VH.GetBounds(this));
        }

        public bool PrepareDimensions()
        {
            return VisualizationBounds.Prepare(this);
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioStandAloneMacroSubject();
        }

        public Visio.Shape GetShape()
        {
            return import.GetShape();
        }
    }
}