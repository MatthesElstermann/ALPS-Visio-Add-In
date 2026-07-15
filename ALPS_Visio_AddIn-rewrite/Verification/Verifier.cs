using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;

namespace ALPS_Visio_AddIn_rewrite.Verification
{
    /// <summary>
    /// ALPS Verification, ported 1:1 from andikra/ALPS-Verification-Thesis (a KIT master-thesis
    /// prototype). Loads a Specification (abstract) and an Implementation OWL model and runs the
    /// implemented SID checks. The raw console output of the checks is captured and returned so it
    /// can be shown in a window instead of a console.
    ///
    /// Note: this is a prototype — only a few SID checks are implemented and the output is raw text
    /// that a human has to interpret; there is no overall verdict yet.
    /// </summary>
    public static class Verifier
    {
        /// <summary>Runs the verification of <paramref name="implPath"/> against <paramref name="specPath"/>.</summary>
        public static string Verify(string specPath, string implPath)
        {
            IList<IPASSProcessModel> models = LoadModels(new List<string> { specPath, implPath });
            return VerifyCore(models);
        }

        /// <summary>
        /// Prueft ein bereits im Speicher gebautes Implementierungs-Modell (aus dem aktuell
        /// geoeffneten Visio-Dokument, siehe VisioPassModelBuilder) gegen eine
        /// Spezifikations-OWL-Datei.
        /// </summary>
        public static string Verify(string specPath, IPASSProcessModel implModel)
        {
            IList<IPASSProcessModel> models;
            try
            {
                models = LoadModels(new List<string> { specPath });
            }
            catch (Exception ex)
            {
                // Das Parsen der Spezifikations-Datei ist in der API fehlgeschlagen
                // (z. B. NullReferenceException in BasicPASSProcessModelElementFactory.
                // createInstance) — die volle Ursache samt Stacktrace zurueckgeben,
                // statt den Aufrufer abstuerzen zu lassen.
                return "Die Spezifikations-Datei konnte nicht geparst werden:\n" + specPath +
                    "\n\n" + ex;
            }
            if (models.Count < 1)
                return "Die Spezifikations-Datei konnte nicht als ALPS-Modell geladen werden: " + specPath;
            return VerifyCore(new List<IPASSProcessModel> { models[0], implModel });
        }

        /// <summary>Laedt OWL-Modelle mit den PLAIN alps.net.api-Klassen (nicht der VisioClassFactory).</summary>
        private static IList<IPASSProcessModel> LoadModels(List<string> paths)
        {
            // Gemeinsamer CWD-Workaround fuer den PASSReaderWriter-Ctor-Bug in alps.net.api 0.9.1.6
            // (siehe AlpsReaderWriterFactory). Der Singleton wird nur einmal erzeugt -- egal, ob
            // Import oder Verification ihn zuerst anfordert.
            PASSReaderWriter parser = AlpsReaderWriterFactory.GetInstanceSafely();

            // Load with the plain alps.net.api classes, NOT our VisioClassFactory: the checks compare
            // against type names like "alps.net.api.StandardPASS.FullySpecifiedSubject", which would
            // never match the substituted Visio* classes. (OWLImporter re-sets its factory per import.)
            // NullSafe-Variante: faengt den createInstance-NRE bei unbekannten Typen ab, damit eine
            // Spezifikation mit einem nicht auffloesbaren Typ nicht die ganze Verifikation abreisst.
            parser.setModelElementFactory(new NullSafeModelElementFactory());

            parser.loadOWLParsingStructure(new List<string>
            {
                ExtractOntology("standard_PASS_ont_v_1.1.0.owl", Properties.Resources.standard_PASS_ont_v_1_1_0),
                ExtractOntology("ALPS_ont_v_0.8.0.owl", Properties.Resources.ALPS_ont_v_0_8_0)
            });

            return parser.loadModels(paths);
        }

        /// <summary>
        /// Führt einen Einzel-Check aus und fängt Fehler ab: ein fehlschlagender
        /// (fragiler Prototyp-)Check bricht nicht die ganze Verifikation ab, sondern
        /// nennt Name + volle Exception (inkl. Stacktrace) im Report/Debug-Log.
        /// </summary>
        private static T RunCheck<T>(string name, Func<T> check)
        {
            try
            {
                return check();
            }
            catch (Exception ex)
            {
                Console.WriteLine("### Check „" + name + "“ fehlgeschlagen: " + ex);
                System.Diagnostics.Debug.WriteLine("Verification check '" + name + "' failed: " + ex);
                return default(T);
            }
        }

        private static string VerifyCore(IList<IPASSProcessModel> models)
        {

            var output = new StringWriter();
            TextWriter original = Console.Out;
            Console.SetOut(output);

            // Ergebnisse der Einzel-Checks fuer das Gesamtergebnis einsammeln (die Checks
            // lieferten die Werte schon immer zurueck, sie wurden nur nie ausgewertet).
            bool checksRan = false;
            bool restrictionsValid = false, subjectsValid = false, connectorsValid = false;

            try
            {
                if (models.Count < 2)
                {
                    Console.WriteLine("Es müssen zwei Modelle geladen werden (Spezifikation + Implementierung).");
                }
                else
                {
                    // models[0] = specification (abstract), models[1] = implementation.
                    IList<ICommunicationRestriction> specifyingRestrictions =
                        models[0].getAllElements().Values.OfType<ICommunicationRestriction>().ToList();
                    IList<IMessageExchange> implementingMessages =
                        models[1].getAllElements().Values.OfType<IMessageExchange>().ToList();

                    // Pair specification/implementation elements (the calls also print their findings).
                    // Jeder Einzel-Check wird gekapselt, damit ein Fehler die genaue Stelle
                    // im Report nennt (samt Stacktrace) statt alles abzubrechen.
                    GetCorrespondingElementsALL getAll = new GetCorrespondingElementsALL();
                    var subjects = RunCheck("GetSubjects", () => getAll.GetSubjects(models));
                    RunCheck("GetMessages", () => { getAll.GetMessages(models); return 0; });
                    var transitions = RunCheck("GetMessageTransitions", () => getAll.GetMessageTransitions(models));
                    RunCheck("GetMessageRestriction", () => { getAll.GetMessageRestriction(models); return 0; });
                    RunCheck("GetStates", () => { getAll.GetStates(models); return 0; });
                    RunCheck("GetTransitions", () => { getAll.GetTransitions(models); return 0; });

                    // Implemented SID checks.
                    CheckSID checkSID = new CheckSID();
                    restrictionsValid = RunCheck("CheckCommunicationRestrictions",
                        () => checkSID.CheckCommunicationRestrictions(specifyingRestrictions, implementingMessages)) == 1;
                    subjectsValid = RunCheck("CheckSubject", () => checkSID.CheckSubject(subjects ?? new List<Tuple<ISubject, ISubject>>()));
                    connectorsValid = RunCheck("CheckMessageconnectors",
                        () => checkSID.CheckMessageconnectors(transitions ?? new List<Tuple<ICommunicationAct, IImplementingElement<ICommunicationAct>>>()));
                    checksRan = true;

                    // SBD checks are not implemented yet in the prototype.
                    new CheckSBD();
                }
            }
            catch (Exception ex)
            {
                // Die portierten KIT-Prototyp-Checks sind fragil: sie erwarten zwei
                // GEPARSTE Modelle (Spezifikation + Implementierung), die ueber
                // "implements"-Verweise verknuepft sind. Ein direkt aus dem geoeffneten
                // Dokument gebautes Modell traegt solche Verweise (noch) nicht, wodurch
                // die Checks intern auf null laufen koennen. Statt hart abzustuerzen die
                // Ursache verstaendlich melden.
                Console.SetOut(original);
                return "Die Verifikation konnte nicht vollständig durchlaufen.\n\n" + ex +
                    "\n\nHinweis: Die Prüfung vergleicht ein Spezifikations- mit einem " +
                    "Implementierungsmodell über deren „implements“-Verweise. Enthält das " +
                    "Implementierungsmodell keine solchen Verweise, gibt es nichts zu paaren. Für " +
                    "eine vollständige Verifikation bitte ein Implementierungsmodell verwenden, das " +
                    "„implements“-Beziehungen zur Spezifikation trägt.\n\nBisherige Ausgabe:\n" + output;
            }
            finally
            {
                Console.SetOut(original);
            }

            string report = output.ToString();
            if (checksRan)
                report += BuildVerdict(restrictionsValid, subjectsValid, connectorsValid,
                    CountOccurrences(report, "Element not implemented!"));

            return string.IsNullOrWhiteSpace(report)
                ? "Keine Ausgabe. Prüfe, ob beide OWL-Dateien gültige ALPS-Modelle (Spezifikation + Implementierung) sind."
                : report;
        }

        /// <summary>
        /// Baut das Gesamtergebnis am Report-Ende. "Nicht implementierte Elemente" zaehlt die
        /// "Element not implemented!"-Zeilen der Paarungs-Laeufe (Subjekte, Messages,
        /// Message-Transitionen, Restriktionen, States, Transitionen) — jedes Element der
        /// Spezifikation, zu dem die Implementierung kein Gegenstueck referenziert.
        /// </summary>
        private static string BuildVerdict(bool restrictionsValid, bool subjectsValid, bool connectorsValid, int notImplemented)
        {
            bool passed = restrictionsValid && subjectsValid && connectorsValid && notImplemented == 0;

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("==========================================");
            sb.AppendLine("GESAMTERGEBNIS");
            sb.AppendLine("==========================================");
            sb.AppendLine("Kommunikations-Restriktionen eingehalten:     " + (restrictionsValid ? "ja" : "NEIN"));
            sb.AppendLine("Subjekt-Typen korrekt implementiert:          " + (subjectsValid ? "ja" : "NEIN"));
            sb.AppendLine("Message-Connector-Typen korrekt implementiert: " + (connectorsValid ? "ja" : "NEIN"));
            sb.AppendLine("Nicht implementierte Spezifikations-Elemente:  " + notImplemented);
            sb.AppendLine("------------------------------------------");
            sb.AppendLine(passed
                ? "VERDICT: BESTANDEN — die Implementierung erfuellt alle geprueften SID-Regeln."
                : "VERDICT: NICHT BESTANDEN — Details in den Abschnitten oben.");
            sb.AppendLine("(Hinweis: Der Pruefer ist ein Prototyp — SBD-Checks sind noch nicht implementiert,");
            sb.AppendLine(" das Verdict deckt nur die SID-Ebene ab.)");
            return sb.ToString();
        }

        /// <summary>Zaehlt nicht-ueberlappende Vorkommen von <paramref name="marker"/> in <paramref name="text"/>.</summary>
        private static int CountOccurrences(string text, string marker)
        {
            int count = 0, index = 0;
            while ((index = text.IndexOf(marker, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += marker.Length;
            }
            return count;
        }

        private static string ExtractOntology(string fileName, byte[] content)
        {
            string path = Path.Combine(Path.GetTempPath(), fileName);
            File.WriteAllBytes(path, content);
            return path;
        }
    }
}
