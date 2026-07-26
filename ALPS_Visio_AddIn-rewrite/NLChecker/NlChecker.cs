using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML;
using Microsoft.ML.Data;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.NLChecker
{
    /// <summary>
    /// PASS Natural-Language checker. Prueft fuer jedes relevante Shape per lokalem
    /// ML.NET-Klassifikator (offline), ob das Label ein gueltiger Name fuer seinen Typ
    /// ist. Fuer ungueltige Labels liefert ein LLM (<see cref="LlmClient"/>, Provider
    /// waehlbar: eingebaut UniGPT/OpenAI/Anthropic oder vom Nutzer angelegte eigene
    /// Provider) Verbesserungsvorschlaege. Provider, Modell und API-Keys kommen aus
    /// <see cref="NlCheckerSettings"/>.
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

        /// <summary>
        /// Prepares the checker: laedt die Einstellungen, das lokale ML-Modell (Erstlauf:
        /// Training) und -- falls der LLM-Provider konfiguriert ist (eingebaute brauchen
        /// einen API-Key, eigene nur ihre Chat-URL) -- den LLM-Client fuer die
        /// Label-Vorschlaege. Returns false with a message when the model can't be built.
        /// </summary>
        public bool Initialize(out string error)
        {
            error = null;

            _settings = NlCheckerSettings.Load();
            _llm = _settings.IsLlmConfigured ? new LlmClient(_settings) : null;

            try
            {
                LoadOrTrainModel();
            }
            catch (Exception ex)
            {
                // Volle Exception-Kette ausgeben (ToString inkl. InnerExceptions/Stacktrace):
                // die eigentliche Ursache -- z. B. eine DllNotFoundException fuer
                // CpuMathNative.dll -- steckt sonst unsichtbar in der InnerException.
                error = "Das NL-Klassifikationsmodell konnte nicht geladen/trainiert werden.\n\n" + ex
                    + "\n\n--- Native-DLL-Suche ---\n" + NativeDiagnostics;
                return false;
            }
            return true;
        }

        // -------------------------------------------------------------------------
        // Native-DLL-Aufloesung fuer ML.NET im Visio-Host
        // -------------------------------------------------------------------------

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        private static bool _nativeResolutionPrepared;

        /// <summary>
        /// Protokoll der Native-DLL-Suche; wird bei Fehlern an die Meldung angehaengt,
        /// damit aus dem Fehlerdialog ablesbar ist, welche Pfade probiert wurden.
        /// </summary>
        internal static string NativeDiagnostics { get; private set; } = "";

        /// <summary>
        /// ML.NET laedt seine nativen Bibliotheken (v. a. CpuMathNative.dll) ueber die
        /// normale Windows-DLL-Suche. Die beginnt beim Ordner der EXE -- im Visio-Host
        /// also bei visio.exe statt beim Add-In-Ausgabeordner. Zusaetzlich kopiert die
        /// Kopier-Regel im NuGet-Paket (Microsoft.ML.CpuMath.props) die Native nur bei
        /// explizitem PlatformTarget x64/x86 -- dieses Projekt baut aber AnyCPU, daher
        /// legt erst ein eigener csproj-Eintrag sie unter NativeAssets\{x64,x86} ab.
        /// Hier wird die zur Prozess-Bitness passende Variante direkt vorgeladen
        /// (eine bereits geladene DLL findet jeder spaetere P/Invoke ueber den Namen).
        /// </summary>
        private static void PrepareNativeLibraryResolution()
        {
            if (_nativeResolutionPrepared) return;
            _nativeResolutionPrepared = true;

            var diag = new StringBuilder();
            try
            {
                diag.AppendLine("Prozess: " + (Environment.Is64BitProcess ? "64-Bit" : "32-Bit"));

                // VSTO laedt Add-In-Assemblies shadow-copied aus
                // %LOCALAPPDATA%\assembly\dl3\... -- Content-Dateien wie die Natives
                // werden dorthin NICHT mitkopiert. Assembly.Location zeigt auf den
                // Cache und ist daher nutzlos; CodeBase zeigt auf den urspruenglichen
                // Ablageort (Build-Output bzw. Installationsordner).
                Assembly asm = Assembly.GetExecutingAssembly();
                var baseDirs = new System.Collections.Generic.List<string>();
                try { baseDirs.Add(Path.GetDirectoryName(new Uri(asm.CodeBase).LocalPath)); }
                catch (Exception ex) { diag.AppendLine("CodeBase nicht lesbar: " + ex.Message); }
                baseDirs.Add(AppDomain.CurrentDomain.BaseDirectory?.TrimEnd('\\'));
                baseDirs.Add(Path.GetDirectoryName(asm.Location));

                string arch = Environment.Is64BitProcess ? "x64" : "x86";
                foreach (string baseDir in baseDirs.Distinct())
                {
                    if (string.IsNullOrEmpty(baseDir)) continue;
                    string[] candidates =
                    {
                        Path.Combine(baseDir, "NativeAssets", arch, "CpuMathNative.dll"),
                        Path.Combine(baseDir, "CpuMathNative.dll"),
                        Path.Combine(baseDir, "runtimes", "win-" + arch, "nativeassets", "netstandard2.0", "CpuMathNative.dll"),
                    };
                    foreach (string candidate in candidates)
                    {
                        if (!File.Exists(candidate))
                        {
                            diag.AppendLine("fehlt:  " + candidate);
                            continue;
                        }
                        if (LoadLibrary(candidate) != IntPtr.Zero)
                        {
                            SetDllDirectory(Path.GetDirectoryName(candidate));
                            diag.AppendLine("geladen: " + candidate);
                            return;
                        }
                        diag.AppendLine("Ladefehler (Win32 " + Marshal.GetLastWin32Error() + "): " + candidate);
                    }
                }
            }
            catch (Exception ex)
            {
                // Best effort -- schlaegt die Vorbereitung fehl, liefert das Training
                // selbst die volle Diagnose (siehe Initialize).
                diag.AppendLine("Vorbereitung fehlgeschlagen: " + ex.Message);
            }
            finally
            {
                NativeDiagnostics = diag.ToString();
            }
        }

        /// <summary>Loads the persisted model, training it from the bundled data if none exists yet.</summary>
        private void LoadOrTrainModel()
        {
            PrepareNativeLibraryResolution();
            Directory.CreateDirectory(AppDataDir);
            if (!File.Exists(ModelFilePath)) TrainModelFromBundledData();

            try
            {
                LoadModelFile();
            }
            catch
            {
                // Korruptes/inkompatibles Cache-Modell (z. B. aus einem frueher
                // abgebrochenen Lauf): einmal neu trainieren statt dauerhaft zu scheitern.
                TrainModelFromBundledData();
                LoadModelFile();
            }
        }

        private void LoadModelFile()
        {
            _model = _mlContext.Model.Load(ModelFilePath, out _);
            _predEngine = _mlContext.Model.CreatePredictionEngine<ShapeName, ShapeNamePrediction>(_model);
        }

        /// <summary>Retrains the model from the bundled training data and reloads it.</summary>
        public void Retrain()
        {
            PrepareNativeLibraryResolution();
            Directory.CreateDirectory(AppDataDir);
            TrainModelFromBundledData();
            LoadModelFile();
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

            result.AppendLine("Prüfung: Lokales ML-Modell (ML.NET)");
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
            if (ShouldValidate(componentType))
            {
                isValid = await Task.Run(() =>
                    _predEngine?.Predict(new ShapeName { Name = label, ShapeType = componentType })?.IsValid);
            }

            sb.AppendLine($"Shape ID: {shape.ID}");
            sb.AppendLine($"  Name: {shape.Name}");
            sb.AppendLine($"  Label: {label}");
            sb.AppendLine($"  ModelComponentType: {componentType}");

            if (isValid.HasValue)
                sb.AppendLine($"  ValidName: {(isValid.Value ? "VALID" : "INVALID")}");

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
