using System;
using System.IO;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite
{
    /// <summary>
    /// Exportiert das aktuell geoeffnete ALPS/PASS-Modell als OWL-Datei, indem der
    /// bewaehrte VBA-Exporter des SID-Stencils aufgerufen wird
    /// (<c>ALPS_RDFOWLExporter.createProcessRDFOWL</c> via <c>Document.ExecuteLine</c>).
    /// Das Makro exportiert immer das AKTIVE Dokument und schreibt die Datei ohne
    /// Rueckgabewert neben die Visio-Datei (Dokumentname, Leerzeichen durch '_'
    /// ersetzt, Endung ".vsdx" entfernt) -- dieser Pfad wird hier exakt nachgebildet,
    /// um die erzeugte Datei anschliessend an BPMN-Konverter/Verification
    /// weiterreichen zu koennen.
    /// </summary>
    public static class VbaOwlExporter
    {
        private const string SidStencilNamePrefix = "Abstract PASS SID Visio Shapes";
        private const string ExportMacro = "ALPS_RDFOWLExporter.createProcessRDFOWL";

        /// <summary>
        /// Prueft ohne Seiteneffekte, ob das aktive Dokument per VBA-Makro exportierbar
        /// ist: Zeichnung aktiv, SID-Stencil (Traeger der Makros) geoeffnet und
        /// mindestens eine Seite traegt die Model-URI (sonst laeuft die Seitensuche
        /// des Makros ins Leere).
        /// </summary>
        public static bool CanExportActiveDocument(Visio.Application app)
        {
            try
            {
                Visio.Document doc = app?.ActiveDocument;
                if (doc == null || doc.Type != Visio.VisDocumentTypes.visTypeDrawing)
                    return false;
                if (FindSidStencil(app) == null)
                    return false;

                foreach (Visio.Page page in doc.Pages)
                {
                    if (page.PageSheet.CellExistsU["Prop." + Constants.Properties.PageModelURI, 0] != 0)
                        return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Ruft den VBA-Export fuer das aktive Dokument auf und liefert den Pfad der
        /// erzeugten OWL-Datei. Wirft mit verstaendlicher Meldung, wenn die
        /// Voraussetzungen fehlen oder die Datei nicht entstanden ist.
        /// </summary>
        public static string ExportActiveModel(Visio.Application app)
        {
            Visio.Document drawing = app?.ActiveDocument;
            if (drawing == null || drawing.Type != Visio.VisDocumentTypes.visTypeDrawing)
                throw new InvalidOperationException("Es ist kein Zeichnungsdokument aktiv.");

            string directory = drawing.Path;
            if (string.IsNullOrEmpty(directory))
                throw new InvalidOperationException(
                    "Das Dokument muss zuerst gespeichert werden — der OWL-Export legt die Datei neben der Visio-Datei ab.");
            if (directory.IndexOf("http", StringComparison.OrdinalIgnoreCase) >= 0)
                throw new InvalidOperationException(
                    "Das Dokument liegt auf OneDrive/SharePoint (" + directory + ") — bitte eine lokal gespeicherte Kopie verwenden.");

            Visio.Document stencil = FindSidStencil(app);
            if (stencil == null)
                throw new InvalidOperationException(
                    "Das SID-Stencil („" + SidStencilNamePrefix + "…“) ist nicht geöffnet — es enthält das Export-Makro.");

            // Zielpfad exakt wie im VBA-Makro nachbilden (siehe createProcessRDFOWL):
            // modelName = Replace(Replace(doc.Name, " ", "_"), ".vsdx", "") & ".owl"
            string modelName = drawing.Name.Replace(" ", "_").Replace(".vsdx", "");
            string expectedPath = Path.Combine(directory, modelName + ".owl");

            // Alte Datei entfernen, damit File.Exists unten wirklich den NEUEN Export
            // nachweist (das Makro meldet Fehler nur per Debug.Print/MsgBox).
            try
            {
                if (File.Exists(expectedPath))
                    File.Delete(expectedPath);
            }
            catch
            {
                // Nicht loeschbar -- das Makro ueberschreibt ohnehin; der Nachweis
                // unten ist dann schwaecher, aber der Export funktioniert trotzdem.
            }

            stencil.ExecuteLine("Call " + ExportMacro);

            if (!File.Exists(expectedPath))
                throw new InvalidOperationException(
                    "Der VBA-Export hat die erwartete Datei nicht erzeugt:\n" + expectedPath +
                    "\nBitte prüfen, ob das aktive Dokument ein ALPS/PASS-Modell mit Model-URI ist.");

            return expectedPath;
        }

        private static Visio.Document FindSidStencil(Visio.Application app)
        {
            foreach (Visio.Document doc in app.Documents)
            {
                if (doc.Name != null && doc.Name.StartsWith(SidStencilNamePrefix, StringComparison.OrdinalIgnoreCase))
                    return doc;
            }
            return null;
        }
    }
}
