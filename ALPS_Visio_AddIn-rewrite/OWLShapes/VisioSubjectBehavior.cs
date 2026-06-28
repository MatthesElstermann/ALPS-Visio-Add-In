using System.Collections.Generic;
using System.Linq;
using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioSubjectBehavior : SubjectBehavior, IVisioImportable
    {
        public VisioSubjectBehavior(IModelLayer layer, string labelForID = null, ISubject subject = null, ISet<IBehaviorDescribingComponent> behaviorDescribingComponents = null, IState initialStateOfBehavior = null, int priorityNumber = 0, string comment = null, string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null) : base(layer, labelForID, subject, behaviorDescribingComponents, initialStateOfBehavior, priorityNumber, comment, additionalLabel, additionalAttribute) { }
        protected VisioSubjectBehavior() { }

        public void ImportToVisio(Visio.Page currentPage)
        {
            // TODO: set page dimensions
            // TODO: hasInitialState
            BehaviorImporter.Draw(this.getBehaviorDescribingComponents(), currentPage);
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioSubjectBehavior();
        }
    }
}