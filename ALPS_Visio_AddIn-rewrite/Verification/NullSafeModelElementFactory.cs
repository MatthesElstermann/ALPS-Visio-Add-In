using System;
using System.Collections.Generic;
using System.Linq;
using alps.net.api;          // ITreeNode
using alps.net.api.parsing;
using alps.net.api.StandardPASS;

namespace ALPS_Visio_AddIn_rewrite.Verification
{
    /// <summary>
    /// Robuste Element-Factory fuer die Verifikation: delegiert an die Standard-
    /// <see cref="BasicPASSProcessModelElementFactory"/>, faengt aber Fehler beim
    /// Instanziieren EINES Individuums ab. Die Original-Factory wirft (statt sauber
    /// zu ueberspringen) eine NullReferenceException, sobald eine OWL-Datei einen Typ
    /// verwendet, den weder eine C#-Klasse noch die geladenen Ontologien kennen
    /// (z. B. eine von einer aelteren Stencil-Version exportierte Spezifikation mit
    /// „FinalizedMessageConnector"). Das riss bislang die gesamte Verifikation ab.
    /// Hier wird das betroffene Element uebersprungen (wie es die Basis-Factory bei
    /// unbekannten Typen ohnehin tut) und der Rest des Modells geparst.
    /// </summary>
    public class NullSafeModelElementFactory : IPASSProcessModelElementFactory<IParseablePASSProcessModelElement>
    {
        private readonly BasicPASSProcessModelElementFactory _inner = new BasicPASSProcessModelElementFactory();

        /// <summary>
        /// OWL-Typen (lokaler Name, namespace-unabhaengig), die die Basis-Factory in
        /// alps.net.api 0.9.1.6 nicht instanziieren kann und bei denen sie statt eines
        /// sauberen Skips eine <see cref="NullReferenceException"/> wirft. Fuer diese Typen
        /// wird die Basis-Factory gar nicht erst aufgerufen — sonst haelt der VS-Debugger bei
        /// jedem (gefangenen) First-Chance-Wurf an, was den Nutzer zwingt, „Weiter" zu
        /// klicken. Das Ergebnis ist identisch zum bisherigen catch-Pfad (Individuum wird
        /// uebersprungen), nur ohne die geworfene Ausnahme.
        /// </summary>
        private static readonly HashSet<string> UnresolvableTypes =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "AbstractMessageExchange",
                "FinalizedMessageExchange",
            };

        public string createInstance(
            IDictionary<string, IList<(ITreeNode<IParseablePASSProcessModelElement>, int)>> parsingDict,
            IList<string> names,
            out IParseablePASSProcessModelElement element)
        {
            // Vorab-Filter: Sind ALLE Typen des Individuums bekannt-unaufloesbar, wird die
            // Basis-Factory uebersprungen, damit sie die NRE gar nicht erst wirft (kein
            // Debugger-Halt). Nur wenn wenigstens ein Typ aufloesbar sein koennte, wird
            // delegiert — dort faengt der catch-Block einen etwaigen NEUEN Problemtyp
            // weiterhin ab (dann einmalig mit Debugger-Halt; der lokale Name steht im Log).
            if (names != null && names.Count > 0 &&
                names.All(n => UnresolvableTypes.Contains(LocalName(n))))
            {
                element = new PASSProcessModelElement();
                return null;
            }

            try
            {
                return _inner.createInstance(parsingDict, names, out element);
            }
            catch (Exception ex)
            {
                // Unbekannter/unaufloesbarer Typ: Element ueberspringen statt den Parse
                // abzubrechen. Rueckgabe null + Platzhalter-Element entspricht dem
                // Verhalten der Basis-Factory bei nicht erkannten Typen.
                string joinedNames = names == null ? "" : string.Join(", ", names);
                System.Diagnostics.Debug.WriteLine(
                    "Verification: Individuum uebersprungen (Typen: " + joinedNames + "): " + ex.Message);
                element = new PASSProcessModelElement();
                return null;
            }
        }

        /// <summary>Lokaler Name einer OWL-Typ-URI (Teil nach dem letzten '#' bzw. '/').</summary>
        private static string LocalName(string uri)
        {
            if (string.IsNullOrEmpty(uri)) return uri;
            int cut = uri.LastIndexOfAny(new[] { '#', '/' });
            return cut >= 0 && cut < uri.Length - 1 ? uri.Substring(cut + 1) : uri;
        }
    }
}
