using ALPS_Visio_AddIn_rewrite.OWLShapes;
using NUnit.Framework;

namespace ALPS_Visio_AddIn_rewrite.Tests
{
    /// <summary>
    /// Tests fuer <see cref="PASSProcessModelElementImport.RowNameSuffix(string)"/>: Sprach-Tags der
    /// Labels werden Teil eines ShapeSheet-Zeilennamens ("lable" + Suffix). Ein Bindestrich wie in
    /// "de-DE" ist dort ungueltig — AddNamedRow warf und der Import des Elements brach ab.
    /// </summary>
    [TestFixture]
    public class LabelRowNameTests
    {
        [TestCase("de", "DE")]
        [TestCase("de-DE", "DE_DE")]
        [TestCase("zh-Hans-CN", "ZH_HANS_CN")]
        [TestCase("x.y z", "XYZ")]
        public void Sprach_Tag_wird_zu_gueltigem_Zeilennamen(string tag, string expected)
        {
            Assert.That(PASSProcessModelElementImport.RowNameSuffix(tag), Is.EqualTo(expected));
        }

        [TestCase(null)]
        [TestCase("")]
        public void Fehlender_Tag_ergibt_leeren_Suffix(string tag)
        {
            Assert.That(PASSProcessModelElementImport.RowNameSuffix(tag), Is.Empty);
        }
    }
}
