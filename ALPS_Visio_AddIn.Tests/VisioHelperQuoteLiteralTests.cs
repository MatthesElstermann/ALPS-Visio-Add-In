using NUnit.Framework;

namespace ALPS_Visio_AddIn_rewrite.Tests
{
    /// <summary>
    /// Tests fuer <see cref="VisioHelper.QuoteLiteral(object)"/> -- eine reine String-Funktion, die
    /// Werte als Visio-ShapeSheet-String-Literal umschliesst und eingebettete Anfuehrungszeichen durch
    /// Verdoppeln escaped. Fehlendes Escaping war historisch eine Fehlerquelle (ungueltige
    /// ShapeSheet-Formeln), deshalb der ideale erste Testkandidat -- ohne jede Visio-Abhaengigkeit.
    /// </summary>
    [TestFixture]
    public class VisioHelperQuoteLiteralTests
    {
        [Test]
        public void Harness_laeuft()
        {
            // Rein NUnit, ohne Bezug zum Add-In: schlaegt dieser Test fehl, ist die Test-Infrastruktur
            // (Adapter/Runner) das Problem -- nicht der Projektverweis oder der Testgegenstand.
            Assert.Pass();
        }

        [Test]
        public void Umschliesst_einfachen_Text_mit_Anfuehrungszeichen()
        {
            Assert.That(VisioHelper.QuoteLiteral("Kunde"), Is.EqualTo("\"Kunde\""));
        }

        [Test]
        public void Verdoppelt_ein_eingebettetes_Anfuehrungszeichen()
        {
            // a"b  ->  "a""b"   (das innere " wird zu "")
            Assert.That(VisioHelper.QuoteLiteral("a\"b"), Is.EqualTo("\"a\"\"b\""));
        }

        [Test]
        public void Null_wird_zu_leerem_Literal()
        {
            Assert.That(VisioHelper.QuoteLiteral(null), Is.EqualTo("\"\""));
        }

        [Test]
        public void Leerer_String_wird_zu_leerem_Literal()
        {
            Assert.That(VisioHelper.QuoteLiteral(string.Empty), Is.EqualTo("\"\""));
        }

        [Test]
        public void Verdoppelt_mehrere_Anfuehrungszeichen()
        {
            // ""  ->  """""" (zwei " werden zu vier, plus die zwei umschliessenden)
            Assert.That(VisioHelper.QuoteLiteral("\"\""), Is.EqualTo("\"\"\"\"\"\""));
        }

        [Test]
        public void Nicht_string_Werte_werden_ueber_ToString_umgesetzt()
        {
            Assert.That(VisioHelper.QuoteLiteral(42), Is.EqualTo("\"42\""));
        }
    }
}
