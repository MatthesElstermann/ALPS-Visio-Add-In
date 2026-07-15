using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using alps.net.api.ALPS;
using alps.net.api.StandardPASS;
using ALPS_Visio_AddIn_rewrite.OWLShapes;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.Verification
{
    /// <summary>
    /// Gegenstueck zur ALPS-Verifikation: erzeugt aus einer abstrakten Spezifikation
    /// (OWL-Datei) ein NEUES implementierendes Modell direkt in Visio — mit gesetzten
    /// „implements"-Verweisen auf die Spezifikations-Elemente.
    ///
    /// Vorgehen: Die Spezifikation wird mit denselben (plain) API-Klassen geladen wie
    /// bei der Verifikation (inkl. <see cref="NullSafeModelElementFactory"/>), damit die
    /// implements-Verweise exakt die URIs tragen, die die Verifikation spaeter vergleicht
    /// (<c>getUriModelComponentID()</c>). Das Implementierungs-Modell wird dann aus den
    /// Visio*-Klassen aufgebaut und ueber die vorhandene Import-Pipeline gezeichnet:
    /// je Spezifikations-Subjekt ein FullySpecified-Subjekt (mit implements-Verweis und
    /// leerer SBD-Seite als Startpunkt), je Spezifikations-Nachricht ein Message-Connector
    /// zwischen den implementierenden Subjekten.
    ///
    /// Bewusst NICHT uebernommen:
    /// - CommunicationRestrictions: Sie beschraenken, was die Implementierung tun darf —
    ///   sie sind Teil der Spezifikation, nicht der Implementierung. (Zudem kennt
    ///   alps.net.api 0.9.1.6 keinen Element-Typ, der implements-Verweise auf
    ///   Restrictions tragen koennte — sie erscheinen im Verifikations-Report daher
    ///   prinzipbedingt als „not implemented".)
    /// - SBD-Inhalte der Spezifikation: Das konkrete Verhalten IST die eigentliche
    ///   Implementierungsarbeit; erzeugt werden leere SBD-Seiten je Subjekt.
    /// </summary>
    public class ImplementationScaffolder
    {
        private readonly List<string> _notes = new List<string>();

        /// <summary>Hinweise fuer den Ergebnisdialog (uebersprungene/nicht abbildbare Elemente).</summary>
        public IList<string> Notes => _notes;

        /// <summary>Anzahl erzeugter Subjekte.</summary>
        public int SubjectCount { get; private set; }

        /// <summary>Anzahl erzeugter Nachrichten-Exchanges.</summary>
        public int MessageCount { get; private set; }

        /// <summary>Name des erzeugten Implementierungs-Modells.</summary>
        public string ModelName { get; private set; }

        // Vergebene IDs (fuer eindeutige, lesbare Namen ohne GUID-Anhaengsel).
        private readonly HashSet<string> _usedIds = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// Laedt die Spezifikation, baut das implementierende Modell und zeichnet es in
        /// das aktive Dokument (bzw. ein neues, falls keines offen ist).
        /// </summary>
        public void ScaffoldFromSpec(string specPath, Visio.Application app)
        {
            // --- 1. Spezifikation laden (wie bei der Verifikation) -------------------------
            IList<IPASSProcessModel> specModels = Verifier.LoadModels(new List<string> { specPath });
            if (specModels.Count < 1)
                throw new InvalidOperationException(
                    "Die Spezifikations-Datei konnte nicht als ALPS-Modell geladen werden:\n" + specPath);
            IPASSProcessModel spec = specModels[0];

            // --- 2. Implementierungs-Modell aus den Visio*-Klassen aufbauen ----------------
            string specName = SanitizeName(Path.GetFileNameWithoutExtension(specPath));
            ModelName = specName + "_Implementation";
            var model = new VisioPASSProcessModel(
                "http://subjective-me.jimdo.com/s-bpm/processmodels/" + ModelName, ModelName);
            var layer = new VisioModelLayer(model);
            model.setBaseLayer(layer);

            // Lesbare IDs statt der API-generierten GUID-Anhaengsel ("ModelLayer-<guid>"):
            // die Layer-ID wird zum Namen der SID-Seite, die Subjekt-IDs erscheinen im
            // Model Explorer, als Prop.modelComponentID und im SBD-Seitennamen.
            // setModelComponentID propagiert die Aenderung sauber (Model und Layer
            // re-keyen ihre Element-Dictionaries via notifyModelComponentIDChanged).
            layer.setModelComponentID(UniqueId("SID_1"));

            // 2a. Subjekte: jedes Spezifikations-Subjekt (auch abstrakte/Interface-Subjekte)
            // bekommt ein konkretes FullySpecified-Gegenstueck mit implements-Verweis.
            // SubjectImport schreibt den Verweis beim Zeichnen als Prop.implements auf die
            // Shape — genau die Zelle, die der VisioPassModelBuilder bei der Verifikation
            // des aktuellen Modells wieder einliest.
            var implBySpecId = new Dictionary<string, ISubject>();
            foreach (ISubject specSubject in spec.getAllElements().Values.OfType<ISubject>())
            {
                string label = FirstLabelOf(specSubject);
                var implSubject = new VisioFullySpecifiedSubject(layer, label);
                implSubject.setModelComponentID(UniqueId(SanitizeName(label)));

                // Leeres, zeichenbares Basisverhalten setzen: SubjectImport legt dann eine
                // (leere) SBD-Seite an und verlinkt sie — der Startpunkt fuer die eigentliche
                // Verhaltens-Modellierung. (Das Feld ist als ISubjectBehavior typisiert; der
                // OWL-Import setzt hier ebenfalls VisioSubjectBehavior-Instanzen.)
                // Gefahrlos trotz Behavior-Tausch (vgl. Ä72): es werden keine Zustaende
                // registriert, das Behavior bleibt leer.
                var behavior = new VisioSubjectBehavior(layer, label + " Behavior");
                behavior.setModelComponentID(UniqueId(SanitizeName(label) + "_Behavior"));
                implSubject.setBaseBehavior(behavior);

                implSubject.addImplementedInterfaceIDReference(specSubject.getUriModelComponentID());
                implBySpecId[specSubject.getModelComponentID()] = implSubject;
                SubjectCount++;
            }
            if (SubjectCount == 0)
                throw new InvalidOperationException(
                    "Die Spezifikation enthält keine Subjekte — es gibt nichts zu implementieren.");

            // 2b. Nachrichten: je Spezifikations-Exchange ein Exchange zwischen den
            // implementierenden Subjekten; je (Sender, Empfaenger) ein Connector
            // (MessageExchangeList), je (Connector, Nachricht) eine eigene
            // MessageSpecification-Instanz (eine geteilte Instanz wuerde beim Zeichnen
            // mehrfach platziert und verloere ihre Shape-Referenz).
            var listsByPair = new Dictionary<string, VisioMessageExchangeList>();
            var messagesByPairAndLabel = new Dictionary<string, VisioMessageSpecification>();
            foreach (IMessageExchange specExchange in spec.getAllElements().Values.OfType<IMessageExchange>())
            {
                ISubject implSender = ResolveImpl(implBySpecId, specExchange.getSender());
                ISubject implReceiver = ResolveImpl(implBySpecId, specExchange.getReceiver());
                if (implSender == null || implReceiver == null)
                {
                    _notes.Add("Nachricht „" + FirstLabelOf(specExchange) +
                               "“: Sender oder Empfänger in der Spezifikation nicht auflösbar — übersprungen.");
                    continue;
                }

                string msgLabel = specExchange.getMessageType() != null
                    ? FirstLabelOf(specExchange.getMessageType())
                    : FirstLabelOf(specExchange);

                string pairKey = specExchange.getSender().getModelComponentID() + "|"
                               + specExchange.getReceiver().getModelComponentID();
                if (!listsByPair.TryGetValue(pairKey, out VisioMessageExchangeList list))
                {
                    list = new VisioMessageExchangeList(layer);
                    list.setModelComponentID(UniqueId("MessageConnector_" +
                        SanitizeName(FirstLabelOf(implSender)) + "_" + SanitizeName(FirstLabelOf(implReceiver))));
                    listsByPair[pairKey] = list;
                }

                string msgKey = pairKey + "|" + msgLabel;
                if (!messagesByPairAndLabel.TryGetValue(msgKey, out VisioMessageSpecification msgSpec))
                {
                    msgSpec = new VisioMessageSpecification(layer);
                    msgSpec.addModelComponentLabel(msgLabel);
                    msgSpec.setModelComponentID(UniqueId(SanitizeName(msgLabel)));
                    messagesByPairAndLabel[msgKey] = msgSpec;
                }

                // Kein implements-Verweis am Exchange: MessageExchange traegt in
                // alps.net.api 0.9.1.6 keine implements-Verweise (kein
                // IImplementingElement) — die Verifikation paart Exchanges daher ohnehin
                // nicht. Die Struktur (wer sendet was an wen) wird 1:1 uebernommen.
                var exchange = new VisioMessageExchange(layer);
                exchange.setModelComponentID(UniqueId("Exchange_" + SanitizeName(msgLabel) + "_" +
                    SanitizeName(FirstLabelOf(implSender)) + "_" + SanitizeName(FirstLabelOf(implReceiver))));
                exchange.addModelComponentLabel(
                    "Message: " + msgLabel + " From: " + FirstLabelOf(implSender) + " To: " + FirstLabelOf(implReceiver));
                exchange.setMessageType(msgSpec);
                exchange.setSender(implSender);
                exchange.setReceiver(implReceiver);
                list.addContainsMessageExchange(exchange);
                MessageCount++;
            }

            // --- 3. Nicht uebernommene Spezifikations-Anteile transparent machen ------------
            if (MessageCount == 0 && SubjectCount > 1)
                _notes.Add("Keine Nachrichten übernommen — die Message-Exchanges der Spezifikation konnten " +
                           "nicht geparst werden (z. B. Abstract-/FinalizedMessageExchange, die alps.net.api " +
                           "nicht instanziieren kann). Nachrichten-Connectoren bitte manuell zeichnen.");
            int restrictionCount = spec.getAllElements().Values.OfType<ICommunicationRestriction>().Count();
            if (restrictionCount > 0)
                _notes.Add(restrictionCount + " Kommunikations-Restriktion(en) der Spezifikation nicht übernommen — " +
                           "Restriktionen beschränken die Implementierung, sind aber nicht Teil von ihr. " +
                           "(Im Verifikations-Report bleiben sie API-bedingt als „not implemented“ gelistet.)");
            int specStateCount = spec.getAllElements().Values.OfType<IState>().Count();
            if (specStateCount > 0)
                _notes.Add("SBD-Inhalte der Spezifikation (" + specStateCount + " Zustände) nicht übernommen — " +
                           "das konkrete Verhalten bitte in den erzeugten (leeren) SBD-Seiten modellieren.");

            // --- 4. Zeichnen ueber die vorhandene Import-Pipeline ---------------------------
            // Gleiche Umgebung wie OWLImporter.Parse: Stencil-VBA still halten (verhindert
            // das Umbenennen der SID-Seite durch die Willkommens-Routine), SID-Stencil
            // oeffnen (Master + EventDrop-Logik der Message-Box), nur ScreenUpdating aus
            // (bewusst NICHT EventsEnabled/DeferRecalc — siehe OWLImporter).
            VH.setVBAListenersRunning(false);
            VH.openStencil(VH.VisioStencils.SID_STENCIL);

            short prevScreenUpdating = app.ScreenUpdating;
            app.ScreenUpdating = 0;
            try
            {
                ((IVisioImportable)model).ImportToVisio(null);
            }
            finally
            {
                app.ScreenUpdating = prevScreenUpdating;
            }
        }

        /// <summary>
        /// Liefert eine modellweit eindeutige, lesbare ID: die Basis unveraendert, bei
        /// Kollision mit Zaehler-Suffix („Subject_2", „Subject_2_2", …).
        /// </summary>
        private string UniqueId(string baseId)
        {
            if (string.IsNullOrWhiteSpace(baseId))
                baseId = "Element";
            string candidate = baseId;
            int counter = 2;
            while (!_usedIds.Add(candidate))
                candidate = baseId + "_" + counter++;
            return candidate;
        }

        private static ISubject ResolveImpl(IDictionary<string, ISubject> implBySpecId, ISubject specSubject)
        {
            if (specSubject == null)
                return null;
            implBySpecId.TryGetValue(specSubject.getModelComponentID(), out ISubject impl);
            return impl;
        }

        private static string FirstLabelOf(IPASSProcessModelElement element)
        {
            IList<string> labels = element.getModelComponentLabelsAsStrings();
            return labels.Count > 0 ? labels[0] : element.getModelComponentID();
        }

        /// <summary>Dateiname → URI-/Seitentaugliches Namenssegment.</summary>
        private static string SanitizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Model";
            var sb = new System.Text.StringBuilder(name.Length);
            foreach (char c in name)
                sb.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_');
            return sb.ToString();
        }
    }
}
