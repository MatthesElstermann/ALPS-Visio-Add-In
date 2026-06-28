using System.Collections.Generic;
using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    /// <summary>
    /// Visio-importable guard behaviour: the behaviour of a guard (the content of a Guard Behaviour
    /// Diagram). Drawn like any other subject behaviour (states + transitions) via
    /// <see cref="BehaviorImporter"/>; the page it is drawn onto becomes the GBD.
    /// </summary>
    public class VisioGuardBehavior : GuardBehavior, IVisioImportable
    {
        public VisioGuardBehavior(IModelLayer layer, string labelForID = null, ISubject subject = null, ISet<IBehaviorDescribingComponent> behaviorDescribingComponents = null, IState initialStateOfBehavior = null, int priorityNumber = 0, string comment = null, string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null) : base(layer, labelForID, subject, behaviorDescribingComponents, initialStateOfBehavior, priorityNumber, comment, additionalLabel, additionalAttribute) { }
        protected VisioGuardBehavior() { }

        public void ImportToVisio(Visio.Page page)
        {
            BehaviorImporter.Draw(this.getBehaviorDescribingComponents(), page);
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioGuardBehavior();
        }
    }
}
