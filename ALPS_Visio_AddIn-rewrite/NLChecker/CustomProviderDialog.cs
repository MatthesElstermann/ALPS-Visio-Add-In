using System;
using System.Windows.Forms;

namespace ALPS_Visio_AddIn_rewrite.NLChecker
{
    /// <summary>
    /// Dialog zum Anlegen bzw. Bearbeiten eines eigenen LLM-Providers
    /// (<see cref="CustomLlmProvider"/>): Name, API-Format (OpenAI-kompatibel oder
    /// Anthropic-Messages) sowie Chat- und optionale Models-URL. Vorlagen fuer
    /// bekannte oeffentliche und lokale Provider befuellen die Felder per Auswahl;
    /// alles bleibt frei editierbar. Validiert Name (nicht leer, nicht reserviert,
    /// eindeutig) und URLs (absolute http/https). Aendert die Settings nicht selbst —
    /// das Ergebnis steht in <see cref="Result"/> und wird vom Einstellungs-Dialog
    /// uebernommen. DPI-fest per AutoSize-Layout.
    /// </summary>
    public class CustomProviderDialog : Form
    {
        private readonly NlCheckerSettings _settings;
        /// <summary>Beim Bearbeiten der bestehende Provider (fuer die Namens-Duplikatpruefung); null = Neuanlage.</summary>
        private readonly CustomLlmProvider _editing;

        /// <summary>Die validierten Eingaben nach OK; vom Aufrufer zu uebernehmen.</summary>
        public CustomLlmProvider Result { get; private set; }

        private ComboBox cmbTemplate;
        private TextBox txtName;
        private ComboBox cmbFormat;
        private TextBox txtChatUrl;
        private TextBox txtModelsUrl;
        private Button btnOK;
        private Button btnCancel;

        private static readonly string[] FormatIds =
        {
            NlCheckerSettings.FormatOpenAiCompatible,
            NlCheckerSettings.FormatAnthropic,
        };

        private static readonly string[] FormatLabels =
        {
            "OpenAI-kompatibel (chat/completions)",
            "Anthropic (messages)",
        };

        /// <summary>Vorlage fuer einen bekannten Provider: befuellt die Eingabefelder.</summary>
        private class ProviderTemplate
        {
            public string Label;
            public string Name;
            public string ApiFormat = NlCheckerSettings.FormatOpenAiCompatible;
            public string ChatUrl;
            /// <summary>Leer = aus der Chat-URL ableitbar.</summary>
            public string ModelsUrl = "";
        }

        /// <summary>
        /// Bekannte oeffentliche Cloud-Provider und lokale Server als Startpunkte.
        /// Alle sprechen das OpenAI-Chat-Completions-Format (Google Gemini ueber
        /// seinen OpenAI-Kompatibilitaets-Endpoint). Die URLs entsprechen dem Stand
        /// der jeweiligen Anbieter-Doku und bleiben im Dialog frei editierbar.
        /// </summary>
        private static readonly ProviderTemplate[] Templates =
        {
            new ProviderTemplate { Label = "Groq", Name = "Groq",
                ChatUrl = "https://api.groq.com/openai/v1/chat/completions" },
            new ProviderTemplate { Label = "OpenRouter", Name = "OpenRouter",
                ChatUrl = "https://openrouter.ai/api/v1/chat/completions" },
            new ProviderTemplate { Label = "Mistral", Name = "Mistral",
                ChatUrl = "https://api.mistral.ai/v1/chat/completions" },
            new ProviderTemplate { Label = "DeepSeek", Name = "DeepSeek",
                ChatUrl = "https://api.deepseek.com/v1/chat/completions" },
            new ProviderTemplate { Label = "Together AI", Name = "Together AI",
                ChatUrl = "https://api.together.xyz/v1/chat/completions" },
            new ProviderTemplate { Label = "xAI (Grok)", Name = "xAI",
                ChatUrl = "https://api.x.ai/v1/chat/completions" },
            new ProviderTemplate { Label = "Google Gemini (OpenAI-Endpoint)", Name = "Gemini",
                ChatUrl = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions" },
            new ProviderTemplate { Label = "Ollama (lokal)", Name = "Ollama",
                ChatUrl = "http://localhost:11434/v1/chat/completions" },
            new ProviderTemplate { Label = "LM Studio (lokal)", Name = "LM Studio",
                ChatUrl = "http://localhost:1234/v1/chat/completions" },
        };

        public CustomProviderDialog(NlCheckerSettings settings, CustomLlmProvider editing)
        {
            _settings = settings;
            _editing = editing;
            InitializeComponent();

            if (editing != null)
            {
                txtName.Text = editing.Name;
                txtChatUrl.Text = editing.ChatUrl;
                txtModelsUrl.Text = editing.ModelsUrl;
                int formatIndex = Array.IndexOf(FormatIds, editing.ApiFormat);
                cmbFormat.SelectedIndex = formatIndex >= 0 ? formatIndex : 0;
            }
            else
            {
                cmbFormat.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Befuellt die Felder aus der gewaehlten Vorlage (Index 0 = keine Vorlage).
        /// Ist der Vorlagen-Name schon vergeben, wird "&lt;Name&gt; 2", "&lt;Name&gt; 3", …
        /// vorgeschlagen, damit die Namens-Validierung nicht sofort anschlaegt.
        /// </summary>
        private void cmbTemplate_SelectedIndexChanged(object sender, EventArgs e)
        {
            int templateIndex = cmbTemplate.SelectedIndex - 1;
            if (templateIndex < 0 || templateIndex >= Templates.Length) return;

            ProviderTemplate template = Templates[templateIndex];

            string name = template.Name;
            for (int suffix = 2; ; suffix++)
            {
                CustomLlmProvider taken = _settings.FindCustomProvider(name);
                if (taken == null || ReferenceEquals(taken, _editing)) break;
                name = template.Name + " " + suffix;
            }

            txtName.Text = name;
            txtChatUrl.Text = template.ChatUrl;
            txtModelsUrl.Text = template.ModelsUrl;
            int formatIndex = Array.IndexOf(FormatIds, template.ApiFormat);
            cmbFormat.SelectedIndex = formatIndex >= 0 ? formatIndex : 0;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            string name = txtName.Text.Trim();
            string chatUrl = txtChatUrl.Text.Trim();
            string modelsUrl = txtModelsUrl.Text.Trim();

            string error = Validate(name, chatUrl, modelsUrl);
            if (error != null)
            {
                MessageBox.Show(error, "PASS NL Checker", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Result = new CustomLlmProvider
            {
                Name = name,
                ApiFormat = FormatIds[cmbFormat.SelectedIndex],
                ChatUrl = chatUrl,
                ModelsUrl = modelsUrl,
            };
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        /// <summary>Liefert die Fehlermeldung zur ersten verletzten Regel, sonst null.</summary>
        private string Validate(string name, string chatUrl, string modelsUrl)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Bitte einen Namen für den Provider angeben.";

            foreach (string builtIn in NlCheckerSettings.BuiltInProviders)
            {
                if (string.Equals(name, builtIn, StringComparison.OrdinalIgnoreCase))
                    return "Der Name „" + builtIn + "“ ist für den eingebauten Provider reserviert.";
            }

            CustomLlmProvider existing = _settings.FindCustomProvider(name);
            if (existing != null && !ReferenceEquals(existing, _editing))
                return "Es gibt bereits einen eigenen Provider mit dem Namen „" + existing.Name + "“.";

            if (!IsValidHttpUrl(chatUrl))
                return "Bitte eine vollständige Chat-URL angeben (http:// oder https://), " +
                       "z. B. http://localhost:11434/v1/chat/completions.";

            if (!string.IsNullOrWhiteSpace(modelsUrl) && !IsValidHttpUrl(modelsUrl))
                return "Die Models-URL ist keine gültige http(s)-Adresse. Feld leer lassen, " +
                       "um sie automatisch aus der Chat-URL abzuleiten.";

            return null;
        }

        private static bool IsValidHttpUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out Uri parsed)
                && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps);
        }

        private void InitializeComponent()
        {
            Label lblTemplate = MakeLabel("Vorlage:");
            Label lblName = MakeLabel("Name:");
            Label lblFormat = MakeLabel("API-Format:");
            Label lblChatUrl = MakeLabel("Chat-URL:");
            Label lblModelsUrl = MakeLabel("Models-URL:");

            cmbTemplate = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
            };
            cmbTemplate.Items.Add("(keine Vorlage — Felder selbst ausfüllen)");
            foreach (ProviderTemplate template in Templates)
                cmbTemplate.Items.Add(template.Label);
            cmbTemplate.SelectedIndex = 0;
            cmbTemplate.SelectedIndexChanged += cmbTemplate_SelectedIndexChanged;

            txtName = MakeTextBox();
            txtChatUrl = MakeTextBox();
            txtModelsUrl = MakeTextBox();

            cmbFormat = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
            };
            cmbFormat.Items.AddRange(FormatLabels);

            Label lblHint = new Label
            {
                AutoSize = true,
                MaximumSize = new System.Drawing.Size(420, 0),
                ForeColor = System.Drawing.SystemColors.GrayText,
                Margin = new Padding(0, 8, 0, 0),
                Text = "Eine Vorlage befüllt die Felder für bekannte Provider (URLs bitte bei " +
                       "Änderungen des Anbieters prüfen); alles bleibt frei editierbar. Die " +
                       "Chat-URL ist der vollständige Endpoint, z. B. für ein lokales Ollama " +
                       "http://localhost:11434/v1/chat/completions. Bleibt die Models-URL leer, " +
                       "wird sie aus der Chat-URL abgeleitet (…/models). Das Modell und ein " +
                       "API-Key (bei lokalen Servern nicht nötig) werden anschließend im " +
                       "Einstellungs-Dialog hinterlegt.",
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
                RowCount = 7,
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            layout.Controls.Add(lblTemplate, 0, 0);
            layout.Controls.Add(cmbTemplate, 1, 0);
            layout.Controls.Add(lblName, 0, 1);
            layout.Controls.Add(txtName, 1, 1);
            layout.Controls.Add(lblFormat, 0, 2);
            layout.Controls.Add(cmbFormat, 1, 2);
            layout.Controls.Add(lblChatUrl, 0, 3);
            layout.Controls.Add(txtChatUrl, 1, 3);
            layout.Controls.Add(lblModelsUrl, 0, 4);
            layout.Controls.Add(txtModelsUrl, 1, 4);
            layout.Controls.Add(lblHint, 1, 5);
            layout.Controls.Add(buttonRow, 1, 6);

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
            this.Name = "CustomProviderDialog";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = _editing == null
                ? "Eigenen Provider anlegen"
                : "Eigenen Provider bearbeiten";
        }

        private static TextBox MakeTextBox()
        {
            return new TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                MinimumSize = new System.Drawing.Size(320, 0),
            };
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
