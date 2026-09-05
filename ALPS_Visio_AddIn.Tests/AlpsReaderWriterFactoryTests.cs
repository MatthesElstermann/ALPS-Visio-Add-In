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
    /// Geprueft werden die beobachtbaren Zusicherungen der Factory: liefert eine Instanz ohne Wurf und
    /// stellt das Arbeitsverzeichnis wieder her. Hinweis: PASSReaderWriter ist ein Singleton, der
    /// (buggy) Ctor laeuft je Testprozess nur EINMAL -- diese Tests pruefen daher die Contracts der
    /// Factory, nicht wiederholt den Ctor-Crash selbst.
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
        public void GetInstanceSafely_wirft_nicht_wenn_das_Arbeitsverzeichnis_kein_bin_enthaelt()
        {
            string original = Directory.GetCurrentDirectory();
            string plainDir = Path.Combine(Path.GetTempPath(), "alps_test_plain_dir");
            Directory.CreateDirectory(plainDir);
            Directory.SetCurrentDirectory(plainDir);
            try
            {
                // Ohne den Workaround wuerde der Library-Ctor hier -- Substring auf einen Pfad ohne
                // "bin" -- mit ArgumentOutOfRangeException sterben. Mit Workaround muss es laufen.
                Assert.That(AlpsReaderWriterFactory.GetInstanceSafely(), Is.Not.Null);
            }
            finally
            {
                Directory.SetCurrentDirectory(original);
            }
        }
    }
}
