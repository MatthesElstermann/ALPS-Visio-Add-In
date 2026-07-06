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
            // Gemeinsamer CWD-Workaround fuer den PASSReaderWriter-Ctor-Bug in alps.net.api 0.9.1.6
            // (siehe AlpsReaderWriterFactory). Der Singleton wird nur einmal erzeugt -- egal, ob
            // Import oder Verification ihn zuerst anfordert.
            PASSReaderWriter parser = AlpsReaderWriterFactory.GetInstanceSafely();

            // Load with the plain alps.net.api classes, NOT our VisioClassFactory: the checks compare
            // against type names like "alps.net.api.StandardPASS.FullySpecifiedSubject", which would
            // never match the substituted Visio* classes. (OWLImporter re-sets its factory per import.)
            parser.setModelElementFactory(new BasicPASSProcessModelElementFactory());

            parser.loadOWLParsingStructure(new List<string>
            {
                ExtractOntology("standard_PASS_ont_v_1.1.0.owl", Properties.Resources.standard_PASS_ont_v_1_1_0),
                ExtractOntology("ALPS_ont_v_0.8.0.owl", Properties.Resources.ALPS_ont_v_0_8_0)
            });

            IList<IPASSProcessModel> models = parser.loadModels(new List<string> { specPath, implPath });

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
                    GetCorrespondingElementsALL getAll = new GetCorrespondingElementsALL();
                    var subjects = getAll.GetSubjects(models);
                    getAll.GetMessages(models);
                    var transitions = getAll.GetMessageTransitions(models);
                    getAll.GetMessageRestriction(models);
                    getAll.GetStates(models);
                    getAll.GetTransitions(models);

                    // Implemented SID checks.
                    CheckSID checkSID = new CheckSID();
                    restrictionsValid = checkSID.CheckCommunicationRestrictions(specifyingRestrictions, implementingMessages) == 1;
                    subjectsValid = checkSID.CheckSubject(subjects);
                    connectorsValid = checkSID.CheckMessageconnectors(transitions);
                    checksRan = true;

                    // SBD checks are not implemented yet in the prototype.
                    new CheckSBD();
                }
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
