using System.Collections.Generic;
using System.Linq;
using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioSubjectBehavior : SubjectBehavior, IVisioExportable
    {
        public VisioSubjectBehavior(IModelLayer layer, string labelForID = null, ISubject subject = null, ISet<IBehaviorDescribingComponent> behaviorDescribingComponents = null, IState initialStateOfBehavior = null, int priorityNumber = 0, string comment = null, string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null) : base(layer, labelForID, subject, behaviorDescribingComponents, initialStateOfBehavior, priorityNumber, comment, additionalLabel, additionalAttribute) { }
        protected VisioSubjectBehavior() { }

        public void ExportToVisio(Visio.Page currentPage)
        {
            // TODO: set page dimensions

            // TODO: hasInitialState

            // TODO: ExtensionBehavior
            // TODO: GuardBehavior
            // set background page
            // set extension
            //currentPage.BackPageFromName = "SBD";
            //currentPage.setSeparationStyle


            foreach (IBehaviorDescribingComponent component in this.getBehaviorDescribingComponents().Values.OrderBy(c => c is ITransition))
            {
                if (!(component is IVisioExportable exportable)) continue;

                if (exportable is IVisioExportableWithShape shapeExportable) shapeExportable.PrepareDimensions(); // TODO: if not: auto arrange

                if (exportable is IState || exportable is ITransition) exportable.ExportToVisio(currentPage);
            }
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioSubjectBehavior();
        }
    }
}