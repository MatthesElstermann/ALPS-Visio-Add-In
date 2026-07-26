using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace ALPS_Visio_AddIn_rewrite.NLChecker
{
    /// <summary>
    /// Ein vom Nutzer selbst angelegter LLM-Provider: beliebiger OpenAI-kompatibler
    /// Chat-Completions-Endpoint (z. B. Ollama/LM Studio lokal, Groq, OpenRouter,
    /// Azure OpenAI) oder ein Endpoint im Anthropic-Messages-Format. Der API-Key wird
    /// wie bei den eingebauten Providern separat je Provider-Name in
    /// <see cref="NlCheckerSettings.ApiKeys"/> gehalten und darf fuer lokale Server
    /// leer bleiben.
    /// </summary>
    public class CustomLlmProvider
    {
        /// <summary>Anzeigename und zugleich Provider-ID (eindeutig, nicht reserviert).</summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// API-Format des Endpoints: <see cref="NlCheckerSettings.FormatOpenAiCompatible"/>
        /// oder <see cref="NlCheckerSettings.FormatAnthropic"/>.
        /// </summary>
        public string ApiFormat { get; set; } = NlCheckerSettings.FormatOpenAiCompatible;

        /// <summary>Vollstaendige Chat-Endpoint-URL, z. B. http://localhost:11434/v1/chat/completions.</summary>
        public string ChatUrl { get; set; } = "";

        /// <summary>
        /// Optionale URL fuer die Modell-Liste (GET). Leer = wird aus
        /// <see cref="ChatUrl"/> abgeleitet (…/chat/completions bzw. …/messages → …/models).
        /// </summary>
        public string ModelsUrl { get; set; } = "";
    }

    /// <summary>
    /// Persistente Einstellungen des PASS NL Checkers: gewaehlter LLM-Provider sowie
    /// API-Key und Modellname je Provider; zusaetzlich die vom Nutzer angelegten
    /// eigenen Provider (<see cref="CustomProviders"/>). Die Gueltigkeitspruefung
    /// laeuft immer ueber das lokale ML-Modell; das LLM liefert nur die
    /// Label-Vorschlaege. Liegt als JSON unter %APPDATA%\ALPS_Visio_AddIn; der alte
    /// Einzel-Key aus llm_api_key.txt (damals nur UniGPT) wird beim ersten Laden migriert.
    /// </summary>
    public class NlCheckerSettings
    {
        public const string ProviderUniGpt = "UniGPT";
        public const string ProviderOpenAi = "OpenAI";
        public const string ProviderAnthropic = "Anthropic";

        public const string FormatOpenAiCompatible = "openai-compatible";
        public const string FormatAnthropic = "anthropic";

        /// <summary>Die fest eingebauten Provider (Namen sind fuer eigene Provider reserviert).</summary>
        public static readonly string[] BuiltInProviders = { ProviderUniGpt, ProviderOpenAi, ProviderAnthropic };

        /// <summary>Aktiver LLM-Provider (fuer die Label-Vorschlaege).</summary>
        public string Provider { get; set; } = ProviderUniGpt;

        /// <summary>API-Key je Provider.</summary>
        public Dictionary<string, string> ApiKeys { get; set; } = new Dictionary<string, string>();

        /// <summary>Modellname je Provider (leer = Default).</summary>
        public Dictionary<string, string> Models { get; set; } = new Dictionary<string, string>();

        /// <summary>Vom Nutzer angelegte eigene Provider (zusaetzlich zu den eingebauten).</summary>
        public List<CustomLlmProvider> CustomProviders { get; set; } = new List<CustomLlmProvider>();

        private static string AppDataDir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ALPS_Visio_AddIn");

        private static string SettingsPath => Path.Combine(AppDataDir, "nl_checker_settings.json");
        private static string LegacyKeyPath => Path.Combine(AppDataDir, "llm_api_key.txt");

        public static string DefaultModelFor(string provider)
        {
            switch (provider)
            {
                case ProviderOpenAi: return "gpt-4o-mini";
                case ProviderAnthropic: return "claude-opus-4-8";
                // UniGPT: fuer den aktuellen Key erlaubt sind u. a. gemma-3, mistral-small.
                case ProviderUniGpt: return "Llama-3.3-70B";
                // Eigene Provider: kein sinnvoller Default -- der Nutzer traegt das
                // Modell ein oder ruft die Liste ueber den Models-Endpoint ab.
                default: return "";
            }
        }

        /// <summary>
        /// Statische Modell-Vorschlaege je Provider fuer die Dropdown-Liste im
        /// Einstellungs-Dialog — als Startpunkt ohne API-Aufruf. Die tatsaechlich
        /// verfuegbaren Modelle liefert <see cref="LlmClient.ListModelsAsync"/>.
        /// </summary>
        public static string[] SuggestedModelsFor(string provider)
        {
            switch (provider)
            {
                case ProviderOpenAi:
                    return new[] { "gpt-4o-mini", "gpt-4o" };
                case ProviderAnthropic:
                    return new[] { "claude-opus-4-8", "claude-sonnet-5", "claude-haiku-4-5" };
                case ProviderUniGpt:
                    return new[] { "Llama-3.3-70B", "gemma-3", "mistral-small" };
                default:
                    return new string[0];
            }
        }

        public static bool IsBuiltInProvider(string provider)
        {
            return Array.IndexOf(BuiltInProviders, provider) >= 0;
        }

        /// <summary>Sucht einen eigenen Provider per Name (ohne Beachtung von Gross-/Kleinschreibung).</summary>
        public CustomLlmProvider FindCustomProvider(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            foreach (CustomLlmProvider custom in CustomProviders)
            {
                if (string.Equals(custom.Name, name, StringComparison.OrdinalIgnoreCase))
                    return custom;
            }
            return null;
        }

        public bool ProviderExists(string provider)
        {
            return IsBuiltInProvider(provider) || FindCustomProvider(provider) != null;
        }

        /// <summary>
        /// True, wenn der aktive Provider fuer Label-Vorschlaege nutzbar ist:
        /// eingebaute Provider brauchen einen API-Key, eigene Provider nur eine
        /// Chat-URL (lokale Server wie Ollama laufen ohne Key).
        /// </summary>
        public bool IsLlmConfigured
        {
            get
            {
                CustomLlmProvider custom = FindCustomProvider(Provider);
                if (custom != null)
                    return !string.IsNullOrWhiteSpace(custom.ChatUrl);
                return IsBuiltInProvider(Provider) && !string.IsNullOrWhiteSpace(ActiveApiKey);
            }
        }

        /// <summary>
        /// Entfernt einen eigenen Provider mitsamt seinem gespeicherten API-Key und
        /// Modellnamen; war er der aktive Provider, faellt die Auswahl auf UniGPT zurueck.
        /// </summary>
        public void RemoveCustomProvider(string name)
        {
            CustomLlmProvider custom = FindCustomProvider(name);
            if (custom == null) return;

            CustomProviders.Remove(custom);
            ApiKeys.Remove(custom.Name);
            Models.Remove(custom.Name);
            if (string.Equals(Provider, custom.Name, StringComparison.OrdinalIgnoreCase))
                Provider = ProviderUniGpt;
        }

        /// <summary>
        /// Zieht API-Key und Modellname beim Umbenennen eines eigenen Providers auf
        /// den neuen Namen um, damit die Eingaben nicht verloren gehen.
        /// </summary>
        public void MoveProviderData(string oldName, string newName)
        {
            if (ApiKeys.TryGetValue(oldName, out string key))
            {
                ApiKeys.Remove(oldName);
                ApiKeys[newName] = key;
            }
            if (Models.TryGetValue(oldName, out string model))
            {
                Models.Remove(oldName);
                Models[newName] = model;
            }
        }

        public string GetApiKey(string provider)
        {
            return ApiKeys.TryGetValue(provider, out string key) ? (key ?? "") : "";
        }

        public void SetApiKey(string provider, string key)
        {
            ApiKeys[provider] = key ?? "";
        }

        public string GetModel(string provider)
        {
            return Models.TryGetValue(provider, out string model) && !string.IsNullOrWhiteSpace(model)
                ? model
                : DefaultModelFor(provider);
        }

        public void SetModel(string provider, string model)
        {
            Models[provider] = model ?? "";
        }

        public string ActiveApiKey => GetApiKey(Provider);
        public string ActiveModel => GetModel(Provider);

        public static NlCheckerSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var loaded = JsonConvert.DeserializeObject<NlCheckerSettings>(File.ReadAllText(SettingsPath));
                    if (loaded != null)
                    {
                        loaded.ApiKeys = loaded.ApiKeys ?? new Dictionary<string, string>();
                        loaded.Models = loaded.Models ?? new Dictionary<string, string>();
                        loaded.CustomProviders = loaded.CustomProviders ?? new List<CustomLlmProvider>();
                        // Defekte Eintraege (z. B. von Hand editierte Datei) aussortieren.
                        loaded.CustomProviders.RemoveAll(p => p == null || string.IsNullOrWhiteSpace(p.Name));
                        // Zeigt der aktive Provider auf einen inzwischen geloeschten
                        // eigenen Provider, auf den Default zurueckfallen.
                        if (!loaded.ProviderExists(loaded.Provider))
                            loaded.Provider = ProviderUniGpt;
                        return loaded;
                    }
                }
            }
            catch
            {
                // Defekte Settings-Datei: mit Defaults weiterarbeiten statt zu crashen.
            }

            var settings = new NlCheckerSettings();

            // Migration vom alten Einzel-Key (llm_api_key.txt, damals immer UniGPT).
            try
            {
                if (File.Exists(LegacyKeyPath))
                    settings.SetApiKey(ProviderUniGpt, File.ReadAllText(LegacyKeyPath).Trim());
            }
            catch
            {
                // Legacy-Key nicht lesbar -- dann eben ohne.
            }

            return settings;
        }

        public void Save()
        {
            Directory.CreateDirectory(AppDataDir);
            File.WriteAllText(SettingsPath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}
