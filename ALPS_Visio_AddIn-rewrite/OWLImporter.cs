using alps.net.api;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using ALPS_Visio_AddIn_rewrite.OWLShapes;
using System.Collections.Generic;
using System.Reflection;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;

namespace ALPS_Visio_AddIn_rewrite
{
    /// <summary>
    /// Importer for OWL ALPS files
    /// </summary>
    public class OWLImporter
    {
        /// <summary>
        /// Singleton OWL importer instance
        /// </summary>
        public static readonly OWLImporter Instance = new OWLImporter();

        private readonly IPASSReaderWriter parser = PASSReaderWriter.getInstance();

        private OWLImporter()
        {
            // enable reflection and set ModelElementFactory to assign parsed objects to Visio classes
            ReflectiveEnumerator.addAssemblyToCheckForTypes(Assembly.GetExecutingAssembly());
            parser.setModelElementFactory(new VisioClassFactory());

            parser.loadOWLParsingStructure(new List<string>
            {
                "../../Resources/standard_PASS_ont_v_1.1.0.owl",
                "../../Resources/ALPS_ont_v_0.8.0.owl"
            });
        }

        /// <summary>
        /// Parse and import OWL file.
        /// </summary>
        public void Parse(string fileName)
        {
            IList<IPASSProcessModel> passProcessModels = parser.loadModels(new List<string> { fileName });

            // open stencils to reduce load time
            VH.openStencil(VH.VisioStencils.SID_STENCIL);

            // disable VBA listeners to prevent interference
            VH.setVBAListenersRunning(false);

            if (passProcessModels.Count > 0 && passProcessModels[0] is IVisioExportable exportable) // FEAT: import all models
            {
                exportable.ExportToVisio(null); // FEAT: import into current page
            }

            VH.setVBAListenersRunning(true);
        }
    }
}