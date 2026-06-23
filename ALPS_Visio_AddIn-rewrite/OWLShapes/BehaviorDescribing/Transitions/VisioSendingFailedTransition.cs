using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;
using System.Collections.Generic;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioSendingFailedTransition : SendingFailedTransition, IVisioImportableWithShape
    {
        private const string shapeType = Constants.SBDMasters.SendingFailedTransition;
        
        private readonly IShapeImport import;
        public VisioSendingFailedTransition(IState sourceState, IState targetState, string labelForID = null, ITransitionCondition transitionCondition = null, ITransition.TransitionType transitionType = ITransition.TransitionType.Standard, string comment = null, string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null) : base(sourceState, targetState, labelForID, transitionCondition, transitionType, comment, additionalLabel, additionalAttribute) { import = new TransitionImport(this); }
        protected VisioSendingFailedTransition() { import = new TransitionImport(this); }

        public void ImportToVisio(Visio.Page page)
        {
            import.Import(shapeType, page, VH.GetBounds(this));
        }

        public bool PrepareDimensions() // TODO: prepare dimensions
        {
            return false;
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioSendingFailedTransition();
        }

        public Visio.Shape GetShape()
        {
            return import.GetShape();
        }
    }
}