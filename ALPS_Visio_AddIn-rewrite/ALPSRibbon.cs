using Microsoft.Office.Tools.Ribbon;
using Microsoft.Office.Core;
using System;
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

            // Group order and labels mirror the original add-in (upstream/main).

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

            // Carried over from the original add-in; not implemented yet (stub).
            RibbonButton verificationButton = this.Factory.CreateRibbonButton();
            verificationButton.Name = "verificationButton";
            verificationButton.Label = "ALPS Verification";
            verificationButton.SuperTip = "Open the verification tool to check if a given model adheres to a given specification (abstract) model.";
            verificationButton.OfficeImageId = "AdpDiagramArrangeTables";
            verificationButton.ShowImage = true;
            verificationButton.ControlSize = RibbonControlSize.RibbonControlSizeLarge;
            verificationButton.Click += new RibbonControlEventHandler(this.AlpsVerification);
            owlGroup.Items.Add(verificationButton);

            // PASS NL Checker (ML.NET label validity + LLM suggestions), ported from NLPPASSChecking.
            RibbonButton naturalLanguageButton = this.Factory.CreateRibbonButton();
            naturalLanguageButton.Name = "naturalLanguageButton";
            naturalLanguageButton.Label = "PASS NL Checker";
            naturalLanguageButton.SuperTip = "Check every shape label for its type (ML) and get LLM suggestions for weak labels.";
            naturalLanguageButton.OfficeImageId = "Spelling";
            naturalLanguageButton.ShowImage = true;
            naturalLanguageButton.ControlSize = RibbonControlSize.RibbonControlSizeLarge;
            naturalLanguageButton.Click += new RibbonControlEventHandler(this.PassNlChecker);
            owlGroup.Items.Add(naturalLanguageButton);

            // Set/replace the LLM API key used by the PASS NL Checker (stored under %APPDATA%).
            RibbonButton apiKeyButton = this.Factory.CreateRibbonButton();
            apiKeyButton.Name = "apiKeyButton";
            apiKeyButton.Label = "LLM API-Key";
            apiKeyButton.SuperTip = "Set or replace the LLM API key used by the PASS NL Checker for label suggestions.";
            apiKeyButton.OfficeImageId = "Lock";
            apiKeyButton.ShowImage = true;
            apiKeyButton.ControlSize = RibbonControlSize.RibbonControlSizeLarge;
            apiKeyButton.Click += new RibbonControlEventHandler(this.SetApiKey);
            owlGroup.Items.Add(apiKeyButton);

            // Carried over from the original add-in; not implemented yet (stub).
            RibbonButton bpmnButton = this.Factory.CreateRibbonButton();
            bpmnButton.Name = "bpmnButton";
            bpmnButton.Label = "PASS BPMN Converter";
            bpmnButton.SuperTip = "Convert between PASS and BPMN process models.";
            bpmnButton.OfficeImageId = "FileSaveAsOtherFormats";
            bpmnButton.ShowImage = true;
            bpmnButton.ControlSize = RibbonControlSize.RibbonControlSizeLarge;
            bpmnButton.Click += new RibbonControlEventHandler(this.NotImplemented);
            owlGroup.Items.Add(bpmnButton);

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

            // Log-Pfad LOKAL berechnen -- OHNE OWLImporter zu beruehren. Jeder Zugriff auf ein
            // statisches Mitglied von OWLImporter (auch nur DiagLogPath) loest dessen statische
            // Initialisierung aus: das Feld "Instance = new OWLImporter()", dessen Konstruktor die
            // Ontologien laedt und ueber alle Typen der Assembly reflektiert. Genau das war der
            // Regress in v4: der Marker verwies auf OWLImporter.DiagLogPath -> die (vermutlich
            // fehlschlagende) statische Init lief VOR der Box -> gar keine Box mehr.
            string logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "alps_import_diag.log");
            try { System.IO.File.WriteAllText(logPath, "=== IMPORT-DIAGNOSE v5 === " + System.DateTime.Now + System.Environment.NewLine); }
            catch { }
            DiagLog(logPath, "Ribbon: Datei gewaehlt = " + dialog.FileName);

            MessageBox.Show(
                "=== IMPORT-DIAGNOSE v5 ===\n\n" +
                "Gewaehlte Datei:\n" + dialog.FileName + "\n\n" +
                "Schritt-Log (Zeile fuer Zeile, ueberlebt Haenger/Absturz):\n" + logPath + "\n\n" +
                "Falls nach dieser Box keine weitere Meldung kommt: diese Datei im Editor oeffnen\n" +
                "und Inhalt hierher kopieren -- die letzte Zeile zeigt, wo es haengt.\n\n" +
                "Geladene Add-In-DLL:\n" + System.Reflection.Assembly.GetExecutingAssembly().Location,
                "OWL-Import — Diagnose (Start)",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            // WICHTIG: Der ERSTE Zugriff auf OWLImporter.Instance loest die statische Initialisierung
            // aus (Konstruktor: Ontologie-Laden via loadOWLParsingStructure + Reflexion ueber alle Typen
            // der Assembly). Das ist der wahrscheinliche Fehlerpunkt -- z. B. eine im ClickOnce-Paket
            // fehlende Abhaengigkeit (Microsoft.ML.* etc.) -> ReflectionTypeLoadException, oder ein
            // Fehler in loadOWLParsingStructure. Er wird separat geloggt.
            //
            // Im catch darf KEIN OWLImporter-Mitglied mehr angefasst werden: schlaegt die statische
            // Init fehl, wirft jeder erneute Zugriff die TypeInitializationException erneut -- mitten
            // im catch -> verschluckt (genau das verhinderte in v3 die Fehler-Box).
            try
            {
                DiagLog(logPath, "vor OWLImporter.Instance (statische Init: Ontologie-Laden + Typ-Reflexion) ...");
                OWLImporter importer = OWLImporter.Instance;
                DiagLog(logPath, "OWLImporter.Instance OK -- rufe Parse ...");
                importer.Parse(dialog.FileName);
                DiagLog(logPath, "Parse zurueckgekehrt (ohne Exception).");
                MessageBox.Show(
                    "Import abgeschlossen (ohne Exception).\n\nLog-Datei:\n" + logPath,
                    "OWL-Import — Diagnose",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                string description = DescribeException(ex);
                DiagLog(logPath, "EXCEPTION:\n" + description);
                MessageBox.Show(
                    "Der OWL-Import ist fehlgeschlagen:\n\n" + description +
                    "\n\n(Vollstaendig auch in der Log-Datei:\n" + logPath + ")",
                    "OWL-Import fehlgeschlagen",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Schreibt eine Diagnosezeile SOFORT in die Logdatei -- bewusst unabhaengig von OWLImporter,
        /// damit auch ein Fehler/Haenger in dessen statischer Initialisierung noch protokolliert wird.
        /// </summary>
        private static void DiagLog(string path, string message)
        {
            try
            {
                System.IO.File.AppendAllText(path,
                    System.DateTime.Now.ToString("HH:mm:ss.fff") + "  [Ribbon] " + message + System.Environment.NewLine);
            }
            catch { }
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
            }
            sb.AppendLine();
            sb.AppendLine("Stacktrace:");
            sb.AppendLine(ex.StackTrace);
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
        /// Shared placeholder for ribbon buttons whose feature is not implemented yet.
        /// </summary>
        private void NotImplemented(object sender, RibbonControlEventArgs e)
        {
            MessageBox.Show("Diese Funktion ist noch nicht implementiert.", "ALPS/PASS Add-In",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        /// Opens the API-key dialog (pre-filled with the current key) so the LLM API key used by the
        /// PASS NL Checker can be set or replaced at any time.
        /// </summary>
        private void SetApiKey(object sender, RibbonControlEventArgs e)
        {
            if (NLChecker.ApiKeyManager.UpdateApiKey())
                MessageBox.Show("LLM API-Key gespeichert.", "PASS NL Checker",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
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
