#nullable enable
using System.Collections.Generic;
using System.IO;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;

namespace PassBpmnConverter.Pass;

/// <summary>
/// Laedt ein PASS-Modell aus einer OWL-Datei fuer die BPMN-Konvertierung. Gegenueber dem
/// Original (das die Ontologien von einem relativen "resources/"-Pfad las) an das Add-In
/// angepasst: die gebuendelten Ontologie-Ressourcen werden nach %TEMP% extrahiert, der
/// PASSReaderWriter kommt ueber <see cref="ALPS_Visio_AddIn_rewrite.AlpsReaderWriterFactory"/>
/// (CWD-Workaround fuer den Singleton-Konstruktor), und es wird explizit die Basis-Factory
/// gesetzt, damit nicht die Visio-Klassen des letzten Imports instanziiert werden.
/// </summary>
public static class PassParser
{
    public static IList<IPASSProcessModel> LoadModels(IList<string> filepaths)
    {
        IPASSReaderWriter io = ALPS_Visio_AddIn_rewrite.AlpsReaderWriterFactory.GetInstanceSafely();

        // Plain alps.net.api-Klassen statt der VisioClassFactory des OWL-Imports:
        // fuer die Konvertierung wird nur das Datenmodell gebraucht, keine Shapes.
        io.setModelElementFactory(new BasicPASSProcessModelElementFactory());

        io.loadOWLParsingStructure(
            new List<string>
            {
                ExtractOntology("standard_PASS_ont_v_1.1.0.owl",
                    ALPS_Visio_AddIn_rewrite.Properties.Resources.standard_PASS_ont_v_1_1_0),
                ExtractOntology("ALPS_ont_v_0.8.0.owl",
                    ALPS_Visio_AddIn_rewrite.Properties.Resources.ALPS_ont_v_0_8_0),
            }
        );

        IList<IPASSProcessModel> models = io.loadModels(filepaths);

        return models;
    }

    private static string ExtractOntology(string fileName, byte[] content)
    {
        string path = Path.Combine(Path.GetTempPath(), fileName);
        File.WriteAllBytes(path, content);
        return path;
    }
}
