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
                MessageBox.Show(
                    "Der OWL-Import ist fehlgeschlagen:\n\n" + DescribeException(ex),
                    "OWL-Import fehlgeschlagen",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
                MessageBox.Show(
                    "Das aktuell geöffnete Dokument enthält kein ALPS/PASS-Modell (keine SID-Seite mit Modell-URI).\n" +
                    "Über den Pfeil des Buttons lassen sich stattdessen zwei OWL-Dateien prüfen.",
                    "ALPS Verification", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string specPath = PickOwlFile("Spezifikation wählen (abstraktes Modell)");
            if (specPath == null) return;

            try
            {
                var builder = new VisioPassModelBuilder();
                var implModel = builder.BuildFromActiveDocument(Globals.ThisAddIn.Application);

                string report = Verification.Verifier.Verify(specPath, implModel);
                if (builder.Warnings.Count > 0)
                    report = "Hinweise beim Lesen des aktuellen Modells:\n- "
                        + string.Join("\n- ", builder.Warnings) + "\n\n" + report;
                new Verification.VerificationResultsForm(report).ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler bei der ALPS Verification:\n" + DescribeException(ex), "ALPS Verification",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                new Verification.VerificationResultsForm(report).ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler bei der ALPS Verification:\n" + ex.Message, "ALPS Verification",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show(
                    "Das aktuell geöffnete Dokument enthält kein ALPS/PASS-Modell (keine SID-Seite mit Modell-URI).\n" +
                    "Über den Pfeil des Buttons lässt sich stattdessen eine OWL-Datei konvertieren.",
                    "PASS BPMN Converter", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                MessageBox.Show("Die BPMN-Konvertierung ist fehlgeschlagen:\n\n" + DescribeException(ex),
                    "PASS BPMN Converter", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show("Die BPMN-Konvertierung ist fehlgeschlagen:\n\n" + DescribeException(ex),
                    "PASS BPMN Converter", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

                string message = "BPMN-Modell erfolgreich gespeichert:\n" + outputPath;
                if (warnings.Count > 0)
                    message += "\n\nNicht (vollständig) konvertierbare Elemente:\n" + string.Join("\n", warnings);
                MessageBox.Show(message, "PASS BPMN Converter", MessageBoxButtons.OK,
                    warnings.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                string consoleText = consoleBuffer.ToString().Trim();
                string message = "Die BPMN-Konvertierung ist fehlgeschlagen:\n\n" + DescribeException(ex);
                if (consoleText.Length > 0)
                    message += "\n\nHinweise des Konverters:\n" + consoleText;
                MessageBox.Show(message, "PASS BPMN Converter", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    MessageBox.Show(error, "PASS NL Checker", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

                new NLChecker.ValidityCheckResultsForm(report).ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler im PASS NL Checker:\n" + ex.Message, "PASS NL Checker",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show("NL-Modell erfolgreich trainiert und gespeichert.", "PASS NL Checker",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Training des NL-Modells fehlgeschlagen:\n\n" + ex
                        + "\n\n--- Native-DLL-Suche ---\n" + NLChecker.NlChecker.NativeDiagnostics,
                    "PASS NL Checker", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                System.Windows.Forms.Cursor.Current = Cursors.Default;
            }
        }

        /// <summary>
        /// Opens the NL-checker settings dialog: check method (local ML model vs. LLM),
        /// LLM provider (UniGPT/OpenAI/Anthropic) and per-provider model + API key.
        /// </summary>
        private void OpenNlCheckerSettings(object sender, RibbonControlEventArgs e)
        {
            var settings = NLChecker.NlCheckerSettings.Load();
            using (var dialog = new NLChecker.NlCheckerSettingsDialog(settings))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                    MessageBox.Show("NL-Checker-Einstellungen gespeichert.", "PASS NL Checker",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
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
