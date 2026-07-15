using System;
using System.Collections.Generic;
using System.Linq;
using alps.net.api.ALPS;
using alps.net.api.StandardPASS;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite
{
    /// <summary>
    /// Baut aus dem aktuell geoeffneten Visio-Dokument ein <see cref="IPASSProcessModel"/>
    /// im Speicher — die Add-In-eigene Alternative zum VBA-OWL-Export des SID-Stencils
    /// (ALPS_RDFOWLExporter.createProcessRDFOWL), dessen Shape-Konventionen hier exakt
    /// nachgebildet sind (Kategorien, Prop.-Zellen, Hyperlink "linkedSBD",
    /// User.idOfCorrespondingShape der Message-Connectoren). Kein Speichern-Zwang,
    /// keine Stencil-MsgBox, kein Temp-OWL: BPMN-Konverter und Verification nehmen
    /// das Modell direkt entgegen.
    ///
    /// Abgedeckt ist der Standard-PASS-Kern: Layer (SID-Seiten), FullySpecified-/
    /// Interface-Subjects, MessageSpecifications + MessageExchanges, Basis-SBDs mit
    /// Do/Send/Receive-States (inkl. Start/Ende) und Do/Send/Receive/SendingFailed/
    /// Time-Transitions. Nicht abgedeckte Elemente (Macro-/Guard-Behaviors,
    /// StateGroups, ChoiceSegments, Datendefinitionen) werden als Warnung gemeldet.
    /// </summary>
    public class VisioPassModelBuilder
    {
        private readonly List<string> _warnings = new List<string>();

        private readonly Dictionary<string, ISubject> _subjectsByVisioId = new Dictionary<string, ISubject>();
        private readonly Dictionary<string, IMessageSpecification> _messagesByVisioId = new Dictionary<string, IMessageSpecification>();
        private readonly Dictionary<string, IMessageSpecification> _messagesByLabel = new Dictionary<string, IMessageSpecification>();
        private readonly Dictionary<string, IState> _statesByVisioId = new Dictionary<string, IState>();
        private readonly HashSet<string> _stateIdsWithIncoming = new HashSet<string>();

        public IList<string> Warnings => _warnings;

        /// <summary>Kurzname des Modells (Dokumentname wie im VBA-Export aufbereitet).</summary>
        public string ModelName { get; private set; }

        // Zaehler fuer die Ergebnis-Zusammenfassung — macht sofort sichtbar, wenn der
        // Builder nichts (oder zu wenig) aus dem Dokument gelesen hat.
        private int _subjectCount;
        private int _messageCount;
        private int _exchangeCount;
        private int _behaviorCount;
        private int _stateCount;
        private int _transitionCount;

        /// <summary>Eine Zeile Statistik fuer den Ergebnisdialog.</summary>
        public string DescribeSummary()
        {
            return "Aus dem Dokument gelesen: " + _subjectCount + " Subjekte, " + _messageCount +
                   " Nachrichten (" + _exchangeCount + " Exchanges), " + _behaviorCount +
                   " Verhalten mit " + _stateCount + " Zuständen und " + _transitionCount + " Transitionen.";
        }

        /// <summary>Prueft ohne Seiteneffekte, ob das aktive Dokument ein ALPS-Modell traegt.</summary>
        public static bool CanBuildFromActiveDocument(Visio.Application app)
        {
            try
            {
                Visio.Document doc = app?.ActiveDocument;
                if (doc == null || doc.Type != Visio.VisDocumentTypes.visTypeDrawing)
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

        /// <summary>Baut das Modell aus dem aktiven Dokument (wirft bei fehlenden Voraussetzungen).</summary>
        public IPASSProcessModel BuildFromActiveDocument(Visio.Application app)
        {
            Visio.Document doc = app?.ActiveDocument;
            if (doc == null || doc.Type != Visio.VisDocumentTypes.visTypeDrawing)
                throw new InvalidOperationException("Es ist kein Zeichnungsdokument aktiv.");

            // SID-Seiten: tragen Prop.modelURI (SBD-Seiten nicht). Wie im VBA-Export
            // bestimmt die erste gefundene Seite die Modell-URI; nur Seiten mit
            // derselben URI werden Layer dieses Modells.
            var sidPages = new List<Visio.Page>();
            string modelUri = null;
            foreach (Visio.Page page in doc.Pages)
            {
                if (page.PageSheet.CellExistsU["Prop." + Constants.Properties.PageModelURI, 0] == 0)
                    continue;
                string pageUri = page.PageSheet.CellsU["Prop." + Constants.Properties.PageModelURI].ResultStr[""];
                if (string.IsNullOrWhiteSpace(pageUri))
                    continue;
                if (modelUri == null)
                    modelUri = pageUri;
                if (pageUri == modelUri)
                    sidPages.Add(page);
            }
            if (modelUri == null || sidPages.Count == 0)
                throw new InvalidOperationException(
                    "Das aktive Dokument enthält keine SID-Seite mit Modell-URI (Prop.modelURI) — kein ALPS/PASS-Modell.");

            ModelName = doc.Name.Replace(" ", "_").Replace(".vsdx", "").Replace(".vsdm", "");

            var model = new PASSProcessModel(modelUri, ModelName);

            bool first = true;
            foreach (Visio.Page sidPage in sidPages)
            {
                IModelLayer layer = BuildLayer(model, sidPage, first);
                first = false;

                // Erster Durchlauf: Subjekte (damit Message-Connectoren und SBDs sie aufloesen koennen).
                foreach (Visio.Shape shape in sidPage.Shapes)
                    SafeParse(shape, () => ParseSubjectShape(layer, shape));

                // Zweiter Durchlauf: Nachrichten (brauchen Sender/Empfaenger).
                foreach (Visio.Shape shape in sidPage.Shapes)
                    SafeParse(shape, () => ParseMessageConnector(layer, shape));

                // Dritter Durchlauf: Verhalten (SBD-Seiten der Subjekte).
                foreach (Visio.Shape shape in sidPage.Shapes)
                    SafeParse(shape, () => ParseSubjectBehavior(layer, shape, doc));
            }

            // Nichts gefunden? Dann die tatsaechlichen Shape-Kategorien der SID-Seiten
            // in die Warnungen kippen — das macht die Ursache (falsche Seite aktiv,
            // unerwartete Kategorien, leeres Dokument) ohne Debugger sichtbar.
            if (_subjectCount == 0)
            {
                _warnings.Add("Keine Subjekte gefunden! Shapes auf den SID-Seiten (" + sidPages.Count + " Seite(n)):");
                int listed = 0;
                foreach (Visio.Page sidPage in sidPages)
                {
                    foreach (Visio.Shape shape in sidPage.Shapes)
                    {
                        if (listed++ >= 15) { _warnings.Add("…"); break; }
                        string categories = "";
                        try
                        {
                            if (shape.CellExistsU["User.msvShapeCategories", 0] != 0)
                                categories = shape.CellsU["User.msvShapeCategories"].ResultStr[""];
                        }
                        catch { }
                        _warnings.Add("  " + shape.NameU + " [Kategorien: " + categories + "]");
                    }
                    if (listed >= 15) break;
                }
            }

            return model;
        }

        private IModelLayer BuildLayer(IPASSProcessModel model, Visio.Page sidPage, bool isFirst)
        {
            string layerName = GetProp(sidPage.PageSheet, Constants.Properties.PageLayer);
            if (string.IsNullOrWhiteSpace(layerName))
                layerName = sidPage.NameU;

            var layer = new ModelLayer(model, layerName);
            if (isFirst)
                model.setBaseLayer(layer);

            string extends = GetProp(sidPage.PageSheet, "extends");
            if (!string.IsNullOrWhiteSpace(extends))
                _warnings.Add("Layer „" + layerName + "“ erweitert „" + extends +
                              "“ — Extension-Layer werden vom Direkt-Export noch nicht abgebildet.");
            return layer;
        }

        // -------------------------------------------------------------------------
        // SID: Subjekte
        // -------------------------------------------------------------------------

        private void ParseSubjectShape(IModelLayer layer, Visio.Shape shape)
        {
            if (shape.HasCategory("StandardActor"))
            {
                string id = GetProp(shape, "modelComponentID");
                string label = GetProp(shape, "lable");

                // Instanz-Restriktion wie im VBA-Export: Einzel-Subjekt = 1,
                // MultiSubject ohne Angabe/"*" = unbegrenzt (hier 99999).
                int maxInstances = 1;
                bool isMulti = GetPropBool(shape, "multiSubject");
                if (isMulti)
                    maxInstances = 99999;
                string explicitMax = GetProp(shape, "maximumNumberOfInstantiation");
                if (!string.IsNullOrWhiteSpace(explicitMax) && explicitMax != "*" && int.TryParse(explicitMax, out int parsedMax))
                    maxInstances = parsedMax;

                var subject = new FullySpecifiedSubject(layer, label, maxSubjectInstanceRestriction: maxInstances);
                RegisterSubject(id, label, subject);
            }
            else if (shape.HasCategory("InterfaceActor") || shape.HasCategory("SubjectGroup"))
            {
                string id = GetProp(shape, "modelComponentID");
                string label = GetProp(shape, "lable");
                var subject = new InterfaceSubject(layer, label);
                RegisterSubject(id, label, subject);
            }
            else if (shape.HasCategory("ActorExtension") || shape.HasCategory("StandAloneMacroSubject") || shape.HasCategory("AbstractActor"))
            {
                _warnings.Add("Subjekt-Shape „" + shape.NameU +
                              "“ (Extension/Macro/Abstract) wird vom Direkt-Export noch nicht abgebildet.");
            }
        }

        private void RegisterSubject(string visioId, string label, ISubject subject)
        {
            _subjectCount++;
            if (!string.IsNullOrWhiteSpace(visioId) && !_subjectsByVisioId.ContainsKey(visioId))
                _subjectsByVisioId[visioId] = subject;
            // Die Send-/Receive-Transitions referenzieren den Partner teils ueber das
            // LABEL (Prop.receivingSubject/senderOfMessage) — beide Schluessel ablegen.
            if (!string.IsNullOrWhiteSpace(label) && !_subjectsByVisioId.ContainsKey(label))
                _subjectsByVisioId[label] = subject;
        }

        // -------------------------------------------------------------------------
        // SID: Nachrichten (Connector + zugehoerige Message-Box)
        // -------------------------------------------------------------------------

        private void ParseMessageConnector(IModelLayer layer, Visio.Shape connector)
        {
            if (!(connector.HasCategory("StandardMessageConnector")
                  || connector.HasCategory("AbstractMessageConnector")
                  || connector.HasCategory("FinalizedMessageConnector")))
                return;

            // Prop.originSubject/targetSubject schreibt die Stencil-VBA beim manuellen
            // Zeichnen -- auf per OWL-Import erzeugten Dokumenten sind sie leer (der
            // Import klebt die Connectoren nur). Fallback: die physisch angeklebten
            // Shapes an Begin (Sender) und End (Empfaenger).
            ISubject sender = ResolveSubject(GetProp(connector, "originSubject"))
                ?? ResolveSubject(GetProp(GetGluedShape(connector, atBegin: true), "modelComponentID"));
            ISubject receiver = ResolveSubject(GetProp(connector, "targetSubject"))
                ?? ResolveSubject(GetProp(GetGluedShape(connector, atBegin: false), "modelComponentID"));
            if (sender == null || receiver == null)
            {
                _warnings.Add("Message-Connector „" + connector.NameU +
                              "“: Sender oder Empfänger nicht auflösbar — Nachrichten dieses Connectors übersprungen.");
                return;
            }

            foreach (Visio.Shape messageShape in GetMessageShapesOf(connector))
            {
                string msgId = GetProp(messageShape, "modelComponentID");
                string msgLabel = GetProp(messageShape, "lable");

                if (!_messagesByVisioId.TryGetValue(msgId ?? "", out IMessageSpecification spec))
                {
                    spec = new MessageSpecification(layer, msgLabel);
                    _messageCount++;
                    if (!string.IsNullOrWhiteSpace(msgId))
                        _messagesByVisioId[msgId] = spec;
                    if (!string.IsNullOrWhiteSpace(msgLabel) && !_messagesByLabel.ContainsKey(msgLabel))
                        _messagesByLabel[msgLabel] = spec;
                }

                var exchange = new MessageExchange(layer,
                    "Message: " + msgLabel + " From: " + FirstLabelOf(sender) + " To: " + FirstLabelOf(receiver));
                exchange.setMessageType(spec);
                exchange.setSender(sender);
                exchange.setReceiver(receiver);
                _exchangeCount++;
            }
        }

        /// <summary>
        /// Message-Shapes eines Connectors: der Connector traegt in
        /// User.idOfCorrespondingShape die Shape-ID seiner Message-Box (Container);
        /// deren Mitglieder mit Kategorie "alpsMessage" sind die Nachrichten.
        /// </summary>
        private IEnumerable<Visio.Shape> GetMessageShapesOf(Visio.Shape connector)
        {
            var result = new List<Visio.Shape>();
            if (connector.CellExistsU["User.idOfCorrespondingShape", 0] == 0)
                return result;

            int boxId = (int)connector.CellsU["User.idOfCorrespondingShape"].Result[""];
            Visio.Shape box = null;
            try { box = connector.ContainingPage.Shapes.ItemFromID[boxId]; }
            catch { /* Box geloescht/nicht vorhanden */ }
            if (box?.ContainerProperties == null)
                return result;

            Array memberIds = (Array)box.ContainerProperties.GetMemberShapes(
                (int)Visio.VisContainerFlags.visContainerFlagsDefault);
            foreach (object idObj in memberIds)
            {
                Visio.Shape member;
                try { member = connector.ContainingPage.Shapes.ItemFromID[Convert.ToInt32(idObj)]; }
                catch { continue; }
                if (member.HasCategory("alpsMessage"))
                    result.Add(member);
            }
            return result;
        }

        // -------------------------------------------------------------------------
        // SBD: Verhalten, Zustaende, Transitionen
        // -------------------------------------------------------------------------

        private void ParseSubjectBehavior(IModelLayer layer, Visio.Shape subjectShape, Visio.Document doc)
        {
            if (!subjectShape.HasCategory("StandardActor"))
                return;

            string subjectId = GetProp(subjectShape, "modelComponentID");
            if (!_subjectsByVisioId.TryGetValue(subjectId ?? "", out ISubject subject) ||
                !(subject is IFullySpecifiedSubject fullSubject))
                return;

            Visio.Page sbdPage = GetLinkedSbdPage(subjectShape, doc);
            if (sbdPage == null)
            {
                _warnings.Add("Subjekt „" + GetProp(subjectShape, "lable") + "“ hat keine verlinkte SBD-Seite — Verhalten fehlt.");
                return;
            }

            string behaviorLabel = sbdPage.NameU.Replace(":", "_");

            // Der FullySpecifiedSubject-Ctor legt automatisch ein leeres
            // "defaultBehavior" an. Nach dem Ersetzen entfernen -- es bliebe sonst als
            // nicht unterstuetztes Rumpf-Behavior im Modell und der BPMN-Konverter
            // warnt bei jedem Lauf darueber.
            var defaultBehavior = fullSubject.getSubjectBaseBehavior();

            var behavior = new SubjectBaseBehavior(layer, behaviorLabel, subject);
            fullSubject.setBaseBehavior(behavior);
            if (defaultBehavior != null && !ReferenceEquals(defaultBehavior, behavior))
            {
                fullSubject.removeBehavior(defaultBehavior.getModelComponentID());
                layer.removeContainedElement(defaultBehavior.getModelComponentID());
            }
            _behaviorCount++;

            _statesByVisioId.Clear();
            _stateIdsWithIncoming.Clear();

            // Erst alle Zustaende, dann die Transitionen (brauchen Quell-/Zielzustand).
            foreach (Visio.Shape shape in sbdPage.Shapes)
                SafeParse(shape, () => ParseState(behavior, shape));
            foreach (Visio.Shape shape in sbdPage.Shapes)
                SafeParse(shape, () => ParseTransition(shape));

            EnsureInitialState(behaviorLabel);
        }

        /// <summary>
        /// Stellt sicher, dass das Verhalten einen Startzustand traegt: ohne
        /// InitialStateOfBehavior erzeugt der BPMN-Konverter kein StartEvent, und bei
        /// zyklischen SBDs findet der Diagramm-Layouter dann keinen Einstieg (der
        /// Prozess bliebe leer). Fallback: erster Zustand ohne eingehende Transition,
        /// sonst der erste Zustand der Seite.
        /// </summary>
        private void EnsureInitialState(string behaviorLabel)
        {
            if (_statesByVisioId.Count == 0)
                return;
            if (_statesByVisioId.Values.Any(s => s.isStateType(IState.StateType.InitialStateOfBehavior)))
                return;

            IState fallback = _statesByVisioId.FirstOrDefault(p => !_stateIdsWithIncoming.Contains(p.Key)).Value
                ?? _statesByVisioId.Values.First();
            fallback.setIsStateType(IState.StateType.InitialStateOfBehavior);
            _warnings.Add("Verhalten „" + behaviorLabel + "“: kein Zustand als Start markiert — „"
                + FirstLabelOf(fallback) + "“ wurde als Startzustand angenommen.");
        }

        private static string FirstLabelOf(IState state)
        {
            IList<string> labels = state.getModelComponentLabelsAsStrings();
            return labels.Count > 0 ? labels[0] : state.getModelComponentID();
        }

        private Visio.Page GetLinkedSbdPage(Visio.Shape subjectShape, Visio.Document doc)
        {
            string subAddress;
            try { subAddress = subjectShape.Hyperlinks.ItemU["linkedSBD"].SubAddress; }
            catch { return null; }
            if (string.IsNullOrWhiteSpace(subAddress))
                return null;

            foreach (Visio.Page page in doc.Pages)
            {
                if (page.NameU == subAddress)
                    return page;
            }
            return null;
        }

        private void ParseState(ISubjectBehavior behavior, Visio.Shape shape)
        {
            string type = GetProp(shape, "modelComponentType");
            if (type == "FunctionState")
                type = "DoState";
            if (type != "DoState" && type != "SendState" && type != "ReceiveState")
            {
                if (type == "StateGroup" || type == "ChoiceSegment" || type == "MacroState" || type == "StateReference" || type == "StateExtension")
                    _warnings.Add("SBD-Element „" + shape.NameU + "“ (" + type + ") wird vom Direkt-Export noch nicht abgebildet.");
                return;
            }

            string id = GetProp(shape, "modelComponentID");
            string label = GetProp(shape, "lable");

            IState state;
            switch (type)
            {
                case "SendState": state = new SendState(behavior); break;
                case "ReceiveState": state = new ReceiveState(behavior); break;
                default: state = new DoState(behavior); break;
            }
            if (!string.IsNullOrWhiteSpace(label))
                state.addModelComponentLabel(label);

            if (GetPropBool(shape, "isStartState"))
                state.setIsStateType(IState.StateType.InitialStateOfBehavior);
            if (GetPropBool(shape, "isEndState"))
                state.setIsStateType(IState.StateType.EndState);

            _stateCount++;
            if (!string.IsNullOrWhiteSpace(id))
                _statesByVisioId[id] = state;
        }

        private void ParseTransition(Visio.Shape shape)
        {
            string type = GetProp(shape, "modelComponentType");
            if (string.IsNullOrWhiteSpace(type) || !type.Contains("Transition"))
                return;
            // VBA-Export: "Standard"-Praefix wird entfernt (StandardDoTransition -> DoTransition).
            type = type.Replace("Standard", "");

            string id = GetProp(shape, "modelComponentID");
            string label = GetProp(shape, "lable");

            // Prop.originState/targetState schreibt die Stencil-VBA beim manuellen
            // Zeichnen -- auf importierten Dokumenten sind sie leer. Fallback: die
            // physisch angeklebten Zustands-Shapes (Begin = Quelle, End = Ziel; so
            // klebt auch der OWL-Import die Transitions).
            string sourceId = GetProp(shape, "originState");
            string targetId = GetProp(shape, "targetState");
            if (!_statesByVisioId.ContainsKey(sourceId ?? ""))
                sourceId = GetProp(GetGluedShape(shape, atBegin: true), "modelComponentID");
            if (!_statesByVisioId.ContainsKey(targetId ?? ""))
                targetId = GetProp(GetGluedShape(shape, atBegin: false), "modelComponentID");

            _statesByVisioId.TryGetValue(sourceId ?? "", out IState source);
            _statesByVisioId.TryGetValue(targetId ?? "", out IState target);
            if (source == null || target == null)
            {
                _warnings.Add("Transition „" + (string.IsNullOrWhiteSpace(label) ? shape.NameU : label) +
                              "“: Quell- oder Zielzustand nicht auflösbar — übersprungen.");
                return;
            }
            _stateIdsWithIncoming.Add(targetId);

            if (type.Contains("SendTransition"))
            {
                var transition = new SendTransition(source, target, label);
                var condition = new SendTransitionCondition(transition);
                IMessageSpecification spec = ResolveMessageOf(shape);
                if (spec != null)
                    condition.setRequiresSendingOfMessage(spec);
                else
                    _warnings.Add("Send-Transition „" + label + "“: Nachricht nicht auflösbar.");
                transition.setTransitionCondition(condition);
                _transitionCount++;
            }
            else if (type.Contains("ReceiveTransition"))
            {
                var transition = new ReceiveTransition(source, target, label);
                var condition = new ReceiveTransitionCondition(transition);
                IMessageSpecification spec = ResolveMessageOf(shape);
                if (spec != null)
                    condition.setReceptionOfMessage(spec);
                else
                    _warnings.Add("Receive-Transition „" + label + "“: Nachricht nicht auflösbar.");
                transition.setTransitionCondition(condition);
                _transitionCount++;
            }
            else if (type.Contains("SendingFailed"))
            {
                new SendingFailedTransition(source, target, label);
                _transitionCount++;
            }
            else if (type.Contains("Time"))
            {
                new TimeTransition(source, target, label);
                _transitionCount++;
                _warnings.Add("Time-Transition „" + label + "“: Zeitbedingung wird vom Direkt-Export noch nicht übernommen.");
            }
            else
            {
                new DoTransition(source, target, label);
                _transitionCount++;
            }
        }

        /// <summary>Nachricht einer Send-/Receive-Transition: Prop.Message traegt das Nachrichten-LABEL.</summary>
        private IMessageSpecification ResolveMessageOf(Visio.Shape transitionShape)
        {
            string messageLabel = GetProp(transitionShape, "Message");
            if (string.IsNullOrWhiteSpace(messageLabel))
                return null;
            _messagesByLabel.TryGetValue(messageLabel, out IMessageSpecification spec);
            return spec;
        }

        // -------------------------------------------------------------------------
        // Helfer
        // -------------------------------------------------------------------------

        // System.Action ausschreiben: alps.net.api.StandardPASS bringt eine eigene
        // "Action"-Klasse mit (PASS-Ontologie) -- der kurze Name waere mehrdeutig.
        private void SafeParse(Visio.Shape shape, System.Action parse)
        {
            try
            {
                parse();
            }
            catch (Exception ex)
            {
                _warnings.Add("Shape „" + shape.NameU + "“ konnte nicht übernommen werden: " + ex.Message);
            }
        }

        /// <summary>Liest Prop.&lt;propName&gt; einer Shape (auch PageSheets sind Shapes); "" wenn Shape/Zelle fehlt.</summary>
        private static string GetProp(Visio.Shape shape, string propName)
        {
            if (shape == null || shape.CellExistsU["Prop." + propName, 0] == 0)
                return "";
            return shape.CellsU["Prop." + propName].ResultStr[""] ?? "";
        }

        private ISubject ResolveSubject(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;
            _subjectsByVisioId.TryGetValue(key, out ISubject subject);
            return subject;
        }

        /// <summary>
        /// Das an Begin bzw. End des 1D-Connectors angeklebte Shape (oder null).
        /// </summary>
        private static Visio.Shape GetGluedShape(Visio.Shape connector, bool atBegin)
        {
            try
            {
                foreach (Visio.Connect connect in connector.Connects)
                {
                    bool isBegin = connect.FromPart == (short)Visio.VisFromParts.visBegin;
                    if (isBegin == atBegin)
                        return connect.ToSheet;
                }
            }
            catch
            {
                // Nicht geklebt/kein 1D-Shape -- dann eben kein Fallback.
            }
            return null;
        }

        private static string FirstLabelOf(ISubject subject)
        {
            IList<string> labels = subject.getModelComponentLabelsAsStrings();
            return labels.Count > 0 ? labels[0] : subject.getModelComponentID();
        }

        private static bool GetPropBool(Visio.Shape shape, string propName)
        {
            if (shape.CellExistsU["Prop." + propName, 0] == 0)
                return false;
            return shape.CellsU["Prop." + propName].Result[""] != 0;
        }
    }
}
