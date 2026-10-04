using System.Collections.Generic;
using System.IO;
using System.Linq;
using alps.net.api.ALPS;
using alps.net.api.StandardPASS;
using NUnit.Framework;

namespace ALPS_Visio_AddIn_rewrite.Tests
{
    /// <summary>
    /// Sichert die Testdatei [Test]_ALPS_Elements.owl ab: Kanal und Restriktion brauchen beide
    /// Endpunkte (sonst legt der Import die Verbinder unverbunden neben die Seite), und die
    /// Extensions muessen auf einer Erweiterungsebene liegen (auf der Basisebene verwirft die
    /// Schablonen-VBA Guard- und Macro-Extensions).
    /// </summary>
    [TestFixture]
    public class AlpsElementsTestFileTests
    {
        /// <summary>Laedt die Datei so wie der OWL-Import (Visio-Klassen-Factory).</summary>
        private static IPASSProcessModel LoadElementsModel()
        {
            DirectoryInfo dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "docs"))) dir = dir.Parent;
            if (dir == null) Assert.Ignore("docs-Ordner nicht gefunden.");

            var io = AlpsReaderWriterFactory.GetInstanceSafely();
            alps.net.api.ReflectiveEnumerator.addAssemblyToCheckForTypes(typeof(OWLShapes.VisioClassFactory).Assembly);
            io.setModelElementFactory(new OWLShapes.VisioClassFactory());
            io.loadOWLParsingStructure(new List<string>
            {
                Path.Combine(dir.FullName, "ALPS_Visio_AddIn-rewrite", "Resources", "standard_PASS_ont_v_1.1.0.owl"),
                Path.Combine(dir.FullName, "ALPS_Visio_AddIn-rewrite", "Resources", "ALPS_ont_v_0.8.0.owl"),
            });
            return io.loadModels(new List<string> { Path.Combine(dir.FullName, "docs", "[Test]_ALPS_Elements.owl") })[0];
        }

        [Test]
        public void Kanal_und_Restriktion_haben_beide_Endpunkte()
        {
            var model = LoadElementsModel();

            var channel = model.getAllElements().Values.OfType<ICommunicationChannel>().Single();
            Assert.That(channel.getCorrespondentA(), Is.Not.Null);
            Assert.That(channel.getCorrespondentB(), Is.Not.Null);

            var restriction = model.getAllElements().Values.OfType<ICommunicationRestriction>().Single();
            Assert.That(restriction.getCorrespondentA(), Is.Not.Null);
            Assert.That(restriction.getCorrespondentB(), Is.Not.Null);
        }

        /// <summary>
        /// Je Extension-Typ eine eigene Erweiterungsebene: alps.net.api leitet den Ebenen-Typ aus
        /// dem Inhalt ab und laesst je Ebene nur einen Typ zu — gemischt fielen Guard- und
        /// Macro-Extension beim Parsen stillschweigend weg.
        /// </summary>
        [TestCase("Layer_Ext", typeof(ISubjectExtension))]
        [TestCase("Layer_Guard", typeof(IGuardExtension))]
        [TestCase("Layer_Macro", typeof(IMacroExtension))]
        public void Jede_Extension_liegt_auf_ihrer_Erweiterungsebene(string layerId, System.Type extensionType)
        {
            var model = LoadElementsModel();
            var layer = model.getAllElements().Values.OfType<IModelLayer>()
                .Single(l => l.getModelComponentID() == layerId);

            Assert.That(layer.getElements().Values.Count(extensionType.IsInstanceOfType), Is.EqualTo(1),
                "Inhalt von " + layerId + ": " + string.Join(", ", layer.getElements().Values
                    .Select(e => e.getModelComponentID() + ":" + e.GetType().Name)));
        }
    }
}
