using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes.elements
{
    public class VisioModelLayer : ModelLayer, IVisioExportable
    {

        public VisioModelLayer(IPASSProcessModel model, string labelForID = null, string comment = null, string additionalLabel = null,
            IList<IIncompleteTriple> additionalAttribute = null)
            : base(model, labelForID, comment, additionalLabel, additionalAttribute) { setContainedBy(model); }

        protected VisioModelLayer() { }

        public void exportToVisio(Visio.Page currentPage, ISimple2DVisualizationBounds bounds = null)
        {
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioModelLayer();
        }
    }
}