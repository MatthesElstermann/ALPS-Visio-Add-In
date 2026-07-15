using System;
using System.Collections.Generic;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;

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

        public string createInstance(
            IDictionary<string, IList<(ITreeNode<IParseablePASSProcessModelElement>, int)>> parsingDict,
            IList<string> names,
            out IParseablePASSProcessModelElement element)
        {
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
    }
}
