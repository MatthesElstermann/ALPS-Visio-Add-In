using System;
using System.IO;
using alps.net.api.parsing;
using NUnit.Framework;

namespace ALPS_Visio_AddIn_rewrite.Tests
{
    /// <summary>
    /// Regressionstests fuer <see cref="AlpsReaderWriterFactory"/>. Sie umgeht einen Bug im
    /// PASSReaderWriter-Konstruktor von alps.net.api 0.9.1.6: Der Ctor baut seinen Logpfad via
    /// <c>Directory.GetCurrentDirectory().Substring(0, path.IndexOf("bin"))</c> und wirft, wenn das
    /// Arbeitsverzeichnis kein "bin" enthaelt (im Visio-Host der Normalfall) -- was den OWL-Import
    /// komplett lahmlegte.
    ///
    /// PASSReaderWriter ist ein Singleton: Der (buggy) Ctor laeuft je AppDomain nur EINMAL. Damit der
    /// Fehlerfall wirklich ausgeloest wird -- und nicht nur ein frueherer Test die Instanz schon
    /// erzeugt hat --, laufen die Crash-Szenarien jeweils in einer FRISCHEN AppDomain.
    /// </summary>
    [TestFixture]
    public class AlpsReaderWriterFactoryTests
    {
        [Test]
        public void GetInstanceSafely_liefert_eine_Instanz()
        {
            Assert.That(AlpsReaderWriterFactory.GetInstanceSafely(), Is.Not.Null);
        }

        [Test]
        public void GetInstanceSafely_stellt_das_Arbeitsverzeichnis_wieder_her()
        {
            string original = Directory.GetCurrentDirectory();
            AlpsReaderWriterFactory.GetInstanceSafely();
            Assert.That(Directory.GetCurrentDirectory(), Is.EqualTo(original));
        }

        [Test]
        public void Vorbedingung_der_Library_Ctor_wirft_ohne_bin_im_Arbeitsverzeichnis()
        {
            // Belegt, dass das Szenario den Bug tatsaechlich ausloest. Ist er in einer neueren
            // alps.net.api behoben, ist der Workaround-Test unten nicht mehr aussagekraeftig.
            Exception error = RunInFreshDomainWithPlainCwd(CallLibraryDirectly);
            if (error == null)
                Assert.Inconclusive("alps.net.api wirft ohne \"bin\" im Arbeitsverzeichnis nicht mehr -- Bug behoben?");
        }

        [Test]
        public void GetInstanceSafely_wirft_nicht_wenn_das_Arbeitsverzeichnis_kein_bin_enthaelt()
        {
            // Ohne den Workaround wuerde der Library-Ctor hier -- Substring auf einen Pfad ohne
            // "bin" -- mit ArgumentOutOfRangeException sterben. Mit Workaround muss es laufen.
            Exception error = RunInFreshDomainWithPlainCwd(CallFactory);
            Assert.That(error, Is.Null, "GetInstanceSafely hat in frischer AppDomain geworfen: " + error);
        }

        // --- Hilfen: Aufrufe in einer frischen AppDomain (eigener, noch leerer Singleton) -------------

        private const string ErrorSlot = "error";

        private static void CallLibraryDirectly()
        {
            Capture(() => PASSReaderWriter.getInstance());
        }

        private static void CallFactory()
        {
            Capture(() => AlpsReaderWriterFactory.GetInstanceSafely());
        }

        private static void Capture(Func<PASSReaderWriter> call)
        {
            try
            {
                if (call() == null)
                    AppDomain.CurrentDomain.SetData(ErrorSlot, "Instanz ist null");
            }
            catch (Exception ex)
            {
                // Als String zurueckgeben: nicht jede Exception ist ueber AppDomain-Grenzen serialisierbar.
                AppDomain.CurrentDomain.SetData(ErrorSlot, ex.ToString());
            }
        }

        /// <summary>
        /// Setzt das (prozessweite) Arbeitsverzeichnis auf einen Ordner OHNE "bin" im Pfad und fuehrt
        /// die statische Methode (keine Lambda/Closure -- die waere nicht serialisierbar) in einer neuen AppDomain aus. Liefert den dort gefangenen Fehler oder null.
        /// </summary>
        private static Exception RunInFreshDomainWithPlainCwd(CrossAppDomainDelegate staticCallback)
        {
            string original = Directory.GetCurrentDirectory();
            string plainDir = Path.Combine(Path.GetTempPath(), "alps_test_plain_dir");
            Directory.CreateDirectory(plainDir);
            Assume.That(plainDir.IndexOf("bin", StringComparison.Ordinal), Is.EqualTo(-1),
                "Temp-Pfad enthaelt selbst \"bin\" -- Szenario hier nicht herstellbar.");

            var setup = new AppDomainSetup
            {
                ApplicationBase = TestContext.CurrentContext.TestDirectory,
                ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile,
            };
            AppDomain domain = AppDomain.CreateDomain("alps-factory-test-" + Guid.NewGuid(), null, setup);
            Directory.SetCurrentDirectory(plainDir);
            try
            {
                domain.DoCallBack(staticCallback);
                return domain.GetData(ErrorSlot) is string error ? new Exception(error) : null;
            }
            finally
            {
                Directory.SetCurrentDirectory(original);
                AppDomain.Unload(domain);
            }
        }
    }
}
