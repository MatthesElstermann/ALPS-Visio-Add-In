using System.Collections.Generic;

namespace ALPS_Visio_AddIn_rewrite
{
    /// <summary>
    /// Sammelt Elemente, deren Zeichnen beim OWL-Import fehlschlug. Die Import-Schritte fangen
    /// solche Fehler bewusst ab (ein Element soll nicht den ganzen Import abbrechen); damit der
    /// Nutzer davon erfährt, meldet <see cref="OWLImporter"/> die Liste am Ende einmal gesammelt.
    /// </summary>
    internal static class ImportDiagnostics
    {
        private static readonly List<string> failures = new List<string>();

        internal static IReadOnlyList<string> Failures => failures;

        internal static void Reset()
        {
            failures.Clear();
        }

        internal static void Report(string elementId, System.Exception error)
        {
            System.Diagnostics.Debug.WriteLine("Import des Elements \"" + elementId + "\" fehlgeschlagen: " + error);
            failures.Add("• " + elementId + ": " + error.Message);
        }
    }
}
