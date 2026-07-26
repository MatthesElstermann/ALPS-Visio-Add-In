using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ALPS_Visio_AddIn_rewrite.NLChecker
{
    /// <summary>
    /// Einstellungs-Dialog des PASS NL Checkers (Nachfolger des reinen API-Key-Dialogs):
    /// LLM-Provider sowie Modell und API-Key je Provider fuer die Label-Vorschlaege.
    /// Neben den eingebauten Providern (UniGPT/OpenAI/Anthropic) kann der Nutzer ueber
    /// „Neu…“ eigene Provider anlegen (<see cref="CustomProviderDialog"/>), bearbeiten
    /// und entfernen; persistent wird alles erst mit OK. Die Gueltigkeitspruefung
    /// selbst laeuft immer ueber das lokale ML-Modell. DPI-fest per
    /// AutoSize-Layout-Containern.
    /// </summary>
    public class NlCheckerSettingsDialog : Form
    {
        private readonly NlCheckerSettings _settings;

        private ComboBox cmbProvider;
        private Button btnNewProvider;
        private Button btnEditProvider;
        private Button btnRemoveProvider;
        private ComboBox cmbModel;
        private Button btnLoadModels;
        private TextBox txtApiKey;
        private Button btnOK;
        private Button btnCancel;

        /// <summary>Provider, dessen Werte gerade in den Textfeldern angezeigt werden.</summary>
        private string _shownProvider;

        /// <summary>Provider-IDs in der Reihenfolge der ComboBox-Eintraege (eingebaute + eigene).</summary>
        private readonly List<string> _providerIds = new List<string>();

        public NlCheckerSettingsDialog(NlCheckerSettings settings)
        {
            _settings = settings;
            InitializeComponent();
            RebuildProviderList(settings.Provider);
        }

        /// <summary>
        /// Baut die Provider-ComboBox aus eingebauten und eigenen Providern neu auf und
        /// waehlt <paramref name="selectProvider"/> aus (Fallback: erster Eintrag). Das
        /// dabei ausgeloeste SelectedIndexChanged zeigt die Provider-Werte an.
        /// </summary>
        private void RebuildProviderList(string selectProvider)
        {
            _providerIds.Clear();
            cmbProvider.Items.Clear();

            _providerIds.Add(NlCheckerSettings.ProviderUniGpt);
            cmbProvider.Items.Add("UniGPT (Uni Münster)");
            _providerIds.Add(NlCheckerSettings.ProviderOpenAi);
            cmbProvider.Items.Add("OpenAI");
            _providerIds.Add(NlCheckerSettings.ProviderAnthropic);
            cmbProvider.Items.Add("Anthropic");

            foreach (CustomLlmProvider custom in _settings.CustomProviders)
            {
                _providerIds.Add(custom.Name);
                cmbProvider.Items.Add(custom.Name + " (eigener Provider)");
            }

            int index = _providerIds.FindIndex(
                id => string.Equals(id, selectProvider, StringComparison.OrdinalIgnoreCase));
            cmbProvider.SelectedIndex = index >= 0 ? index : 0;
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
            if (cmbProvider.SelectedIndex < 0) return;
            StoreShownProvider();
            ShowProvider(_providerIds[cmbProvider.SelectedIndex]);

            // Bearbeiten/Entfernen gibt es nur fuer eigene Provider.
            bool isCustom = _settings.FindCustomProvider(_shownProvider) != null;
            btnEditProvider.Enabled = isCustom;
            btnRemoveProvider.Enabled = isCustom;
        }

        private void btnNewProvider_Click(object sender, EventArgs e)
        {
            using (var dialog = new CustomProviderDialog(_settings, null))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                StoreShownProvider();
                _settings.CustomProviders.Add(dialog.Result);
                RebuildProviderList(dialog.Result.Name);
            }
        }

        private void btnEditProvider_Click(object sender, EventArgs e)
        {
            CustomLlmProvider custom = _settings.FindCustomProvider(_shownProvider);
            if (custom == null) return;

            using (var dialog = new CustomProviderDialog(_settings, custom))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                StoreShownProvider();
                string oldName = custom.Name;
                custom.Name = dialog.Result.Name;
                custom.ApiFormat = dialog.Result.ApiFormat;
                custom.ChatUrl = dialog.Result.ChatUrl;
                custom.ModelsUrl = dialog.Result.ModelsUrl;

                // Beim Umbenennen wandern gespeicherter API-Key und Modell mit.
                if (!string.Equals(oldName, custom.Name, StringComparison.Ordinal))
                    _settings.MoveProviderData(oldName, custom.Name);

                _shownProvider = null;
                RebuildProviderList(custom.Name);
            }
        }

        private void btnRemoveProvider_Click(object sender, EventArgs e)
        {
            CustomLlmProvider custom = _settings.FindCustomProvider(_shownProvider);
            if (custom == null) return;

            DialogResult confirm = MessageBox.Show(
                "Provider „" + custom.Name + "“ samt gespeichertem API-Key und Modell entfernen?\n" +
                "(Wird erst mit OK endgültig übernommen.)",
                "PASS NL Checker", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            // _shownProvider zuerst verwerfen, damit StoreShownProvider den geloeschten
            // Provider nicht ueber die Feld-Werte wieder in ApiKeys/Models anlegt.
            _shownProvider = null;
            _settings.RemoveCustomProvider(custom.Name);
            RebuildProviderList(NlCheckerSettings.ProviderUniGpt);
        }

        /// <summary>
        /// Holt die verfuegbaren Modelle des angezeigten Providers ueber dessen
        /// Models-Endpoint (eingebaute Provider brauchen den eingetragenen API-Key)
        /// und fuellt die Liste.
        /// </summary>
        private async void btnLoadModels_Click(object sender, EventArgs e)
        {
            string provider = _shownProvider;
            string apiKey = txtApiKey.Text.Trim();

            btnLoadModels.Enabled = false;
            try
            {
                var models = await LlmClient.ListModelsAsync(_settings, provider, apiKey);

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
            _settings.Provider = _providerIds[cmbProvider.SelectedIndex];
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
                MinimumSize = new System.Drawing.Size(200, 0),
            };
            cmbProvider.SelectedIndexChanged += cmbProvider_SelectedIndexChanged;

            btnNewProvider = MakeRowButton("Neu…");
            btnNewProvider.Click += btnNewProvider_Click;
            btnEditProvider = MakeRowButton("Bearbeiten…");
            btnEditProvider.Click += btnEditProvider_Click;
            btnRemoveProvider = MakeRowButton("Entfernen");
            btnRemoveProvider.Click += btnRemoveProvider_Click;

            TableLayoutPanel providerRow = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 4,
                RowCount = 1,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0),
            };
            providerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            providerRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            providerRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            providerRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            providerRow.Controls.Add(cmbProvider, 0, 0);
            providerRow.Controls.Add(btnNewProvider, 1, 0);
            providerRow.Controls.Add(btnEditProvider, 2, 0);
            providerRow.Controls.Add(btnRemoveProvider, 3, 0);

            // Editierbare Dropdown-Liste: Vorschlaege/abgerufene Modelle waehlbar,
            // freie Eingabe fuer ungelistete Modellnamen bleibt moeglich.
            cmbModel = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDown,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                MinimumSize = new System.Drawing.Size(200, 0),
            };

            btnLoadModels = MakeRowButton("Abrufen");
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
                MaximumSize = new System.Drawing.Size(460, 0),
                ForeColor = System.Drawing.SystemColors.GrayText,
                Margin = new Padding(0, 8, 0, 0),
                Text = "Geprüft wird immer mit dem lokalen ML-Modell (offline). Der API-Key des " +
                       "gewählten Providers wird nur für die Label-Vorschläge zu ungültigen Namen " +
                       "benötigt. Modell und Key werden je Provider gespeichert. " +
                       "„Abrufen“ lädt die beim Provider verfügbaren Modelle (API-Key nötig; " +
                       "eigene Provider auch ohne Key). Über „Neu…“ lassen sich weitere Provider " +
                       "anlegen — jeder OpenAI-kompatible Endpoint (z. B. Ollama, LM Studio, Groq, " +
                       "OpenRouter) oder ein Endpoint im Anthropic-Format.",
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
            layout.Controls.Add(providerRow, 1, 0);
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

        /// <summary>Kompakter Button fuer eine Eingabezeile (neben ComboBox/TextBox).</summary>
        private static Button MakeRowButton(string text)
        {
            return new Button
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(6, 1, 6, 1),
                Margin = new Padding(6, 0, 0, 0),
                Text = text,
                UseVisualStyleBackColor = true,
            };
        }
    }
}
