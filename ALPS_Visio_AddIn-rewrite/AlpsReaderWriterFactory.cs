using System.IO;
using alps.net.api.parsing;

namespace ALPS_Visio_AddIn_rewrite
{
    /// <summary>
    /// Liefert den <see cref="PASSReaderWriter"/>-Singleton und umgeht dabei einen Bug in
    /// alps.net.api 0.9.1.6 (und bis einschl. Tag v0.9.1.7): Der Konstruktor baut seinen Serilog-
    /// Logpfad aus dem Arbeitsverzeichnis via
    /// <code>Directory.GetCurrentDirectory().Substring(0, path.IndexOf("bin"))</code>.
    /// Enthaelt das Arbeitsverzeichnis kein "bin" -- im Visio-Host der Normalfall, insbesondere
    /// nachdem ein OpenFileDialog das Arbeitsverzeichnis auf den ausgewaehlten Dateiordner gesetzt
    /// hat -- liefert <c>IndexOf("bin")</c> den Wert -1 und <c>Substring(0, -1)</c> wirft eine
    /// <see cref="System.ArgumentOutOfRangeException"/> ("length"). Der Fehler entsteht im
    /// Typeninitialisierer des Aufrufers (OWLImporter/Verifier) und legt den kompletten OWL-Import
    /// lautlos lahm.
    ///
    /// Da der Konstruktor nur EINMAL laeuft (Singleton), genuegt es, unmittelbar vor dem ersten
    /// <c>getInstance()</c> das Arbeitsverzeichnis kurz auf einen Ordner zu setzen, dessen Pfad
    /// "bin" enthaelt, und es danach wiederherzustellen.
    /// </summary>
    internal static class AlpsReaderWriterFactory
    {
        internal static PASSReaderWriter GetInstanceSafely()
        {
            string previousCwd = null;
            try { previousCwd = Directory.GetCurrentDirectory(); }
            catch { /* Arbeitsverzeichnis nicht lesbar -- dann eben ohne Wiederherstellung */ }

            try
            {
                // Ordner "bin" unter dem Temp-Verzeichnis: garantiert, dass IndexOf("bin") >= 0 ist,
                // sodass der Substring im Library-Konstruktor nicht mehr wirft.
                string safeDir = Path.Combine(Path.GetTempPath(), "bin");
                Directory.CreateDirectory(safeDir);
                Directory.SetCurrentDirectory(safeDir);
            }
            catch { /* schlaegt das fehl, versuchen wir getInstance() trotzdem */ }

            try
            {
                return PASSReaderWriter.getInstance();
            }
            finally
            {
                if (previousCwd != null)
                {
                    try { Directory.SetCurrentDirectory(previousCwd); }
                    catch { /* Wiederherstellung best effort */ }
                }
            }
        }
    }
}
