using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ALPS_Visio_AddIn_rewrite.NLChecker
{
    /// <summary>
    /// Provider-agnostischer LLM-Client des PASS NL Checkers (Nachfolger des reinen
    /// UniGPT-LabelImprovers). Unterstuetzt OpenAI-kompatible Chat-Completions
    /// (UniGPT Uni Muenster, OpenAI) sowie die Anthropic Messages API — jeweils direkt
    /// per HttpClient, da das offizielle Anthropic-SDK modernes .NET voraussetzt und
    /// dieses Projekt auf .NET Framework 4.8 (VSTO) festgenagelt ist. Neben den drei
    /// eingebauten Providern laufen auch die vom Nutzer angelegten eigenen Provider
    /// (<see cref="CustomLlmProvider"/>) ueber dieselben zwei Formate; ihre Endpoints
    /// kommen aus den Settings statt aus den Konstanten unten.
    /// </summary>
    public class LlmClient
    {
        // Ein geteilter HttpClient fuer alle Aufrufe (vorgesehenes Nutzungsmuster) mit
        // Timeout, damit eine haengende API den Check-Durchlauf nicht endlos blockiert.
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

        static LlmClient()
        {
            // .NET Framework 4.8 im Office-Host: je nach Windows-/Registry-Konfiguration
            // fehlt TLS 1.2 im Default-Protokollsatz -- HTTPS-Aufrufe scheitern dann mit
            // einer generischen HttpRequestException. TLS 1.2 explizit zuschalten,
            // TLS 1.3 best effort (erst ab neueren Windows-Versionen vorhanden).
            try { System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12; }
            catch { }
            try { System.Net.ServicePointManager.SecurityProtocol |= (System.Net.SecurityProtocolType)12288; }
            catch { }
        }

        /// <summary>
        /// Fuehrt den HTTP-Aufruf aus und uebersetzt Netzwerkfehler in verstaendliche
        /// Meldungen: HttpRequestException traegt die eigentliche Ursache (DNS,
        /// Verbindung, TLS) unsichtbar in der InnerException; bei UniGPT kommt der
        /// Hinweis dazu, dass der Endpoint nur aus dem Uni-Netz/VPN erreichbar ist.
        /// </summary>
        private static async Task<string> SendWithDiagnosticsAsync(HttpRequestMessage request, string provider)
        {
            try
            {
                var response = await _httpClient.SendAsync(request);
                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                string reason = ex.GetBaseException().Message;
                string hint = provider == NlCheckerSettings.ProviderUniGpt
                    ? "\nHinweis: gpt.uni-muenster.de ist in der Regel nur aus dem Universitätsnetz bzw. per VPN erreichbar."
                    : "";
                throw new Exception("Der Provider " + provider + " ist nicht erreichbar: " + reason + hint, ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new Exception("Zeitüberschreitung beim Aufruf von " + provider + " (60 s).", ex);
            }
        }

        /// <summary>Parst die API-Antwort; nicht-JSON (Proxy-/Fehlerseiten) wird lesbar gemeldet.</summary>
        private static JObject ParseResponse(string responseString, string provider)
        {
            try
            {
                return JObject.Parse(responseString);
            }
            catch
            {
                string preview = responseString == null ? "<leer>"
                    : responseString.Length > 300 ? responseString.Substring(0, 300) + "…" : responseString;
                throw new Exception("Unerwartete (Nicht-JSON-)Antwort von " + provider + ": " + preview);
            }
        }

        private const string UniGptUrl = "https://gpt.uni-muenster.de/v1/chat/completions";
        private const string OpenAiUrl = "https://api.openai.com/v1/chat/completions";
        private const string AnthropicUrl = "https://api.anthropic.com/v1/messages";
        private const string AnthropicVersion = "2023-06-01";

        private const string UniGptModelsUrl = "https://gpt.uni-muenster.de/v1/models";
        private const string OpenAiModelsUrl = "https://api.openai.com/v1/models";
        private const string AnthropicModelsUrl = "https://api.anthropic.com/v1/models";

        /// <summary>Aufgeloeste Endpoint-Daten eines Providers (eingebaut oder eigener).</summary>
        private class ProviderEndpoint
        {
            public string ChatUrl;
            /// <summary>Null, wenn keine Models-URL bekannt/ableitbar ist.</summary>
            public string ModelsUrl;
            /// <summary>True = Anthropic-Messages-Format, sonst OpenAI-Chat-Completions.</summary>
            public bool AnthropicFormat;
            /// <summary>Eingebaute Provider verlangen einen API-Key, eigene nicht (lokale Server).</summary>
            public bool RequiresApiKey;
        }

        /// <summary>
        /// Liefert Chat-/Models-URL und API-Format des Providers: fuer die drei
        /// eingebauten aus den Konstanten, fuer eigene Provider aus den Settings.
        /// </summary>
        private static ProviderEndpoint ResolveEndpoint(NlCheckerSettings settings, string provider)
        {
            switch (provider)
            {
                case NlCheckerSettings.ProviderUniGpt:
                    return new ProviderEndpoint { ChatUrl = UniGptUrl, ModelsUrl = UniGptModelsUrl, RequiresApiKey = true };
                case NlCheckerSettings.ProviderOpenAi:
                    return new ProviderEndpoint { ChatUrl = OpenAiUrl, ModelsUrl = OpenAiModelsUrl, RequiresApiKey = true };
                case NlCheckerSettings.ProviderAnthropic:
                    return new ProviderEndpoint { ChatUrl = AnthropicUrl, ModelsUrl = AnthropicModelsUrl, AnthropicFormat = true, RequiresApiKey = true };
            }

            CustomLlmProvider custom = settings.FindCustomProvider(provider);
            if (custom == null)
                throw new InvalidOperationException("Unbekannter LLM-Provider: " + provider);

            return new ProviderEndpoint
            {
                ChatUrl = custom.ChatUrl,
                ModelsUrl = !string.IsNullOrWhiteSpace(custom.ModelsUrl) ? custom.ModelsUrl : DeriveModelsUrl(custom.ChatUrl),
                AnthropicFormat = custom.ApiFormat == NlCheckerSettings.FormatAnthropic,
                RequiresApiKey = false,
            };
        }

        /// <summary>
        /// Leitet die Models-URL aus einer Standard-Chat-URL ab
        /// (…/chat/completions bzw. …/messages → …/models); null, wenn die URL keinem
        /// der beiden Muster folgt — dann muss der Nutzer die Models-URL eintragen.
        /// </summary>
        private static string DeriveModelsUrl(string chatUrl)
        {
            string trimmed = (chatUrl ?? "").TrimEnd('/');
            if (trimmed.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
                return trimmed.Substring(0, trimmed.Length - "/chat/completions".Length) + "/models";
            if (trimmed.EndsWith("/messages", StringComparison.OrdinalIgnoreCase))
                return trimmed.Substring(0, trimmed.Length - "/messages".Length) + "/models";
            return null;
        }

        private readonly string _provider;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly ProviderEndpoint _endpoint;

        public LlmClient(NlCheckerSettings settings)
        {
            _provider = settings.Provider;
            _apiKey = settings.ActiveApiKey;
            _model = settings.ActiveModel;
            _endpoint = ResolveEndpoint(settings, settings.Provider);
        }

        /// <summary>Kurzbeschreibung fuer den Report-Kopf, z. B. "OpenAI / gpt-4o-mini".</summary>
        public string Describe()
        {
            return _provider + " / " + _model;
        }

        /// <summary>The shape types the LLM can produce label suggestions for.</summary>
        public enum ShapeType
        {
            FullySpecifiedSubject,
            MultiSubject,
            MessageSpecification,
            DoState,
            DoTransition,
            SendState,
            ReceiveState,
            InterfaceSubject,
        }

        private static string GetShapeDescription(ShapeType shapeType)
        {
            switch (shapeType)
            {
                case ShapeType.FullySpecifiedSubject:
                    return "A FullySpecifiedSubject represents real people, machines, or abstract entities such as departments or entire companies.";
                case ShapeType.MultiSubject:
                    return "A MultiSubject represents a collection of actors that can execute an identical workflow either in parallel or multiple times within an S-BPM PASS diagram. A well-formed Multi-Subject name should be in plural form, role-based, specific yet generic enough to describe the group’s function, and consistent with other subject names in the model.";
                case ShapeType.MessageSpecification:
                    return "A MessageSpecification characterizes the exchange of information on all possible channels, including the exchange of physical objects.";
                case ShapeType.DoState:
                    return "A DoState represents an internal, independent action of a subject, such as processing data or making a decision.";
                case ShapeType.DoTransition:
                    return "A DoTransition represents the completion or result of a DoState, indicating that the internal action of a subject has been successfully executed and the state can transition to the next phase.";
                case ShapeType.SendState:
                    return "A SendState represents the activity of the transmission of messages or interactions with other subjects.";
                case ShapeType.ReceiveState:
                    return "A ReceiveState represents the receipt of messages or information from other subjects.";
                case ShapeType.InterfaceSubject:
                    return "An InterfaceSubject represents an external actor or system whose internal behavior is not modeled within the current process. It serves solely to send or receive messages and is used when the external entity is unknown, irrelevant, or defined elsewhere.";
                default:
                    throw new ArgumentException("Invalid shape type.");
            }
        }

        /// <summary>
        /// Requests two improved label suggestions for the given shape type and current label.
        /// </summary>
        public async Task<string> ImproveLabel(ShapeType shapeType, string currentLabel)
        {
            string prompt = $@"
        In an S-BPM (PASS) diagram, you are analyzing a {shapeType}.
        {GetShapeDescription(shapeType)}
        The current label for this {shapeType} is: '{currentLabel}'.

        Provide **two suggestions** for a more precise and meaningful label that better describes the purpose or functionality of this {shapeType}.
        Each suggestion should be concise, clear, and aligned with the role of a {shapeType} in an S-BPM (PASS) diagram.
        Format the output as:
        1. Suggestion 1
        2. Suggestion 2
    ";
            return await CompleteAsync(prompt);
        }

        /// <summary>
        /// Fragt die beim Provider tatsaechlich verfuegbaren Modelle ab (GET auf den
        /// Models-Endpoint — OpenAI-kompatibel mit Bearer-Token, Anthropic-Format mit
        /// x-api-key-Header). Fuer die Modell-Auswahl im Einstellungs-Dialog. Eigene
        /// Provider duerfen ohne API-Key abgefragt werden (lokale Server wie Ollama).
        /// </summary>
        public static async Task<System.Collections.Generic.IList<string>> ListModelsAsync(
            NlCheckerSettings settings, string provider, string apiKey)
        {
            ProviderEndpoint endpoint = ResolveEndpoint(settings, provider);

            if (endpoint.RequiresApiKey && string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Kein API-Key für Provider " + provider + " eingetragen.");
            if (string.IsNullOrWhiteSpace(endpoint.ModelsUrl))
                throw new InvalidOperationException(
                    "Für den Provider " + provider + " ist keine Models-URL bekannt und sie ließ sich "
                    + "nicht aus der Chat-URL ableiten. Bitte im Provider-Dialog eine Models-URL eintragen.");

            using (var request = new HttpRequestMessage(HttpMethod.Get, endpoint.ModelsUrl))
            {
                if (endpoint.AnthropicFormat)
                {
                    if (!string.IsNullOrWhiteSpace(apiKey))
                        request.Headers.Add("x-api-key", apiKey);
                    request.Headers.Add("anthropic-version", AnthropicVersion);
                }
                else if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                }

                string responseString = await SendWithDiagnosticsAsync(request, provider);

                JObject json = ParseResponse(responseString, provider);
                if (json["error"] != null)
                    throw new Exception("API Error (" + provider + "): " + json["error"]["message"]);
                if (json["type"]?.ToString() == "error")
                    throw new Exception("API Error (" + provider + "): " + json["error"]?["message"]);

                var models = new System.Collections.Generic.List<string>();
                foreach (JToken entry in json["data"] ?? new JArray())
                {
                    string id = entry["id"]?.ToString();
                    if (string.IsNullOrWhiteSpace(id))
                        continue;
                    // OpenAI listet auch Embedding-/Audio-/Bild-Modelle -- fuer den
                    // Checker sind nur die Chat-Modelle sinnvoll.
                    if (provider == NlCheckerSettings.ProviderOpenAi
                        && !(id.StartsWith("gpt-") || id.StartsWith("chatgpt-")
                             || (id.Length > 1 && id[0] == 'o' && char.IsDigit(id[1]))))
                        continue;
                    models.Add(id);
                }

                if (models.Count == 0)
                    throw new Exception("Der Provider " + provider + " hat keine (passenden) Modelle gemeldet.");
                models.Sort(StringComparer.OrdinalIgnoreCase);
                return models;
            }
        }

        private async Task<string> CompleteAsync(string prompt)
        {
            if (_endpoint.RequiresApiKey && string.IsNullOrWhiteSpace(_apiKey))
                throw new InvalidOperationException("Kein API-Key fuer Provider " + _provider + " hinterlegt.");
            if (string.IsNullOrWhiteSpace(_model))
                throw new InvalidOperationException("Kein Modell fuer Provider " + _provider + " eingetragen.");

            return _endpoint.AnthropicFormat
                ? await CompleteAnthropicAsync(prompt)
                : await CompleteOpenAiCompatibleAsync(prompt);
        }

        /// <summary>
        /// OpenAI-kompatible Chat-Completions (UniGPT, OpenAI und eigene Provider im
        /// OpenAI-Format): POST auf die Chat-URL mit Bearer-Token (falls Key vorhanden),
        /// Antwort in choices[0].message.content.
        /// </summary>
        private async Task<string> CompleteOpenAiCompatibleAsync(string prompt)
        {
            // Neuere OpenAI-Modelle (gpt-5, o-Serie) lehnen das klassische "max_tokens"
            // ab (verlangen "max_completion_tokens") und akzeptieren auch keine
            // temperature != 1 mehr. Zudem sind es Reasoning-Modelle: das interne
            // Denken zaehlt mit ins Budget, deshalb deutlich mehr Tokens erlauben,
            // sonst kommt eine leere Antwort zurueck. Alle anderen OpenAI-kompatiblen
            // Provider (UniGPT, Ollama & Co.) bleiben beim klassischen Parametersatz.
            object requestBody = _provider == NlCheckerSettings.ProviderOpenAi
                ? (object)new
                {
                    model = _model,
                    messages = new[] { new { role = "user", content = prompt } },
                    max_completion_tokens = 2000
                }
                : new
                {
                    model = _model,
                    messages = new[] { new { role = "user", content = prompt } },
                    temperature = 0.7,
                    max_tokens = 300
                };

            using (var request = new HttpRequestMessage(HttpMethod.Post, _endpoint.ChatUrl)
            {
                Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json")
            })
            {
                if (!string.IsNullOrWhiteSpace(_apiKey))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                string responseString = await SendWithDiagnosticsAsync(request, _provider);

                JObject json = ParseResponse(responseString, _provider);
                if (json["error"] != null)
                    throw new Exception("API Error (" + _provider + "): " + json["error"]["message"]);

                string content = json["choices"]?[0]?["message"]?["content"]?.ToString();
                if (string.IsNullOrWhiteSpace(content))
                    throw new Exception("Leere Antwort von " + _provider + ".");
                return content.Trim();
            }
        }

        /// <summary>
        /// Anthropic Messages API (eingebauter Anthropic-Provider und eigene Provider
        /// im Anthropic-Format): POST auf die Chat-URL mit x-api-key- (falls Key
        /// vorhanden) und anthropic-version-Header; die Antwort traegt eine
        /// content-Liste, deren text-Bloecke eingesammelt werden. Fehler kommen als
        /// {"type":"error",...}.
        /// </summary>
        private async Task<string> CompleteAnthropicAsync(string prompt)
        {
            var requestBody = new
            {
                model = _model,
                max_tokens = 300,
                messages = new[] { new { role = "user", content = prompt } }
            };

            using (var request = new HttpRequestMessage(HttpMethod.Post, _endpoint.ChatUrl)
            {
                Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json")
            })
            {
                if (!string.IsNullOrWhiteSpace(_apiKey))
                    request.Headers.Add("x-api-key", _apiKey);
                request.Headers.Add("anthropic-version", AnthropicVersion);
                string responseString = await SendWithDiagnosticsAsync(request, _provider);

                JObject json = ParseResponse(responseString, _provider);
                if (json["type"]?.ToString() == "error")
                    throw new Exception("API Error (" + _provider + "): " + json["error"]?["message"]);

                var sb = new StringBuilder();
                foreach (JToken block in json["content"] ?? new JArray())
                {
                    if (block["type"]?.ToString() == "text")
                        sb.Append(block["text"]?.ToString());
                }

                if (sb.Length == 0)
                    throw new Exception("Leere Antwort von " + _provider + " (stop_reason: " + json["stop_reason"] + ").");
                return sb.ToString().Trim();
            }
        }
    }
}
