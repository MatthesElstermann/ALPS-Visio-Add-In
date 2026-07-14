using System;
using System.Windows.Forms;

namespace ALPS_Visio_AddIn_rewrite.NLChecker
{
    /// <summary>
    /// Einstellungs-Dialog des PASS NL Checkers (Nachfolger des reinen API-Key-Dialogs):
    /// LLM-Provider (UniGPT/OpenAI/Anthropic) sowie Modell und API-Key je Provider fuer
    /// die Label-Vorschlaege. Die Gueltigkeitspruefung selbst laeuft immer ueber das
    /// lokale ML-Modell. DPI-fest per AutoSize-Layout-Containern.
    /// </summary>
    public class NlCheckerSettingsDialog : Form
    {
        private readonly NlCheckerSettings _settings;

        private ComboBox cmbProvider;
        private ComboBox cmbModel;
        private Button btnLoadModels;
        private TextBox txtApiKey;
        private Button btnOK;
        private Button btnCancel;

        /// <summary>Provider, dessen Werte gerade in den Textfeldern angezeigt werden.</summary>
        private string _shownProvider;

        private static readonly string[] ProviderIds =
        {
            NlCheckerSettings.ProviderUniGpt,
            NlCheckerSettings.ProviderOpenAi,
            NlCheckerSettings.ProviderAnthropic,
        };

        private static readonly string[] ProviderLabels =
        {
            "UniGPT (Uni Münster)",
            "OpenAI",
            "Anthropic",
        };

        public NlCheckerSettingsDialog(NlCheckerSettings settings)
        {
            _settings = settings;
            InitializeComponent();

            int providerIndex = Array.IndexOf(ProviderIds, settings.Provider);
            cmbProvider.SelectedIndex = providerIndex >= 0 ? providerIndex : 0;
            ShowProvider(ProviderIds[cmbProvider.SelectedIndex]);
        }

        /// <summary>Feld-Werte in die Settings des aktuell angezeigten Providers sichern.</summary>
        private void StoreShownProvider()
        {
            if (_shownProvider == null) return;
            _settings.SetApiKey(_shownProvider, txtApiKey.Text.Trim());
            _settings.SetModel(_shownProvider, cmbModel.Text.Trim());
        }

        private void ShowProvider(string provider)
        {
            _shownProvider = provider;
            txtApiKey.Text = _settings.GetApiKey(provider);

            // Statische Vorschlaege als Startpunkt; "Abrufen" ersetzt sie durch die
            // tatsaechlich verfuegbaren Modelle des Providers. Die ComboBox bleibt
            // editierbar, damit auch ungelistete Modellnamen eingetragen werden koennen.
            cmbModel.Items.Clear();
            cmbModel.Items.AddRange(NlCheckerSettings.SuggestedModelsFor(provider));
            cmbModel.Text = _settings.GetModel(provider);
        }

        private void cmbProvider_SelectedIndexChanged(object sender, EventArgs e)
        {
            StoreShownProvider();
            ShowProvider(ProviderIds[cmbProvider.SelectedIndex]);
        }

        /// <summary>
        /// Holt die verfuegbaren Modelle des angezeigten Providers ueber dessen
        /// /v1/models-Endpoint (braucht den eingetragenen API-Key) und fuellt die Liste.
        /// </summary>
        private async void btnLoadModels_Click(object sender, EventArgs e)
        {
            string provider = _shownProvider;
            string apiKey = txtApiKey.Text.Trim();

            btnLoadModels.Enabled = false;
            try
            {
                var models = await LlmClient.ListModelsAsync(provider, apiKey);

                // Provider koennte waehrend des Abrufs gewechselt worden sein.
                if (_shownProvider != provider) return;

                string current = cmbModel.Text;
                cmbModel.Items.Clear();
                cmbModel.Items.AddRange(System.Linq.Enumerable.ToArray(models));
                cmbModel.Text = current;
                cmbModel.DroppedDown = true;
            }
            catch (Exception ex)
            {
                // Ursachenkette ausgeben: bei Netzwerkfehlern steckt der eigentliche
                // Grund (DNS/Verbindung/TLS) in den InnerExceptions.
                var messages = new System.Text.StringBuilder();
                for (Exception inner = ex; inner != null; inner = inner.InnerException)
                    messages.AppendLine(inner.Message);
                MessageBox.Show("Modelle konnten nicht abgerufen werden:\n\n" + messages,
                    "PASS NL Checker", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                btnLoadModels.Enabled = true;
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            StoreShownProvider();
            _settings.Provider = ProviderIds[cmbProvider.SelectedIndex];
            _settings.Save();

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void InitializeComponent()
        {
            Label lblProvider = MakeLabel("Provider:");
            Label lblModel = MakeLabel("Modell:");
            Label lblApiKey = MakeLabel("API-Key:");

            cmbProvider = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
            };
            cmbProvider.Items.AddRange(ProviderLabels);
            cmbProvider.SelectedIndexChanged += cmbProvider_SelectedIndexChanged;

            // Editierbare Dropdown-Liste: Vorschlaege/abgerufene Modelle waehlbar,
            // freie Eingabe fuer ungelistete Modellnamen bleibt moeglich.
            cmbModel = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDown,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                MinimumSize = new System.Drawing.Size(200, 0),
            };

            btnLoadModels = new Button
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(6, 1, 6, 1),
                Margin = new Padding(6, 0, 0, 0),
                Text = "Abrufen",
                UseVisualStyleBackColor = true,
            };
            btnLoadModels.Click += btnLoadModels_Click;

            TableLayoutPanel modelRow = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0),
            };
            modelRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            modelRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            modelRow.Controls.Add(cmbModel, 0, 0);
            modelRow.Controls.Add(btnLoadModels, 1, 0);

            txtApiKey = new TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                MinimumSize = new System.Drawing.Size(280, 0),
            };

            Label lblHint = new Label
            {
                AutoSize = true,
                MaximumSize = new System.Drawing.Size(420, 0),
                ForeColor = System.Drawing.SystemColors.GrayText,
                Margin = new Padding(0, 8, 0, 0),
                Text = "Geprüft wird immer mit dem lokalen ML-Modell (offline). Der API-Key des " +
                       "gewählten Providers wird nur für die Label-Vorschläge zu ungültigen Namen " +
                       "benötigt. Modell und Key werden je Provider gespeichert. " +
                       "„Abrufen“ lädt die beim Provider verfügbaren Modelle (API-Key nötig).",
            };

            btnOK = MakeButton("OK");
            btnOK.Click += btnOK_Click;
            btnCancel = MakeButton("Abbrechen");
            btnCancel.DialogResult = DialogResult.Cancel;

            FlowLayoutPanel buttonRow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0, 12, 0, 0),
                WrapContents = false,
            };
            buttonRow.Controls.Add(btnCancel);
            buttonRow.Controls.Add(btnOK);

            TableLayoutPanel layout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 5,
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            layout.Controls.Add(lblProvider, 0, 0);
            layout.Controls.Add(cmbProvider, 1, 0);
            layout.Controls.Add(lblModel, 0, 1);
            layout.Controls.Add(modelRow, 1, 1);
            layout.Controls.Add(lblApiKey, 0, 2);
            layout.Controls.Add(txtApiKey, 1, 2);
            layout.Controls.Add(lblHint, 1, 3);
            layout.Controls.Add(buttonRow, 1, 4);

            this.AutoScaleMode = AutoScaleMode.Font;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
            this.Controls.Add(layout);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "NlCheckerSettingsDialog";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "PASS NL Checker – Einstellungen";
        }

        private static Label MakeLabel(string text)
        {
            return new Label
            {
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 6, 8, 0),
                Text = text,
            };
        }

        private static Button MakeButton(string text)
        {
            return new Button
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new System.Drawing.Size(88, 28),
                Padding = new Padding(10, 2, 10, 2),
                Text = text,
                UseVisualStyleBackColor = true,
            };
        }
    }
}
