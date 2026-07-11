using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML;
using Microsoft.ML.Data;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.NLChecker
{
    /// <summary>
    /// PASS Natural-Language checker. Prueft fuer jedes relevante Shape, ob das Label ein
    /// gueltiger Name fuer seinen Typ ist — wahlweise per lokalem ML.NET-Klassifikator
    /// (Default, offline) oder per LLM (<see cref="LlmClient"/>, Provider waehlbar:
    /// UniGPT/OpenAI/Anthropic). Fuer ungueltige Labels liefert das LLM Vorschlaege.
    /// Methode, Provider, Modell und API-Keys kommen aus <see cref="NlCheckerSettings"/>.
    /// </summary>
    public class NlChecker
    {
        private readonly MLContext _mlContext = new MLContext(seed: 0);
        private ITransformer _model;
        private PredictionEngine<ShapeName, ShapeNamePrediction> _predEngine;

        private NlCheckerSettings _settings;
        private LlmClient _llm;

        private const string TrainingResourceName = "ALPS_Visio_AddIn_rewrite.NLChecker.training.tsv";

        private static string AppDataDir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ALPS_Visio_AddIn");

        private static string ModelFilePath => Path.Combine(AppDataDir, "nl_model.zip");

        private bool UseLlmCheck => _settings.CheckMethod == NlCheckerSettings.MethodLlm;

        /// <summary>
        /// Prepares the checker: laedt die Einstellungen und je nach Pruefmethode das lokale
        /// ML-Modell (Erstlauf: Training) bzw. den LLM-Client. Returns false with a message
        /// when the chosen method cannot run.
        /// </summary>
        public bool Initialize(out string error)
        {
            error = null;

            _settings = NlCheckerSettings.Load();
            _llm = string.IsNullOrWhiteSpace(_settings.ActiveApiKey) ? null : new LlmClient(_settings);

            if (UseLlmCheck)
            {
                if (_llm == null)
                {
                    error = "Prüfmethode „LLM“ ist gewählt, aber für den Provider " + _settings.Provider +
                            " ist kein API-Key hinterlegt.\nBitte über den Ribbon-Button " +
                            "„NL-Checker Einstellungen“ setzen (oder auf das lokale ML-Modell umstellen).";
                    return false;
                }
                // Kein ML-Modell noetig -- die Pruefung laeuft komplett ueber das LLM.
                return true;
            }

            try
            {
                LoadOrTrainModel();
            }
            catch (Exception ex)
            {
                error = "Das NL-Klassifikationsmodell konnte nicht geladen/trainiert werden:\n" + ex.Message;
                return false;
            }
            return true;
        }

        /// <summary>Loads the persisted model, training it from the bundled data if none exists yet.</summary>
        private void LoadOrTrainModel()
        {
            Directory.CreateDirectory(AppDataDir);
            if (!File.Exists(ModelFilePath)) TrainModelFromBundledData();

            _model = _mlContext.Model.Load(ModelFilePath, out _);
            _predEngine = _mlContext.Model.CreatePredictionEngine<ShapeName, ShapeNamePrediction>(_model);
        }

        /// <summary>Retrains the model from the bundled training data and reloads it.</summary>
        public void Retrain()
        {
            TrainModelFromBundledData();
            _model = _mlContext.Model.Load(ModelFilePath, out _);
            _predEngine = _mlContext.Model.CreatePredictionEngine<ShapeName, ShapeNamePrediction>(_model);
        }

        private void TrainModelFromBundledData()
        {
            string trainingPath = ExtractTrainingData();

            IDataView trainingDataView = _mlContext.Data.LoadFromTextFile<ShapeName>(
                path: trainingPath, hasHeader: false, separatorChar: '\t');

            IEstimator<ITransformer> pipeline =
                _mlContext.Transforms.Text.FeaturizeText("NameFeaturized", nameof(ShapeName.Name))
                    .Append(_mlContext.Transforms.Text.FeaturizeText("ShapeTypeFeaturized", nameof(ShapeName.ShapeType)))
                    .Append(_mlContext.Transforms.Concatenate("Features", "NameFeaturized", "ShapeTypeFeaturized"))
                    .AppendCacheCheckpoint(_mlContext);

            var trainer = _mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(
                labelColumnName: nameof(ShapeName.IsValidName), featureColumnName: "Features");

            _model = pipeline.Append(trainer).Fit(trainingDataView);

            Directory.CreateDirectory(AppDataDir);
            _mlContext.Model.Save(_model, trainingDataView.Schema, ModelFilePath);
        }

        /// <summary>Writes the embedded training set to a temp file and returns its path.</summary>
        private static string ExtractTrainingData()
        {
            string path = Path.Combine(Path.GetTempPath(), "alps_nl_training.tsv");
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(TrainingResourceName))
            {
                if (stream == null)
                    throw new FileNotFoundException("Eingebettete Trainingsdaten nicht gefunden: " + TrainingResourceName);
                using (FileStream fs = File.Create(path))
                    stream.CopyTo(fs);
            }
            return path;
        }

        /// <summary>Iterates all shapes of the active document and builds the NL-check report.</summary>
        public async Task<string> CheckActiveDocumentAsync(Visio.Application application, ProcessingForm progress)
        {
            if (application?.ActiveDocument == null)
                throw new Exception("Kein aktives Dokument.");

            var document = application.ActiveDocument;
            var result = new StringBuilder();

            result.AppendLine("Prüfmethode: " + (UseLlmCheck
                ? "LLM (" + _llm.Describe() + ")"
                : "Lokales ML-Modell (ML.NET)"));
            result.AppendLine("Vorschläge: " + (_llm != null ? _llm.Describe() : "deaktiviert (kein API-Key)"));
            result.AppendLine();

            int totalShapes = document.Pages.Cast<Visio.Page>().Sum(p => p.Shapes.Count);
            int processed = 0;

            foreach (Visio.Page page in document.Pages)
            {
                result.AppendLine($"#### Page: {page.Name} ####");

                foreach (Visio.Shape shape in page.Shapes)
                {
                    processed++;
                    progress?.UpdateProgress(processed, totalShapes);

                    result.Append(await ProcessShapeAsync(shape));
                }
            }
            return result.ToString();
        }

        private async Task<string> ProcessShapeAsync(Visio.Shape shape)
        {
            var sb = new StringBuilder();

            string label = GetShapePropertyValue(shape, "Prop.lable");
            string componentType = GetShapePropertyValue(shape, "Prop.modelComponentType");
            string multiSubject = GetShapePropertyValue(shape, "Prop.multiSubject");

            bool isMultiSubject = multiSubject.Equals("TRUE", StringComparison.OrdinalIgnoreCase) || multiSubject == "1";
            if (isMultiSubject) componentType = "MultiSubject";

            // Skip connectors / message boxes.
            if (componentType == "StandardMessageConnector" || componentType == "messageBox")
                return string.Empty;

            bool? isValid = null;
            string checkError = null;
            if (ShouldValidate(componentType))
            {
                if (UseLlmCheck)
                {
                    if (Enum.TryParse(componentType, out LlmClient.ShapeType shapeTypeEnum))
                    {
                        try
                        {
                            isValid = await _llm.CheckLabel(shapeTypeEnum, label);
                        }
                        catch (Exception ex)
                        {
                            checkError = ex.Message;
                        }
                    }
                }
                else
                {
                    isValid = await Task.Run(() =>
                        _predEngine?.Predict(new ShapeName { Name = label, ShapeType = componentType })?.IsValid);
                }
            }

            sb.AppendLine($"Shape ID: {shape.ID}");
            sb.AppendLine($"  Name: {shape.Name}");
            sb.AppendLine($"  Label: {label}");
            sb.AppendLine($"  ModelComponentType: {componentType}");

            if (isValid.HasValue)
                sb.AppendLine($"  ValidName: {(isValid.Value ? "VALID" : "INVALID")}");
            else if (checkError != null)
                sb.AppendLine($"  ValidName: FEHLER ({checkError})");

            if (isValid.HasValue && !isValid.Value)
            {
                var suggestions = await GetSuggestionsAsync(componentType, label);
                sb.AppendLine($"  Suggestions:\n{suggestions}");
            }

            sb.AppendLine("------------------------------------");
            return sb.ToString();
        }

        private static string GetShapePropertyValue(Visio.Shape shape, string propName)
        {
            try
            {
                return shape.CellsU[propName]?.ResultStr[(short)Visio.VisUnitCodes.visNoCast] ?? "N/A";
            }
            catch
            {
                return "N/A";
            }
        }

        private static bool ShouldValidate(string componentType)
        {
            return componentType == "FullySpecifiedSubject" ||
                   componentType == "MultiSubject" ||
                   componentType == "DoState" ||
                   componentType == "MessageSpecification" ||
                   componentType == "DoTransition" ||
                   componentType == "SendState" ||
                   componentType == "ReceiveState" ||
                   componentType == "InterfaceSubject";
        }

        private async Task<string> GetSuggestionsAsync(string componentType, string currentLabel)
        {
            if (_llm == null || !Enum.TryParse(componentType, out LlmClient.ShapeType shapeTypeEnum))
                return "Suggestions unavailable (no API key or unsupported type).";
            try
            {
                return await _llm.ImproveLabel(shapeTypeEnum, currentLabel);
            }
            catch (Exception ex)
            {
                return $"Suggestion Error: {ex.Message}";
            }
        }

        /// <summary>Training/prediction input row (matches the tab-separated training data).</summary>
        public class ShapeName
        {
            [LoadColumn(0)] public string Name { get; set; }
            [LoadColumn(1)] public string ShapeType { get; set; }
            [LoadColumn(2)] public bool IsValidName { get; set; }
        }

        /// <summary>Prediction output row.</summary>
        public class ShapeNamePrediction
        {
            [ColumnName("PredictedLabel")] public bool IsValid { get; set; }
        }
    }
}
