using System.Collections.Generic;
using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioMacroBehavor : MacroBehavior, IVisioExportable
    {
        public VisioMacroBehavor(IModelLayer layer, string labelForID = null, ISubject subject = null, ISet<IBehaviorDescribingComponent> behaviorDescribingComponents = null, ISet<IStateReference> stateReferences = null, IState initialStateOfBehavior = null, int priorityNumber = 0, string comment = null, string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null) : base(layer, labelForID, subject, behaviorDescribingComponents, stateReferences, initialStateOfBehavior, priorityNumber, comment, additionalLabel, additionalAttribute) { }

        protected VisioMacroBehavor() { }

        public void ExportToVisio(Visio.Page currentPage)
        {
            // TODO: set page dimensions

            foreach (IBehaviorDescribingComponent component in this.getBehaviorDescribingComponents().Values)
            {
                if (!(component is IVisioExportable exportable)) continue;

                if (exportable is IVisioExportableWithShape shapeExportable) shapeExportable.PrepareDimensions();

                if (exportable is IState || exportable is ITransition) exportable.ExportToVisio(currentPage);
            }
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioMacroBehavor();
        }
    }
}