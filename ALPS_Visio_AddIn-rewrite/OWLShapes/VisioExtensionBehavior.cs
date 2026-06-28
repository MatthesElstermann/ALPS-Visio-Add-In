using System.Collections.Generic;
using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    /// <summary>
    /// Visio-importable extension behaviour: the behaviour an <see cref="ISubjectExtension"/> adds on
    /// top of a base subject. Drawn like any other subject behaviour (states + transitions) via
    /// <see cref="BehaviorImporter"/>; the page it is drawn onto becomes the extension's GBD.
    /// </summary>
    public class VisioExtensionBehavior : ExtensionBehavior, IVisioImportable
    {
        public VisioExtensionBehavior(IModelLayer layer, string labelForId = null, ISubject subject = null, ISet<IBehaviorDescribingComponent> behaviorDescribingComponents = null, IState initialStateOfBehavior = null, int priorityNumber = 0, string comment = null, string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null) : base(layer, labelForId, subject, behaviorDescribingComponents, initialStateOfBehavior, priorityNumber, comment, additionalLabel, additionalAttribute) { }
        protected VisioExtensionBehavior() { }

        public void ImportToVisio(Visio.Page page)
        {
            BehaviorImporter.Draw(this.getBehaviorDescribingComponents(), page);
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioExtensionBehavior();
        }
    }
}
