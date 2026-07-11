using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ALPS_Visio_AddIn_rewrite.NLChecker
{
    /// <summary>
    /// Improves the labels of S-BPM (PASS) diagram shapes via an LLM (Uni Münster GPT / ChatGPT-style API).
    /// Ported from the standalone NLPPASSChecking add-in.
    /// </summary>
    public class LabelImprover
    {
        // Ein HttpClient fuer alle Improver-Instanzen: pro Check-Lauf wird ein neuer
        // LabelImprover erzeugt, ein Instanz-Client wuerde also bei jedem Lauf einen
        // weiteren Socket-Pool liegen lassen (HttpClient ist auf Wiederverwendung
        // ausgelegt und wird hier nie disposed). Der Timeout verhindert, dass ein
        // haengender API-Aufruf den Check-Durchlauf endlos blockiert.
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

        private readonly string _apiKey;

        /// <summary>Creates the improver with the given API key.</summary>
        public LabelImprover(string apiKey)
        {
            _apiKey = apiKey;
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

        /// <summary>
        /// Requests two improved label suggestions for the given shape type and current label from the LLM.
        /// </summary>
        public async Task<string> ImproveLabel(ShapeType shapeType, string currentLabel)
        {
            string shapeDescription;
            switch (shapeType)
            {
                case ShapeType.FullySpecifiedSubject:
                    shapeDescription = "A FullySpecifiedSubject represents real people, machines, or abstract entities such as departments or entire companies.";
                    break;
                case ShapeType.MultiSubject:
                    shapeDescription = "A MultiSubject represents a collection of actors that can execute an identical workflow either in parallel or multiple times within an S-BPM PASS diagram. A well-formed Multi-Subject name should be in plural form, role-based, specific yet generic enough to describe the group’s function, and consistent with other subject names in the model.";
                    break;
                case ShapeType.MessageSpecification:
                    shapeDescription = "A MessageSpecification characterizes the exchange of information on all possible channels, including the exchange of physical objects.";
                    break;
                case ShapeType.DoState:
                    shapeDescription = "A DoState represents an internal, independent action of a subject, such as processing data or making a decision.";
                    break;
                case ShapeType.DoTransition:
                    shapeDescription = "A DoTransition represents the completion or result of a DoState, indicating that the internal action of a subject has been successfully executed and the state can transition to the next phase.";
                    break;
                case ShapeType.SendState:
                    shapeDescription = "A SendState represents the activity of the transmission of messages or interactions with other subjects.";
                    break;
                case ShapeType.ReceiveState:
                    shapeDescription = "A ReceiveState represents the receipt of messages or information from other subjects.";
                    break;
                case ShapeType.InterfaceSubject:
                    shapeDescription = "An InterfaceSubject represents an external actor or system whose internal behavior is not modeled within the current process. It serves solely to send or receive messages and is used when the external entity is unknown, irrelevant, or defined elsewhere.";
                    break;
                default:
                    throw new ArgumentException("Invalid shape type.");
            }

            string prompt = $@"
        In an S-BPM (PASS) diagram, you are analyzing a {shapeType}.
        {shapeDescription}
        The current label for this {shapeType} is: '{currentLabel}'.

        Provide **two suggestions** for a more precise and meaningful label that better describes the purpose or functionality of this {shapeType}.
        Each suggestion should be concise, clear, and aligned with the role of a {shapeType} in an S-BPM (PASS) diagram.
        Format the output as:
        1. Suggestion 1
        2. Suggestion 2
    ";

            var requestBody = new
            {
                model = "Llama-3.3-70B", // Allowed models for the current API key: ['gemma-3', 'mistral-small', 'Llama-3.3-70B']
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                },
                temperature = 0.7,  // controls sampling "creativity"
                max_tokens = 200    // limits generated sequence length
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            const string url = "https://gpt.uni-muenster.de/v1/chat/completions";

            try
            {
                // Authorization pro Request statt auf DefaultRequestHeaders: der Client ist
                // geteilt, und der Nutzer kann den API-Key zwischen zwei Laeufen wechseln.
                string responseString;
                using (var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content })
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                    var response = await _httpClient.SendAsync(request);
                    responseString = await response.Content.ReadAsStringAsync();
                }

                var responseObject = JObject.Parse(responseString);

                // OpenAI-style error format
                if (responseObject["error"] != null)
                {
                    throw new Exception($"API Error: {responseObject["error"]["message"]}");
                }

                var choices = responseObject["choices"]?.ToArray();
                if (choices == null || choices.Length == 0)
                {
                    throw new Exception("No choices found in the API response.");
                }

                var messageContent = choices[0]?["message"]?["content"]?.ToString();

                if (string.IsNullOrWhiteSpace(messageContent))
                {
                    throw new Exception("API response contains empty message content.");
                }

                return messageContent.Trim();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"API request failed: {ex.Message}", ex);
            }
            catch (JsonException jsonEx)
            {
                throw new Exception($"Failed to parse API response: {jsonEx.Message}", jsonEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to process API response: {ex.Message}", ex);
            }
        }
    }
}
