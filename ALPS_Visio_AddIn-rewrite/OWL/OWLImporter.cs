using System.Collections.Generic;
using System.Diagnostics;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using VisioAddIn.OwlShapes; // TODO
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite
{
    public class OWLImporter
    {
        private IPASSReaderWriter parser = PASSReaderWriter.getInstance();
        private string fileName;

        public OWLImporter(string fileName)
        {
            // setup parser
            parser.setModelElementFactory(new VisioClassFactory());
            parser.loadOWLParsingStructure(new List<string>
            {
                "../../Resources/standard_PASS_ont_v_1.1.0.owl",
                "../../Resources/ALPS_ont_v_0.8.0.owl"
            });

            this.fileName = fileName;
        }

        public void parse(Visio.Page mainPage, Visio.Document activeDoc)
        {
            // load models
            IList<IPASSProcessModel> passProcessModels = parser.loadModels(new List<string> { fileName });

            // necessary so the Visio VBA Listerners do not delete message on transitions before the complete model has been imported
            VisioHelper.setVBAListenersRunning(false);
            if (passProcessModels.Count > 0 && passProcessModels[0] is IVisioExportable exportable)
            {
                exportable.exportToVisio(mainPage);
            }
            VisioHelper.setVBAListenersRunning(true);
        }
    }
}