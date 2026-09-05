using System.IO;
using ALPS_Visio_AddIn_rewrite.Verification;
using NUnit.Framework;

namespace ALPS_Visio_AddIn_rewrite.Tests
{
    /// <summary>
    /// Integrationstests fuer die ALPS-Verification (<see cref="Verifier.Verify"/>): laedt eine
    /// Spezifikations- und eine Implementierungs-OWL und fuehrt die SID-Checks aus. Laeuft KOMPLETT
    /// ohne Visio -- die Verification nutzt die schlichten alps.net.api-Klassen, kein COM-Zeichnen.
    /// Damit deckt EIN Test die ganze Parse-und-Pruef-Pipeline ab. Die Beispielmodelle liegen in
    /// docs/ ([Verif]_Spec_* / [Verif]_Impl_*).
    /// </summary>
    [TestFixture]
    public class VerifierTests
    {
        /// <summary>
        /// Sucht den docs/-Ordner, indem vom Testverzeichnis (…\bin\Debug\net48) aufwaerts gegangen
        /// wird, bis ein Verzeichnis mit einem docs/-Unterordner gefunden ist.
        /// </summary>
        private static string DocsDir()
        {
            DirectoryInfo dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "docs")))
                dir = dir.Parent;
            if (dir == null)
                Assert.Ignore("docs-Ordner nicht gefunden -- Beispiel-OWLs nicht auffindbar.");
            return Path.Combine(dir.FullName, "docs");
        }

        [TestCase("[Verif]_Spec_AbstractModel.owl", "[Verif]_Impl_ImplementingModel.owl")]
        [TestCase("[Verif]_Spec_CustomerIsKing.owl", "[Verif]_Impl_CustomerIsKing.owl")]
        public void Verify_liefert_einen_Report_fuer_die_Beispielmodelle(string specFile, string implFile)
        {
            string docs = DocsDir();
            string spec = Path.Combine(docs, specFile);
            string impl = Path.Combine(docs, implFile);
            Assert.That(File.Exists(spec), Is.True, "Spec-OWL fehlt: " + spec);
            Assert.That(File.Exists(impl), Is.True, "Impl-OWL fehlt: " + impl);

            string report = Verifier.Verify(spec, impl);

            Assert.That(report, Is.Not.Null.And.Not.Empty);
            // Strukturelle Marker: beweisen, dass beide Modelle geladen und die SID-Checks
            // ausgefuehrt wurden (unabhaengig vom konkreten Spec-vs-Impl-Ergebnis).
            Assert.That(report, Does.Contain("Subject Implementation"),
                "Erwarteter Abschnitt fehlt -- wurden beide Modelle geladen?");
            Assert.That(report, Does.Contain("Communication Restrictions"),
                "Der SID-Restriktions-Check scheint nicht gelaufen zu sein.");
            // Das Gesamtergebnis muss am Report-Ende stehen und ein eindeutiges Verdict nennen.
            Assert.That(report, Does.Contain("GESAMTERGEBNIS"),
                "Das Gesamtergebnis fehlt am Report-Ende.");
            Assert.That(report, Does.Contain("VERDICT: BESTANDEN").Or.Contain("VERDICT: NICHT BESTANDEN"),
                "Es wurde kein eindeutiges Verdict ausgegeben.");
        }
    }
}
