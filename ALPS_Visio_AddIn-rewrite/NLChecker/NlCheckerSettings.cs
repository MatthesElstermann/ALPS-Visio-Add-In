using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace ALPS_Visio_AddIn_rewrite.NLChecker
{
    /// <summary>
    /// Persistente Einstellungen des PASS NL Checkers: gewaehlter LLM-Provider sowie
    /// API-Key und Modellname je Provider. Die Gueltigkeitspruefung laeuft immer ueber
    /// das lokale ML-Modell; das LLM liefert nur die Label-Vorschlaege. Liegt als
    /// JSON unter %APPDATA%\ALPS_Visio_AddIn; der alte Einzel-Key aus llm_api_key.txt
    /// (damals nur UniGPT) wird beim ersten Laden migriert.
    /// </summary>
    public class NlCheckerSettings
    {
        public const string ProviderUniGpt = "UniGPT";
        public const string ProviderOpenAi = "OpenAI";
        public const string ProviderAnthropic = "Anthropic";

        /// <summary>Aktiver LLM-Provider (fuer die Label-Vorschlaege).</summary>
        public string Provider { get; set; } = ProviderUniGpt;

        /// <summary>API-Key je Provider.</summary>
        public Dictionary<string, string> ApiKeys { get; set; } = new Dictionary<string, string>();

        /// <summary>Modellname je Provider (leer = Default).</summary>
        public Dictionary<string, string> Models { get; set; } = new Dictionary<string, string>();

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
                default: return "Llama-3.3-70B";
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
                default:
                    return new[] { "Llama-3.3-70B", "gemma-3", "mistral-small" };
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
