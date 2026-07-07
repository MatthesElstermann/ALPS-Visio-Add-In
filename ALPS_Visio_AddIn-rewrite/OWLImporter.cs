using alps.net.api;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using ALPS_Visio_AddIn_rewrite.OWLShapes;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;
using Visio = Microsoft.Office.Interop.Visio;

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

        private readonly IPASSReaderWriter parser;

        private OWLImporter()
        {
            // PASSReaderWriter ueber die Factory holen: umgeht einen CWD-abhaengigen Bug im
            // Konstruktor von alps.net.api 0.9.1.6, der den OWL-Import im Visio-Host lahmlegte
            // (siehe AlpsReaderWriterFactory).
            parser = AlpsReaderWriterFactory.GetInstanceSafely();

            // enable reflection and set ModelElementFactory to assign parsed objects to Visio classes
            ReflectiveEnumerator.addAssemblyToCheckForTypes(Assembly.GetExecutingAssembly());
            parser.setModelElementFactory(new VisioClassFactory());

            // Load the ontology (parsing structure) from the embedded resources, written to
            // temp files. The previous relative paths ("../../Resources/...") only resolved
            // when the current working directory was the build output folder -- when the
            // add-in is hosted in Visio the CWD differs, so the ontology was not found and the
            // import silently produced nothing. (Imports are resolved by ontology IRI from the
            // file content, so the file location does not matter.)
            parser.loadOWLParsingStructure(new List<string>
            {
                WriteOntologyToTempFile("standard_PASS_ont_v_1.1.0.owl", Properties.Resources.standard_PASS_ont_v_1_1_0),
                WriteOntologyToTempFile("ALPS_ont_v_0.8.0.owl", Properties.Resources.ALPS_ont_v_0_8_0)
            });
        }

        /// <summary>
        /// Writes an embedded ontology resource to a temp file and returns its path, so the
        /// parser can load it by path independent of the current working directory.
        /// </summary>
        private static string WriteOntologyToTempFile(string fileName, byte[] content)
        {
            string path = Path.Combine(Path.GetTempPath(), fileName);
            File.WriteAllBytes(path, content);
            return path;
        }

        /// <summary>
        /// Parse and import OWL file.
        /// </summary>
        public void Parse(string fileName)
        {
            // Re-establish the Visio class substitution before every import. Other features (e.g. the
            // ALPS Verification) share this parser singleton and swap in the plain factory, which would
            // otherwise leave imports drawing nothing.
            parser.setModelElementFactory(new VisioClassFactory());

            IList<IPASSProcessModel> passProcessModels = parser.loadModels(new List<string> { fileName });

            // FEAT: import all models -- currently only the first model is imported.
            // Make a missing model visible instead of silently doing nothing.
            if (passProcessModels.Count == 0 || !(passProcessModels[0] is IVisioImportable importable))
            {
                System.Windows.Forms.MessageBox.Show(
                    "Keine importierbaren PASS-/ALPS-Modelle in der Datei gefunden:\n" + fileName +
                    "\n\nHinweis: Ontologie-Dateien (Schema) enthalten keine Modelle und können " +
                    "nicht importiert werden.",
                    "OWL-Import");
                return;
            }

            // Disable the stencil's VBA listeners BEFORE opening the stencil, so the flag cell
            // already exists (= 0) when the stencil's VBA initializes. Otherwise the stencil
            // runs its "Willkommen"-routine, which on close renames the freshly created SID
            // page back to the Visio default ("Zeichenblatt-2").
            VH.setVBAListenersRunning(false);

            // open stencils to reduce load time
            VH.openStencil(VH.VisioStencils.SID_STENCIL);

            // Waehrend des Zeichnens nur das Bildschirm-Rendering aussetzen. Bewusst NICHT
            // EventsEnabled/DeferRecalc: Die ALPS-Stencils sind SmartShapes — der Drop des
            // Message-Connectors erzeugt z. B. die Message-Box erst ueber seine
            // EventDrop-Logik, und der Import liest direkt danach Formel-ERGEBNISSE
            // zurueck (User.idOnPage-Matching). Ohne Events fehlt die Box
            // (messageBox == null), mit aufgeschobenem Recalc waeren die Reads stale.
            Visio.Application app = Globals.ThisAddIn.Application;
            short prevScreenUpdating = app.ScreenUpdating;
            app.ScreenUpdating = 0;
            try
            {
                importable.ImportToVisio(null); // FEAT: import into current page
            }
            finally
            {
                app.ScreenUpdating = prevScreenUpdating;
            }

            // VBA listeners are intentionally NOT re-enabled here. The stencil's run-mode
            // welcome routine renames the imported SID page when its popup is closed (which
            // happens AFTER this method returns); re-enabling the flag would let that routine
            // run. Keeping it at 0 keeps the stencil VBA quiet -- the rewrite manages page and
            // model state itself.
            // VH.setVBAListenersRunning(true);
        }
    }
}