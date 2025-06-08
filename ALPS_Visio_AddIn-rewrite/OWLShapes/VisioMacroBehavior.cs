using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Configuration;
using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioMacroBehavor : MacroBehavior, IVisioExportableWithShape
    {
        public VisioMacroBehavor(IModelLayer layer, string labelForID = null, ISubject subject = null, ISet<IBehaviorDescribingComponent> behaviorDescribingComponents = null, ISet<IStateReference> stateReferences = null, IState initialStateOfBehavior = null, int priorityNumber = 0, string comment = null, string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null) : base(layer, labelForID, subject, behaviorDescribingComponents, stateReferences, initialStateOfBehavior, priorityNumber, comment, additionalLabel, additionalAttribute)
        {
        }

        protected VisioMacroBehavor()
        {
        }

        public void exportToVisio(Visio.Page currentPage)
        {
            foreach (IBehaviorDescribingComponent component in behaviorDescriptionComponents.Values)
            {
                if (!(component is IVisioExportable exportable)) continue;

                if (exportable is IVisioExportableWithShape shapeExportable) shapeExportable.prep2DInfo();

                if (exportable is IState || exportable is ITransition) exportable.exportToVisio(currentPage);
            }
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioMacroBehavor();
        }

        public Visio.Shape getShape()
        {
            throw new NotImplementedException();
        }

        public void setShape(Visio.Shape shape)
        {
            throw new NotImplementedException();
        }

        public bool prep2DInfo()
        {
            return false;
        }
    }
}