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
        internal static string WriteOntologyToTempFile(string fileName, byte[] content)
        {
            // Eigener Unterordner pro Prozess: laufen zwei Visio-Instanzen, sperrte die eine die
            // gemeinsame Temp-Datei der anderen — die IOException im statischen Konstruktor wurde
            // zur TypeInitializationException und legte den Import bis zum Visio-Neustart lahm.
            string dir = Path.Combine(Path.GetTempPath(), "ALPS_Visio_AddIn",
                System.Diagnostics.Process.GetCurrentProcess().Id.ToString());
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, fileName);
            try
            {
                File.WriteAllBytes(path, content);
            }
            catch (IOException) when (File.Exists(path))
            {
                // Bereits vorhanden und gerade gesperrt — der Inhalt stammt aus derselben Ressource.
            }
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
                UI.ResultDialog.ShowWarning(
                    "Kein importierbares Modell gefunden",
                    "Die Datei enthält kein PASS-/ALPS-Modell zum Importieren.",
                    "Datei:\n" + fileName +
                    "\n\nHinweis: Reine Ontologie-Dateien (Schema) enthalten keine Modelle und " +
                    "können nicht importiert werden.");
                return;
            }

            // Disable the stencil's VBA listeners BEFORE opening the stencil, so the flag cell
            // already exists (= 0) when the stencil's VBA initializes. Otherwise the stencil
            // runs its "Willkommen"-routine, which on close renames the freshly created SID
            // page back to the Visio default ("Zeichenblatt-2").
            // Vor allen Zeichenschritten zuruecksetzen — auch CopyPatternMasters meldet hierher.
            ImportDiagnostics.Reset();

            // Ohne aktive Zeichnung (keine oder nur eine Schablone offen) eine neue anlegen.
            Visio.Document drawing = VH.EnsureActiveDrawing();
            VH.setVBAListenersRunning(false);
            try
            {
                ImportInto(drawing, importable);
            }
            finally
            {
                // Auch bei Abbruch/Fehler: die VBA erst nach Abschluss der zugestellten
                // Import-Ereignisse wieder einschalten (siehe ReenableVBAListenersWhenIdle) —
                // sonst blieb sie dauerhaft aus, u. a. das Umschalten des Shape-Sets
                // (SID-/SBD-Schablone je nach Seite). Nicht synchron hier: die Willkommens-
                // Routine der Schablone wuerde sonst die importierte SID-Seite umbenennen.
                VH.ReenableVBAListenersWhenIdle(drawing);
            }
        }

        private void ImportInto(Visio.Document drawing, IVisioImportable importable)
        {
            // Beide Schablonen vorab oeffnen. Fehlt eine, hat openStencil das bereits gemeldet —
            // dann abbrechen, statt pro Element erneut einen Fehlerdialog zu zeigen und am Ende
            // leere Seiten als erfolgreichen Import zu hinterlassen.
            if (VH.openStencil(VH.VisioStencils.SID_STENCIL) == null || VH.openStencil(VH.VisioStencils.SBD_STENCIL) == null)
                return;

            // Die Verbinder-Master (Transitions, MessageConnector) zeichnen ihre
            // Pfeilspitzen ueber benutzerdefinierte Linienmuster; Visio uebertraegt
            // diese Muster beim programmatischen Drop nicht ins Zieldokument. Vor dem
            // Zeichnen kopieren, sonst erscheinen alle Verbinder ohne Pfeilspitzen.
            VH.CopyPatternMasters(drawing);

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
            catch (System.Exception ex)
            {
                // Catch-all-Diagnose: die volle Exception (inkl. Stacktrace) ins
                // Ausgabefenster schreiben, bevor sie an den Ribbon-Handler weitergereicht
                // wird — so ist die exakte Absturzstelle auch dann sichtbar, wenn sie tief
                // in der API liegt (der Dialog zeigt nur die Meldung).
                System.Diagnostics.Debug.WriteLine("### OWL-Import abgebrochen: " + ex);
                throw;
            }
            finally
            {
                app.ScreenUpdating = prevScreenUpdating;
            }

            // Seitenverwaltung einmal komplett neu aufbauen: erst jetzt sind alle Ebenen-Beziehungen
            // verdrahtet, sodass SBD/GBD-Seiten ihren Hintergrund bekommen und State References
            // auf ihre Basis-States einrasten — ohne dass der Nutzer erst etwas verschieben muss.
            try { Globals.ThisAddIn.updateClicked(); }
            catch (System.Exception e) { System.Diagnostics.Debug.WriteLine("[Import] controller refresh failed: " + e); }

            // Einzelne Elemente werden beim Zeichnen abgefangen, damit der Rest importiert wird —
            // die Ausfaelle hier einmal gesammelt melden statt sie nur ins Debug-Log zu schreiben.
            if (ImportDiagnostics.Failures.Count > 0)
            {
                UI.ResultDialog.ShowWarning("Import unvollständig",
                    ImportDiagnostics.Failures.Count + " Element(e) konnten nicht gezeichnet werden.",
                    string.Join("\n", ImportDiagnostics.Failures));
            }
        }
    }
}