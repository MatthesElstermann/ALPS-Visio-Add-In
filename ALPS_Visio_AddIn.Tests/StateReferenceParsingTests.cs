using System.Collections.Generic;
using System.IO;
using System.Linq;
using alps.net.api.StandardPASS;
using ALPS_Visio_AddIn_rewrite.OWLShapes;
using NUnit.Framework;

namespace ALPS_Visio_AddIn_rewrite.Tests
{
    /// <summary>
    /// Kontrakt, auf dem StateImport aufbaut: alps.net.api wandelt eine geparste StateReference in
    /// einen gewoehnlichen (Visio-)State um und behaelt nur den Verweis auf den Ziel-State. Am
    /// Verweis erkennt der Import, dass der State als "State Reference" zu zeichnen ist. Aendert
    /// eine neue API-Version dieses Verhalten, schlaegt der Test an.
    /// </summary>
    [TestFixture]
    public class StateReferenceParsingTests
    {
        [Test]
        public void State_Reference_behaelt_den_Verweis_auf_den_Basis_State()
        {
            DirectoryInfo dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "docs"))) dir = dir.Parent;
            if (dir == null) Assert.Ignore("docs-Ordner nicht gefunden.");

            var io = AlpsReaderWriterFactory.GetInstanceSafely();
            alps.net.api.ReflectiveEnumerator.addAssemblyToCheckForTypes(typeof(VisioClassFactory).Assembly);
            io.setModelElementFactory(new VisioClassFactory());
            io.loadOWLParsingStructure(new List<string>
            {
                Path.Combine(dir.FullName, "ALPS_Visio_AddIn-rewrite", "Resources", "standard_PASS_ont_v_1.1.0.owl"),
                Path.Combine(dir.FullName, "ALPS_Visio_AddIn-rewrite", "Resources", "ALPS_ont_v_0.8.0.owl"),
            });
            var model = io.loadModels(new List<string> { Path.Combine(dir.FullName, "docs", "[Test]_ALPS_Layered.owl") })[0];

            IState reference = model.getAllElements().Values.OfType<IState>()
                .Single(state => state.getModelComponentID() == "G_WorkRef");

            Assert.That(reference, Is.InstanceOf<IVisioImportable>(), "Die Visio-Klassen muessen greifen.");
            Assert.That(((IStateReference)reference).getReferencedState()?.getModelComponentID(), Is.EqualTo("W_DoState"));
        }
    }
}
