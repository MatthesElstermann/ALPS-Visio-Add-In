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
    /// dieses Projekt auf .NET Framework 4.8 (VSTO) festgenagelt ist.
    /// </summary>
    public class LlmClient
    {
        // Ein geteilter HttpClient fuer alle Aufrufe (vorgesehenes Nutzungsmuster) mit
        // Timeout, damit eine haengende API den Check-Durchlauf nicht endlos blockiert.
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

        private const string UniGptUrl = "https://gpt.uni-muenster.de/v1/chat/completions";
        private const string OpenAiUrl = "https://api.openai.com/v1/chat/completions";
        private const string AnthropicUrl = "https://api.anthropic.com/v1/messages";
        private const string AnthropicVersion = "2023-06-01";

        private readonly string _provider;
        private readonly string _apiKey;
        private readonly string _model;

        public LlmClient(NlCheckerSettings settings)
        {
            _provider = settings.Provider;
            _apiKey = settings.ActiveApiKey;
            _model = settings.ActiveModel;
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

        private async Task<string> CompleteAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                throw new InvalidOperationException("Kein API-Key fuer Provider " + _provider + " hinterlegt.");

            return _provider == NlCheckerSettings.ProviderAnthropic
                ? await CompleteAnthropicAsync(prompt)
                : await CompleteOpenAiCompatibleAsync(prompt);
        }

        /// <summary>
        /// OpenAI-kompatible Chat-Completions (UniGPT und OpenAI teilen sich das Format):
        /// POST auf /v1/chat/completions mit Bearer-Token, Antwort in choices[0].message.content.
        /// </summary>
        private async Task<string> CompleteOpenAiCompatibleAsync(string prompt)
        {
            var requestBody = new
            {
                model = _model,
                messages = new[] { new { role = "user", content = prompt } },
                temperature = 0.7,
                max_tokens = 300
            };

            string url = _provider == NlCheckerSettings.ProviderOpenAi ? OpenAiUrl : UniGptUrl;
            using (var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json")
            })
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                var response = await _httpClient.SendAsync(request);
                string responseString = await response.Content.ReadAsStringAsync();

                JObject json = JObject.Parse(responseString);
                if (json["error"] != null)
                    throw new Exception("API Error (" + _provider + "): " + json["error"]["message"]);

                string content = json["choices"]?[0]?["message"]?["content"]?.ToString();
                if (string.IsNullOrWhiteSpace(content))
                    throw new Exception("Leere Antwort von " + _provider + ".");
                return content.Trim();
            }
        }

        /// <summary>
        /// Anthropic Messages API: POST auf /v1/messages mit x-api-key- und
        /// anthropic-version-Header; die Antwort traegt eine content-Liste, deren
        /// text-Bloecke eingesammelt werden. Fehler kommen als {"type":"error",...}.
        /// </summary>
        private async Task<string> CompleteAnthropicAsync(string prompt)
        {
            var requestBody = new
            {
                model = _model,
                max_tokens = 300,
                messages = new[] { new { role = "user", content = prompt } }
            };

            using (var request = new HttpRequestMessage(HttpMethod.Post, AnthropicUrl)
            {
                Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json")
            })
            {
                request.Headers.Add("x-api-key", _apiKey);
                request.Headers.Add("anthropic-version", AnthropicVersion);
                var response = await _httpClient.SendAsync(request);
                string responseString = await response.Content.ReadAsStringAsync();

                JObject json = JObject.Parse(responseString);
                if (json["type"]?.ToString() == "error")
                    throw new Exception("API Error (Anthropic): " + json["error"]?["message"]);

                var sb = new StringBuilder();
                foreach (JToken block in json["content"] ?? new JArray())
                {
                    if (block["type"]?.ToString() == "text")
                        sb.Append(block["text"]?.ToString());
                }

                if (sb.Length == 0)
                    throw new Exception("Leere Antwort von Anthropic (stop_reason: " + json["stop_reason"] + ").");
                return sb.ToString().Trim();
            }
        }
    }
}
