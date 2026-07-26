using Microsoft.Office.Tools.Ribbon;
using Microsoft.Office.Core;
using System;
using System.Linq;
using System.Windows.Forms;

namespace ALPS_Visio_AddIn_rewrite
{
    partial class ALPSRibbon : RibbonBase
    {
        /// <summary>
        /// Ribbon containing ALPS Menu
        /// </summary>
        public ALPSRibbon() : base(Globals.Factory.GetRibbonFactory())
        {
            this.RibbonType = "Microsoft.Visio.Drawing";

            RibbonTab alpsTab = this.Factory.CreateRibbonTab();
            alpsTab.Label = "ALPS/PASS ADDIN";
            this.Tabs.Add(alpsTab);

            // Group order and labels mirror the original add-in (upstream/main);
            // the "PASS NL Checker" group is new (ported NLPPASSChecking features).

            // --- Group 1: Standard Functions ---
            RibbonGroup standardGroup = this.Factory.CreateRibbonGroup();
            standardGroup.Label = "Standard Functions";
            alpsTab.Groups.Add(standardGroup);

            RibbonButton openStencilsButton = this.Factory.CreateRibbonButton();
            openStencilsButton.Name = "openStencilsButton";
            openStencilsButton.Label = "Open ALPS/PASS Stencils";
            openStencilsButton.SuperTip = "Tries to open the (necessary) ALPS Visio stencils if they are available on the system.";
            openStencilsButton.Image = Properties.Resources.document_open_7;
            openStencilsButton.ShowImage = true;
            openStencilsButton.ControlSize = RibbonControlSize.RibbonControlSizeLarge;
            openStencilsButton.Click += new RibbonControlEventHandler(this.OpenStencils);
            standardGroup.Items.Add(openStencilsButton);

            // --- Group 2: ALPS Layer Editing ---
            RibbonGroup layerGroup = this.Factory.CreateRibbonGroup();
            layerGroup.Label = "ALPS Layer Editing";
            alpsTab.Groups.Add(layerGroup);

            RibbonButton layerExplorerButton = this.Factory.CreateRibbonButton();
            layerExplorerButton.Name = "layerExplorerButton";
            layerExplorerButton.Label = "Show layer Explorer";
            layerExplorerButton.SuperTip = "Open a the layer explorer, a tool for advanced multi-layered ALPS (Abstract Layered PASS editing)";
            layerExplorerButton.OfficeImageId = "LayersMenu";
            layerExplorerButton.ShowImage = true;
            layerExplorerButton.ControlSize = RibbonControlSize.RibbonControlSizeLarge;
            layerExplorerButton.Click += new RibbonControlEventHandler(this.ShowLayerExplorer);
            layerGroup.Items.Add(layerExplorerButton);

            // --- Group 3: OWL PASS Tools ---
            RibbonGroup owlGroup = this.Factory.CreateRibbonGroup();
            owlGroup.Label = "OWL PASS Tools";
            alpsTab.Groups.Add(owlGroup);

            RibbonButton owlImporterButton = this.Factory.CreateRibbonButton();
            owlImporterButton.Name = "owlImporterButton";
            owlImporterButton.Label = "Import OWL";
            owlImporterButton.SuperTip = "Use this tool to import PASS and ALPS Process Models from OWL Files based on the standard pass ontology";
            owlImporterButton.Image = Properties.Resources.owlIcon2;
            owlImporterButton.ShowImage = true;
            owlImporterButton.ControlSize = RibbonControlSize.RibbonControlSizeLarge;
            owlImporterButton.Click += new RibbonControlEventHandler(this.LoadOWLFile);
            owlGroup.Items.Add(owlImporterButton);

            // Split-Button wie beim Auto Arrange: Klick = Standardaktion (aktuell
            // geoeffnetes Modell als Implementierung pruefen), Pfeil = Variante waehlen.
            RibbonSplitButton verificationSplitButton = this.Factory.CreateRibbonSplitButton();
            verificationSplitButton.Name = "verificationSplitButton";
            verificationSplitButton.Label = "ALPS Verification";
            verificationSplitButton.SuperTip = "Prüft das aktuell geöffnete Modell (Implementierung) gegen ein Spezifikationsmodell (OWL-Datei). Über den Pfeil lassen sich stattdessen beide Modelle als Dateien wählen.";
            verificationSplitButton.OfficeImageId = "AdpDiagramArrangeTables";
            verificationSplitButton.ControlSize = RibbonControlSize.RibbonControlSizeLarge;
            verificationSplitButton.Click += new RibbonControlEventHandler(this.VerifyCurrentModel);

            RibbonButton verifyCurrentItem = this.Factory.CreateRibbonButton();
            verifyCurrentItem.Name = "verifyCurrentItem";
            verifyCurrentItem.Label = "Aktuelles Modell prüfen";
            verifyCurrentItem.SuperTip = "Spezifikation als OWL-Datei wählen; das aktuell geöffnete Modell ist die Implementierung.";
            verifyCurrentItem.Click += new RibbonControlEventHandler(this.VerifyCurrentModel);
            verificationSplitButton.Items.Add(verifyCurrentItem);

            RibbonButton verifyFilesItem = this.Factory.CreateRibbonButton();
            verifyFilesItem.Name = "verifyFilesItem";
            verifyFilesItem.Label = "OWL-Dateien prüfen…";
            verifyFilesItem.SuperTip = "Spezifikation und Implementierung als OWL-Dateien wählen.";
            verifyFilesItem.Click += new RibbonControlEventHandler(this.AlpsVerification);
            verificationSplitButton.Items.Add(verifyFilesItem);

            owlGroup.Items.Add(verificationSplitButton);

            // Umkehrung der Verifikation: aus einer abstrakten Spezifikation ein
            // implementierendes Modell erzeugen (implements-Verweise gesetzt).
            // Eigener Button neben der Verification, damit das Tool direkt sichtbar ist.
            RibbonButton scaffoldButton = this.Factory.CreateRibbonButton();
            scaffoldButton.Name = "scaffoldImplementationButton";
            scaffoldButton.Label = "Implementierung erzeugen";
            scaffoldButton.SuperTip = "Erzeugt aus einer abstrakten Spezifikation (OWL-Datei) ein neues implementierendes Modell in Visio: je Spezifikations-Subjekt ein konkretes Subjekt mit gesetztem implements-Verweis und leerer SBD-Seite, dazu die Nachrichten-Struktur. Anschließend über „ALPS Verification“ prüfbar.";
            scaffoldButton.OfficeImageId = "TableInsert";
            scaffoldButton.ShowImage = true;
            scaffoldButton.ControlSize = RibbonControlSize.RibbonControlSizeLarge;
            scaffoldButton.Click += new RibbonControlEventHandler(this.ScaffoldImplementationFromSpec);
            owlGroup.Items.Add(scaffoldButton);

            // --- Group 4: PASS NL Checker ---
            // Eigener Ribbon-Abschnitt fuer die NL-Pruefung: Pruefung immer per lokalem
            // ML-Modell; das LLM (Provider waehlbar) liefert nur die Label-Vorschlaege.
            RibbonGroup nlGroup = this.Factory.CreateRibbonGroup();
            nlGroup.Label = "PASS NL Checker";
            alpsTab.Groups.Add(nlGroup);

            // PASS NL Checker (ML.NET label validity + LLM suggestions), ported from NLPPASSChecking.
            RibbonButton naturalLanguageButton = this.Factory.CreateRibbonButton();
            naturalLanguageButton.Name = "naturalLanguageButton";
            naturalLanguageButton.Label = "PASS NL Checker";
            naturalLanguageButton.SuperTip = "Check every shape label for its type (local ML model) and get LLM suggestions for weak labels.";
            naturalLanguageButton.OfficeImageId = "Spelling";
            naturalLanguageButton.ShowImage = true;
            naturalLanguageButton.ControlSize = RibbonControlSize.RibbonControlSizeLarge;
            naturalLanguageButton.Click += new RibbonControlEventHandler(this.PassNlChecker);
            nlGroup.Items.Add(naturalLanguageButton);

            // Lokales NL-Modell (neu) trainieren -- entspricht dem Retrain-Button des
            // Original-Add-Ins (NLPPASSChecking), der im Port bisher fehlte.
            RibbonButton retrainButton = this.Factory.CreateRibbonButton();
            retrainButton.Name = "retrainButton";
            retrainButton.Label = "NL-Modell trainieren";
            retrainButton.SuperTip = "Trainiert das lokale ML-Modell des PASS NL Checkers neu aus den mitgelieferten Trainingsdaten und ersetzt das gecachte Modell unter %APPDATA%.";
            retrainButton.OfficeImageId = "Repeat";
            retrainButton.ShowImage = true;
            retrainButton.ControlSize = RibbonControlSize.RibbonControlSizeLarge;
            retrainButton.Click += new RibbonControlEventHandler(this.RetrainNlModel);
            nlGroup.Items.Add(retrainButton);

            // NL-Checker-Einstellungen: LLM-Provider (UniGPT/OpenAI/Anthropic) sowie
            // Modell + API-Key je Provider (%APPDATA%-JSON) fuer die Label-Vorschlaege.
            RibbonButton nlSettingsButton = this.Factory.CreateRibbonButton();
            nlSettingsButton.Name = "nlSettingsButton";
            nlSettingsButton.Label = "NL-Checker Einstellungen";
            nlSettingsButton.SuperTip = "LLM-Provider (UniGPT/OpenAI/Anthropic) sowie Modell und API-Key für die Label-Vorschläge des PASS NL Checkers festlegen. Geprüft wird immer mit dem lokalen ML-Modell.";
            nlSettingsButton.OfficeImageId = "Lock";
            nlSettingsButton.ShowImage = true;
            nlSettingsButton.ControlSize = RibbonControlSize.RibbonControlSizeLarge;
            nlSettingsButton.Click += new RibbonControlEventHandler(this.OpenNlCheckerSettings);
            nlGroup.Items.Add(nlSettingsButton);

            // PASS→BPMN-Konverter (portiert von github.com/pass-bpmn-converter).
            // Split-Button: Klick = aktuell geoeffnetes Modell konvertieren,
            // Pfeil = stattdessen eine OWL-Datei als Quelle waehlen.
            RibbonSplitButton bpmnSplitButton = this.Factory.CreateRibbonSplitButton();
            bpmnSplitButton.Name = "bpmnSplitButton";
            bpmnSplitButton.Label = "PASS BPMN Converter";
            bpmnSplitButton.SuperTip = "Konvertiert das aktuell geöffnete PASS-Modell in ein BPMN-2.0-Modell (.bpmn, z. B. für bpmn.io oder Camunda). Über den Pfeil lässt sich stattdessen eine OWL-Datei konvertieren.";
            bpmnSplitButton.OfficeImageId = "FileSaveAsOtherFormats";
            bpmnSplitButton.ControlSize = RibbonControlSize.RibbonControlSizeLarge;
            bpmnSplitButton.Click += new RibbonControlEventHandler(this.ConvertCurrentModelToBpmn);

            RibbonButton bpmnCurrentItem = this.Factory.CreateRibbonButton();
            bpmnCurrentItem.Name = "bpmnCurrentItem";
            bpmnCurrentItem.Label = "Aktuelles Modell konvertieren";
            bpmnCurrentItem.SuperTip = "Konvertiert das aktuell geöffnete Modell direkt (ohne OWL-Zwischendatei).";
            bpmnCurrentItem.Click += new RibbonControlEventHandler(this.ConvertCurrentModelToBpmn);
            bpmnSplitButton.Items.Add(bpmnCurrentItem);

            RibbonButton bpmnFileItem = this.Factory.CreateRibbonButton();
            bpmnFileItem.Name = "bpmnFileItem";
            bpmnFileItem.Label = "OWL-Datei konvertieren…";
            bpmnFileItem.SuperTip = "Wählt eine PASS-OWL-Datei und konvertiert sie nach BPMN.";
            bpmnFileItem.Click += new RibbonControlEventHandler(this.ConvertOwlFileToBpmn);
            bpmnSplitButton.Items.Add(bpmnFileItem);

            RibbonButton bpmnVisioPageItem = this.Factory.CreateRibbonButton();
            bpmnVisioPageItem.Name = "bpmnVisioPageItem";
            bpmnVisioPageItem.Label = "Als BPMN-Zeichenblatt anzeigen";
            bpmnVisioPageItem.SuperTip = "Konvertiert das aktuell geöffnete Modell und zeichnet das Ergebnis " +
                "mit den Visio-BPMN-Shapes auf ein neues Zeichenblatt — ohne Datei zu speichern. " +
                "Benötigt die BPMN-Schablone von Visio Professional bzw. Visio Plan 2.";
            bpmnVisioPageItem.Click += new RibbonControlEventHandler(this.ShowCurrentModelAsBpmnPage);
            bpmnSplitButton.Items.Add(bpmnVisioPageItem);

            RibbonButton bpmnImportItem = this.Factory.CreateRibbonButton();
            bpmnImportItem.Name = "bpmnImportItem";
            bpmnImportItem.Label = "BPMN-Datei anzeigen…";
            bpmnImportItem.SuperTip = "Liest eine BPMN-2.0-Datei (z. B. aus bpmn.io oder dem Camunda Modeler) " +
                "ein und zeichnet sie mit den Visio-BPMN-Shapes auf ein neues Zeichenblatt. Enthält die Datei " +
                "kein Layout (BPMN DI), wird es automatisch erzeugt.";
            bpmnImportItem.Click += new RibbonControlEventHandler(this.ShowBpmnFileAsPage);
            bpmnSplitButton.Items.Add(bpmnImportItem);

            owlGroup.Items.Add(bpmnSplitButton);

            // Split button: clicking the button portion runs the default arrange immediately,
            // the lower arrow opens the dropdown with both directions. On a RibbonSplitButton
            // the button properties (Label/Image/Click) sit directly on the control itself.
            RibbonSplitButton arrangeSplitButton = this.Factory.CreateRibbonSplitButton();
            arrangeSplitButton.Name = "arrangeSplitButton";
            arrangeSplitButton.ControlSize = RibbonControlSize.RibbonControlSizeLarge;
            arrangeSplitButton.Label = "Auto Arrange";
            arrangeSplitButton.SuperTip = "Re-arranges the active SID or SBD page from its shapes, flowing left to right. Use the arrow to pick the direction.";
            arrangeSplitButton.OfficeImageId = "Refresh";
            arrangeSplitButton.Click += new RibbonControlEventHandler(this.ArrangeLeftRight);

            // Lower arrow — dropdown with both directions.
            RibbonButton arrangeTopDownItem = this.Factory.CreateRibbonButton();
            arrangeTopDownItem.Name = "arrangeTopDownItem";
            arrangeTopDownItem.Label = "Top-Down";
            arrangeTopDownItem.SuperTip = "States fall into layers downward; subjects line up in a column.";
            arrangeTopDownItem.Click += new RibbonControlEventHandler(this.ArrangeTopDown);
            arrangeSplitButton.Items.Add(arrangeTopDownItem);

            RibbonButton arrangeLeftRightItem = this.Factory.CreateRibbonButton();
            arrangeLeftRightItem.Name = "arrangeLeftRightItem";
            arrangeLeftRightItem.Label = "Left-Right";
            arrangeLeftRightItem.SuperTip = "States fall into layers rightward; subjects line up in a row.";
            arrangeLeftRightItem.Click += new RibbonControlEventHandler(this.ArrangeLeftRight);
            arrangeSplitButton.Items.Add(arrangeLeftRightItem);

            owlGroup.Items.Add(arrangeSplitButton);
        }

        /// <summary>
        /// Open file dialog and import OWL file.
        /// </summary>
        private void LoadOWLFile(object sender, RibbonControlEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "Ontology Files (.owl)|*.owl|RDF Files (*.rdf)|*.rdf"
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            // VSTO-Ribbon-Handler verschlucken unbehandelte Exceptions still, sodass ein
            // fehlgeschlagener Import wie "es passiert nichts" aussieht. Deshalb explizit fangen und
            // die Ursache samt InnerException-Kette sichtbar machen. Im catch bewusst KEIN Zugriff auf
            // OWLImporter-Mitglieder -- schluege dessen statische Init fehl, wuerde ein erneuter Zugriff
            // die TypeInitializationException nur wieder werfen.
            try
            {
                OWLImporter.Instance.Parse(dialog.FileName);
            }
            catch (Exception ex)
            {
                UI.ResultDialog.ShowError("OWL-Import fehlgeschlagen",
                    "Die gewählte Datei konnte nicht importiert werden.", DescribeException(ex));
            }
        }

        /// <summary>
        /// Baut eine lesbare Beschreibung einer Exception samt vollstaendiger InnerException-Kette
        /// und Stacktrace, damit die eigentliche Ursache (oft eine InnerException) sichtbar wird.
        /// </summary>
        private static string DescribeException(Exception ex)
        {
            var sb = new System.Text.StringBuilder();
            int depth = 0;
            for (Exception cur = ex; cur != null; cur = cur.InnerException, depth++)
            {
                string indent = new string(' ', depth * 2);
                sb.Append(indent);
                sb.Append(depth == 0 ? "" : "-> ");
                sb.AppendLine(cur.GetType().FullName + ": " + cur.Message);

                // Bei fehlenden/nicht ladbaren Abhaengigkeiten (typisch fuer ein unvollstaendiges
                // ClickOnce-Paket) steckt die eigentliche Ursache in den LoaderExceptions -- sie nennen
                // die konkret fehlende Assembly (z. B. Microsoft.ML, Newtonsoft.Json, ...).
                if (cur is System.Reflection.ReflectionTypeLoadException rtle && rtle.LoaderExceptions != null)
                {
                    foreach (Exception le in rtle.LoaderExceptions)
                    {
                        if (le == null) continue;
                        sb.AppendLine(indent + "   LoaderException: " + le.GetType().FullName + ": " + le.Message);
                    }
                }

                // Stacktrace JEDER Ebene ausgeben. Die eigentliche Fehlerstelle steckt im Stacktrace
                // der INNERSTEN Exception -- eine TypeInitializationException zeigt sonst nur den
                // ausloesenden Zugriff, nicht die Zeile, die tatsaechlich wirft.
                if (!string.IsNullOrEmpty(cur.StackTrace))
                {
                    sb.AppendLine(indent + "   Stacktrace:");
                    foreach (string line in cur.StackTrace.Split('\n'))
                        sb.AppendLine(indent + "     " + line.TrimEnd());
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Open ALPS stencils.
        /// </summary>
        private void OpenStencils(object sender, RibbonControlEventArgs e)
        {
            VisioHelper.openStencil(VisioHelper.VisioStencils.SID_STENCIL);
        }

        /// <summary>
        /// Show the layer explorer.
        /// </summary>
        private void ShowLayerExplorer(object sender, RibbonControlEventArgs e)
        {
            Globals.ThisAddIn.showDirectoryClicked();
        }

        /// <summary>
        /// ALPS Verification: picks a Specification (abstract) and an Implementation OWL model and
        /// checks whether the implementation adheres to the specification's SID rules. Ported 1:1 from
        /// the KIT master-thesis prototype (andikra/ALPS-Verification-Thesis) — a limited set of SID
        /// checks with raw textual output.
        /// </summary>
        /// <summary>Standardaktion: aktuelles Modell als Implementierung gegen eine Spezifikations-Datei prüfen.</summary>
        private void VerifyCurrentModel(object sender, RibbonControlEventArgs e)
        {
            if (!VisioPassModelBuilder.CanBuildFromActiveDocument(Globals.ThisAddIn.Application))
            {
                UI.ResultDialog.ShowWarning(
                    "Kein ALPS/PASS-Modell geöffnet",
                    "Das aktive Dokument trägt kein Modell (keine SID-Seite mit Modell-URI).",
                    "Über den Pfeil des Buttons lassen sich stattdessen zwei OWL-Dateien prüfen.");
                return;
            }

            string specPath = PickOwlFile("Spezifikation wählen (abstraktes Modell)");
            if (specPath == null) return;

            try
            {
                var builder = new VisioPassModelBuilder();
                var implModel = builder.BuildFromActiveDocument(Globals.ThisAddIn.Application);

                string report = Verification.Verifier.Verify(specPath, implModel);
                report = builder.DescribeSummary()
                    + (builder.Warnings.Count > 0
                        ? "\nHinweise beim Lesen des aktuellen Modells:\n- " + string.Join("\n- ", builder.Warnings)
                        : "")
                    + "\n\n" + report;
                ShowVerificationReport(report);
            }
            catch (Exception ex)
            {
                UI.ResultDialog.ShowError("Verifikation fehlgeschlagen",
                    "Bei der ALPS-Verifikation ist ein Fehler aufgetreten.", DescribeException(ex));
            }
        }

        /// <summary>
        /// Zeigt einen Verifikations-Report im einheitlichen Ergebnisdialog. Titel, Statusfarbe und
        /// Untertitel werden aus dem VERDICT am Report-Ende abgeleitet; der vollständige Report bleibt
        /// als Detailtext erhalten.
        /// </summary>
        private static void ShowVerificationReport(string report)
        {
            UI.ResultStatus status;
            string title, subtitle;
            if (report.IndexOf("VERDICT: BESTANDEN", StringComparison.Ordinal) >= 0)
            {
                status = UI.ResultStatus.Success;
                title = "Verifikation bestanden";
                subtitle = "Die Implementierung erfüllt alle geprüften SID-Regeln.";
            }
            else if (report.IndexOf("VERDICT: NICHT BESTANDEN", StringComparison.Ordinal) >= 0)
            {
                status = UI.ResultStatus.Warning;
                title = "Verifikation: nicht bestanden";
                subtitle = "Nicht alle geprüften SID-Regeln sind erfüllt — Details unten.";
            }
            else
            {
                status = UI.ResultStatus.Error;
                title = "Verifikation nicht durchführbar";
                subtitle = "Die Prüfung konnte nicht abgeschlossen werden — Details unten.";
            }
            new UI.ResultDialog(status, title, subtitle, report, bodyIsReport: true).ShowDialog();
        }

        /// <summary>Datei-Variante (Dropdown): Spezifikation und Implementierung als OWL-Dateien wählen.</summary>
        private void AlpsVerification(object sender, RibbonControlEventArgs e)
        {
            string specPath = PickOwlFile("Spezifikation wählen (abstraktes Modell)");
            if (specPath == null) return;
            string implPath = PickOwlFile("Implementierung wählen (implementierendes Modell)");
            if (implPath == null) return;

            try
            {
                string report = Verification.Verifier.Verify(specPath, implPath);
                ShowVerificationReport(report);
            }
            catch (Exception ex)
            {
                UI.ResultDialog.ShowError("Verifikation fehlgeschlagen",
                    "Bei der ALPS-Verifikation ist ein Fehler aufgetreten.", DescribeException(ex));
            }
        }

        /// <summary>
        /// Umkehrung der Verifikation: erzeugt aus einer abstrakten Spezifikation (OWL) ein
        /// implementierendes Modell in Visio — Subjekte mit implements-Verweisen und leeren
        /// SBD-Seiten, dazu die Nachrichten-Struktur (siehe ImplementationScaffolder).
        /// </summary>
        private void ScaffoldImplementationFromSpec(object sender, RibbonControlEventArgs e)
        {
            string specPath = PickOwlFile("Spezifikation wählen (abstraktes Modell)");
            if (specPath == null) return;

            try
            {
                System.Windows.Forms.Cursor.Current = Cursors.WaitCursor;
                var scaffolder = new Verification.ImplementationScaffolder();
                scaffolder.ScaffoldFromSpec(specPath, Globals.ThisAddIn.Application);

                string subtitle = scaffolder.SubjectCount + " Subjekte und " + scaffolder.MessageCount +
                    " Nachrichten aus der Spezifikation übernommen — implements-Verweise sind gesetzt.";
                if (scaffolder.Notes.Count > 0)
                    new UI.ResultDialog(UI.ResultStatus.Warning,
                        "Implementierungs-Modell erzeugt – mit Hinweisen", subtitle,
                        "Hinweise:\n• " + string.Join("\n• ", scaffolder.Notes), bodyIsReport: false).ShowDialog();
                else
                    UI.ResultDialog.ShowSuccess("Implementierungs-Modell erzeugt", subtitle,
                        "Nächste Schritte: Verhalten in den (leeren) SBD-Seiten modellieren, dann über " +
                        "„ALPS Verification“ gegen die Spezifikation prüfen.");
            }
            catch (Exception ex)
            {
                UI.ResultDialog.ShowError("Erzeugen fehlgeschlagen",
                    "Aus der Spezifikation konnte kein Implementierungs-Modell erzeugt werden.",
                    DescribeException(ex));
            }
            finally
            {
                System.Windows.Forms.Cursor.Current = Cursors.Default;
            }
        }

        /// <summary>Opens a file dialog for an OWL/RDF model file; returns the path or null if cancelled.</summary>
        private static string PickOwlFile(string title)
        {
            using (var dialog = new OpenFileDialog
            {
                Title = title,
                Filter = "Ontology Files (*.owl)|*.owl|RDF Files (*.rdf)|*.rdf|All Files (*.*)|*.*"
            })
            {
                return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
            }
        }


        /// <summary>
        /// Konvertiert ein PASS-Modell (OWL-Datei) in ein BPMN-2.0-Modell und speichert es als
        /// .bpmn-Datei (portierter pass-bpmn-converter, siehe BpmnConverter/). Die Warnungen des
        /// Konverters (Console.WriteLine im Original) werden eingefangen und mit angezeigt.
        /// </summary>
        /// <summary>Standardaktion: das aktuell geöffnete Modell direkt (in-memory) nach BPMN konvertieren.</summary>
        private void ConvertCurrentModelToBpmn(object sender, RibbonControlEventArgs e)
        {
            if (!VisioPassModelBuilder.CanBuildFromActiveDocument(Globals.ThisAddIn.Application))
            {
                UI.ResultDialog.ShowWarning(
                    "Kein ALPS/PASS-Modell geöffnet",
                    "Das aktive Dokument trägt kein Modell (keine SID-Seite mit Modell-URI).",
                    "Über den Pfeil des Buttons lässt sich stattdessen eine OWL-Datei konvertieren.");
                return;
            }

            try
            {
                var builder = new VisioPassModelBuilder();
                var passModel = builder.BuildFromActiveDocument(Globals.ThisAddIn.Application);
                RunBpmnConversion(passModel, builder.ModelName, builder.Warnings);
            }
            catch (Exception ex)
            {
                UI.ResultDialog.ShowError("BPMN-Konvertierung fehlgeschlagen",
                    "Das aktuelle Modell konnte nicht nach BPMN konvertiert werden.", DescribeException(ex));
            }
        }

        /// <summary>Datei-Variante (Dropdown): eine PASS-OWL-Datei nach BPMN konvertieren.</summary>
        private void ConvertOwlFileToBpmn(object sender, RibbonControlEventArgs e)
        {
            string inputPath = PickOwlFile("PASS-Modell (OWL) wählen");
            if (inputPath == null) return;

            try
            {
                var models = PassBpmnConverter.Pass.PassParser.LoadModels(
                    new System.Collections.Generic.List<string> { inputPath });
                if (models == null || models.Count < 1)
                    throw new Exception("Aus der gewählten Datei konnte kein PASS-Modell geladen werden.");

                RunBpmnConversion(models[0], System.IO.Path.GetFileNameWithoutExtension(inputPath), null);
            }
            catch (Exception ex)
            {
                UI.ResultDialog.ShowError("BPMN-Konvertierung fehlgeschlagen",
                    "Die gewählte OWL-Datei konnte nicht nach BPMN konvertiert werden.", DescribeException(ex));
            }
        }

        /// <summary>
        /// Dropdown-Variante: das aktuell geoeffnete Modell konvertieren und statt als
        /// .bpmn-Datei zu speichern direkt mit den Visio-BPMN-Shapes auf einem neuen
        /// Zeichenblatt darstellen (<see cref="BpmnVisioRenderer"/>).
        /// </summary>
        private void ShowCurrentModelAsBpmnPage(object sender, RibbonControlEventArgs e)
        {
            if (!VisioPassModelBuilder.CanBuildFromActiveDocument(Globals.ThisAddIn.Application))
            {
                UI.ResultDialog.ShowWarning(
                    "Kein ALPS/PASS-Modell geöffnet",
                    "Das aktive Dokument trägt kein Modell (keine SID-Seite mit Modell-URI).",
                    "Die BPMN-Anzeige braucht das aktuell geöffnete Modell als Quelle.");
                return;
            }

            // Konverter-Warnungen laufen wie bei RunBpmnConversion ueber die
            // umgeleitete Konsole und werden mit Builder- und Renderer-Hinweisen
            // im Ergebnisdialog gebuendelt.
            var consoleBuffer = new System.IO.StringWriter();
            System.IO.TextWriter originalOut = Console.Out;
            try
            {
                System.Windows.Forms.Cursor.Current = Cursors.WaitCursor;
                Console.SetOut(consoleBuffer);

                var builder = new VisioPassModelBuilder();
                var passModel = builder.BuildFromActiveDocument(Globals.ThisAddIn.Application);
                var bpmnModel = PassBpmnConverter.Conversion.Converter.ConvertPassToBpmn(passModel);
                PassBpmnConverter.Bpmn.BpmnDiagramGenerator.GenerateDiagram(bpmnModel);

                var warnings = new System.Collections.Generic.List<string>();
                if (builder.Warnings != null)
                    warnings.AddRange(builder.Warnings);
                warnings.AddRange(
                    BpmnVisioRenderer.Render(Globals.ThisAddIn.Application, bpmnModel, builder.ModelName));
                warnings.AddRange(consoleBuffer.ToString()
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(line => line.StartsWith("Warning:") || line.StartsWith("Error:")));

                if (warnings.Count > 0)
                    new UI.ResultDialog(UI.ResultStatus.Warning,
                        "BPMN-Zeichenblatt erstellt – mit Hinweisen",
                        "Das Modell wurde als BPMN gezeichnet. Einige Elemente ließen sich nicht vollständig übernehmen.",
                        "Hinweise:\n• " + string.Join("\n• ", warnings), bodyIsReport: false).ShowDialog();
                else
                    UI.ResultDialog.ShowSuccess("BPMN-Zeichenblatt erstellt",
                        "Das aktuelle Modell wurde als BPMN-Diagramm auf einem neuen Zeichenblatt dargestellt.");
            }
            catch (Exception ex)
            {
                UI.ResultDialog.ShowError("BPMN-Anzeige fehlgeschlagen",
                    "Das Modell konnte nicht als BPMN-Zeichenblatt dargestellt werden.", DescribeException(ex));
            }
            finally
            {
                Console.SetOut(originalOut);
                System.Windows.Forms.Cursor.Current = Cursors.Default;
            }
        }

        /// <summary>
        /// Dropdown-Variante: eine BPMN-2.0-Datei (bpmn.io, Camunda, eigener Export)
        /// einlesen und als BPMN-Zeichenblatt darstellen. Bringt die Datei kein
        /// BPMN-DI-Layout mit, erzeugt der <see cref="PassBpmnConverter.Bpmn.BpmnDiagramGenerator"/>
        /// die Anordnung automatisch.
        /// </summary>
        private void ShowBpmnFileAsPage(object sender, RibbonControlEventArgs e)
        {
            string inputPath;
            using (var openDialog = new OpenFileDialog
            {
                Title = "BPMN-Datei wählen",
                Filter = "BPMN Files (*.bpmn;*.xml)|*.bpmn;*.xml|Alle Dateien (*.*)|*.*"
            })
            {
                if (openDialog.ShowDialog() != DialogResult.OK) return;
                inputPath = openDialog.FileName;
            }

            try
            {
                System.Windows.Forms.Cursor.Current = Cursors.WaitCursor;

                var warnings = new System.Collections.Generic.List<string>();
                var bpmnModel = PassBpmnConverter.Bpmn.Serialization.BpmnDeserializer.Deserialize(inputPath, warnings);

                // Datei ohne Diagramm-Teil: Layout mit dem vorhandenen Generator erzeugen.
                if (bpmnModel.Definitions == null || bpmnModel.Definitions.Diagrams.Count == 0)
                {
                    PassBpmnConverter.Bpmn.BpmnDiagramGenerator.GenerateDiagram(bpmnModel);
                    warnings.Add("Die Datei enthielt kein Diagramm-Layout (BPMN DI) — die Anordnung wurde automatisch erzeugt.");
                }

                // Ohne offenes Dokument gibt es kein Ziel-Zeichenblatt — dann eine neue Zeichnung anlegen.
                if (Globals.ThisAddIn.Application.Documents.Count == 0)
                    Globals.ThisAddIn.Application.Documents.Add("");

                warnings.AddRange(BpmnVisioRenderer.Render(Globals.ThisAddIn.Application, bpmnModel,
                    System.IO.Path.GetFileNameWithoutExtension(inputPath)));

                if (warnings.Count > 0)
                    new UI.ResultDialog(UI.ResultStatus.Warning,
                        "BPMN-Datei dargestellt – mit Hinweisen",
                        System.IO.Path.GetFileName(inputPath) + " wurde gezeichnet. Einige Elemente ließen sich nicht vollständig übernehmen.",
                        "Hinweise:\n• " + string.Join("\n• ", warnings), bodyIsReport: false).ShowDialog();
                else
                    UI.ResultDialog.ShowSuccess("BPMN-Datei dargestellt",
                        System.IO.Path.GetFileName(inputPath) + " wurde auf einem neuen Zeichenblatt dargestellt.");
            }
            catch (Exception ex)
            {
                UI.ResultDialog.ShowError("BPMN-Import fehlgeschlagen",
                    "Die BPMN-Datei konnte nicht dargestellt werden.", DescribeException(ex));
            }
            finally
            {
                System.Windows.Forms.Cursor.Current = Cursors.Default;
            }
        }

        /// <summary>
        /// Ergaenzt am Ergebnisdialog einen „Ordner öffnen"-Button, der den Explorer oeffnet und die
        /// erzeugte Datei markiert. Fehlschlaege (Pfad weg, Explorer nicht verfuegbar) werden still
        /// ignoriert — der Button ist ein Komfort, kein Muss.
        /// </summary>
        private static void AddOpenFolderButton(UI.ResultDialog dialog, string filePath)
        {
            dialog.AddActionButton("Ordner öffnen", () =>
            {
                try { System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + filePath + "\""); }
                catch { /* Komfortfunktion — Fehler bewusst schlucken */ }
            });
        }

        /// <summary>
        /// Gemeinsamer Konvertierungskern: Ziel-Datei erfragen, Konverter + Layouter +
        /// Serializer laufen lassen, Warnungen (Konverter-Konsole + optional Builder)
        /// im Ergebnisdialog buendeln.
        /// </summary>
        private static void RunBpmnConversion(alps.net.api.StandardPASS.IPASSProcessModel passModel,
            string defaultFileName, System.Collections.Generic.IList<string> builderWarnings)
        {
            string outputPath;
            using (var saveDialog = new SaveFileDialog
            {
                Title = "BPMN-Ausgabedatei wählen",
                Filter = "BPMN Files (*.bpmn)|*.bpmn",
                DefaultExt = "bpmn",
                FileName = (string.IsNullOrWhiteSpace(defaultFileName) ? "model" : defaultFileName) + ".bpmn"
            })
            {
                if (saveDialog.ShowDialog() != DialogResult.OK) return;
                outputPath = saveDialog.FileName;
            }

            // Der portierte Konverter meldet Warnungen per Console.WriteLine — im Visio-Host
            // gibt es keine Konsole, deshalb umleiten und im Ergebnisdialog mit ausgeben.
            var consoleBuffer = new System.IO.StringWriter();
            System.IO.TextWriter originalOut = Console.Out;
            try
            {
                System.Windows.Forms.Cursor.Current = Cursors.WaitCursor;
                Console.SetOut(consoleBuffer);

                var bpmnModel = PassBpmnConverter.Conversion.Converter.ConvertPassToBpmn(passModel);
                PassBpmnConverter.Bpmn.BpmnDiagramGenerator.GenerateDiagram(bpmnModel);
                PassBpmnConverter.Bpmn.BpmnSerializer.Serialize(bpmnModel, outputPath);

                // Nur echte Konverter-Hinweise anzeigen ("Warning:"/"Error:"-Praefix),
                // nicht das Parser-Grundrauschen von alps.net.api.
                var warnings = new System.Collections.Generic.List<string>();
                if (builderWarnings != null)
                    warnings.AddRange(builderWarnings);
                warnings.AddRange(consoleBuffer.ToString()
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(line => line.StartsWith("Warning:") || line.StartsWith("Error:")));

                // Kurzer, freundlicher Erfolgs-Header; die (optionalen) Hinweise erscheinen nur,
                // wenn es welche gibt — dann als Warnung, sonst reine Erfolgsmeldung.
                string fileName = System.IO.Path.GetFileName(outputPath);
                UI.ResultDialog dialog = warnings.Count > 0
                    ? new UI.ResultDialog(UI.ResultStatus.Warning,
                        "BPMN-Modell gespeichert – mit Hinweisen",
                        fileName + " wurde erstellt. Einige Elemente ließen sich nicht vollständig übernehmen.",
                        "Nicht (vollständig) konvertierbare Elemente:\n• " + string.Join("\n• ", warnings), bodyIsReport: false)
                    : new UI.ResultDialog(UI.ResultStatus.Success,
                        "BPMN-Modell gespeichert",
                        fileName + " wurde erfolgreich erstellt.",
                        outputPath, bodyIsReport: false);
                AddOpenFolderButton(dialog, outputPath);
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                string consoleText = consoleBuffer.ToString().Trim();
                string details = DescribeException(ex);
                if (consoleText.Length > 0)
                    details += "\n\nHinweise des Konverters:\n" + consoleText;
                UI.ResultDialog.ShowError("BPMN-Konvertierung fehlgeschlagen",
                    "Das Modell konnte nicht nach BPMN konvertiert werden.", details);
            }
            finally
            {
                Console.SetOut(originalOut);
                System.Windows.Forms.Cursor.Current = Cursors.Default;
            }
        }

        /// <summary>
        /// PASS NL Checker: classifies each shape label as valid/invalid (ML.NET model trained from the
        /// bundled data) and asks an LLM for better labels where invalid. Ported from the standalone
        /// NLPPASSChecking add-in. Needs an LLM API key (prompted on first use).
        /// </summary>
        private async void PassNlChecker(object sender, RibbonControlEventArgs e)
        {
            try
            {
                var checker = new NLChecker.NlChecker();
                if (!checker.Initialize(out string error))
                {
                    UI.ResultDialog.ShowWarning("PASS NL Checker nicht bereit", null, error);
                    return;
                }

                var progress = new NLChecker.ProcessingForm();
                progress.Show();

                string report;
                try
                {
                    report = await checker.CheckActiveDocumentAsync(Globals.ThisAddIn.Application, progress);
                }
                finally
                {
                    progress.Close();
                }

                new UI.ResultDialog(UI.ResultStatus.Info, "NL-Prüfung – Ergebnis",
                    "Model Integrity Check des aktiven Dokuments", report, bodyIsReport: true).ShowDialog();
            }
            catch (Exception ex)
            {
                UI.ResultDialog.ShowError("PASS NL Checker fehlgeschlagen",
                    "Die Prüfung konnte nicht abgeschlossen werden.", DescribeException(ex));
            }
        }

        /// <summary>
        /// Trains the local NL-checker model from the bundled training data (replaces the
        /// cached model). Shows the FULL exception chain on failure -- the root cause
        /// (e.g. a missing native ML.NET library) hides in the inner exceptions.
        /// </summary>
        private void RetrainNlModel(object sender, RibbonControlEventArgs e)
        {
            try
            {
                System.Windows.Forms.Cursor.Current = Cursors.WaitCursor;
                new NLChecker.NlChecker().Retrain();
                UI.ResultDialog.ShowSuccess("NL-Modell trainiert",
                    "Das lokale Prüfmodell wurde neu trainiert und gespeichert.");
            }
            catch (Exception ex)
            {
                UI.ResultDialog.ShowError("Training fehlgeschlagen",
                    "Das NL-Modell konnte nicht trainiert werden.",
                    DescribeException(ex) + "\n\n--- Native-DLL-Suche ---\n" + NLChecker.NlChecker.NativeDiagnostics);
            }
            finally
            {
                System.Windows.Forms.Cursor.Current = Cursors.Default;
            }
        }

        /// <summary>
        /// Opens the NL-checker settings dialog: LLM provider (built-in
        /// UniGPT/OpenAI/Anthropic plus user-defined custom providers) and
        /// per-provider model + API key.
        /// </summary>
        private void OpenNlCheckerSettings(object sender, RibbonControlEventArgs e)
        {
            var settings = NLChecker.NlCheckerSettings.Load();
            using (var dialog = new NLChecker.NlCheckerSettingsDialog(settings))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                    UI.ResultDialog.ShowSuccess("Einstellungen gespeichert",
                        "Die NL-Checker-Einstellungen wurden übernommen.");
            }
        }

        /// <summary>
        /// Re-arrange the active page from its shapes, flowing top to bottom.
        /// </summary>
        private void ArrangeTopDown(object sender, RibbonControlEventArgs e)
        {
            AutoArranger.ArrangeActivePage(Globals.ThisAddIn.Application, AutoArranger.LayoutDirection.TopToBottom);
        }

        /// <summary>
        /// Re-arrange the active page from its shapes, flowing left to right.
        /// </summary>
        private void ArrangeLeftRight(object sender, RibbonControlEventArgs e)
        {
            AutoArranger.ArrangeActivePage(Globals.ThisAddIn.Application, AutoArranger.LayoutDirection.LeftToRight);
        }
    }
}
