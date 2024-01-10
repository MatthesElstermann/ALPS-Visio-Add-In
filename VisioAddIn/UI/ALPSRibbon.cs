using alps.net.api;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using Microsoft.Office.Tools.Ribbon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using VisioAddIn.OwlShapes;


namespace VisioAddIn
{
    public partial class ALPSRibbon
    {


        private void Ribbon1_Load(object sender, RibbonUIEventArgs e)
        {
            this.openShapes.Image = VisioAddIn.Properties.Resources.document_open_7;
            this.showDirectory.Image = VisioAddIn.Properties.Resources.pageSetup;
        }

        private void dropDown1_SelectionChanged(object sender, RibbonControlEventArgs e)
        {

        }

        private void openShapes_Click(object sender, RibbonControlEventArgs e)
        {
            Globals.ThisAddIn.openShapesClicked();
        }

        private void showDirectoryClicked(object sender, RibbonControlEventArgs e)
        {
            Globals.ThisAddIn.showDirectoryClicked();
        }

        private void buttonCreateFromFile_Click(object sender, RibbonControlEventArgs e)
        {
            Globals.ThisAddIn.createGraphFromOwlClicked();
        }

        // Test-method with 0 references, delete?
        private void button1_Click(object sender, RibbonControlEventArgs e)
        {
            Globals.ThisAddIn.testButtonClicked();
        }

        // Test-method with 0 references, delete?
        private void button1_Click_1(object sender, RibbonControlEventArgs e)
        {
            Globals.ThisAddIn.versionButtonClicked();
        }

        // Test-method with 0 references, delete?
        private void button2_Click(object sender, RibbonControlEventArgs e)
        {

        }

        private void button1_Click_2(object sender, RibbonControlEventArgs e)
        {
            System.Windows.MessageBox.Show("Hello World 3 from C#");
            Debug.Print("Debug Printout");
            Debug.WriteLine("write a line");
            VisioHelper.toggleVBAListeners();
            //tryMethod();

        }

        private void tryMethod()
        {
            Debug.WriteLine("Trymethod Loading models");
            ReflectiveEnumerator.addAssemblyToCheckForTypes(Assembly.GetExecutingAssembly());
            IPASSReaderWriter owlGraph = PASSReaderWriter.getInstance();
            owlGraph.setModelElementFactory(new VisioClassFactory());
            IList<string> paths = new List<string>
            {
               "C:\\Users\\qs0196\\source\\repos\\alps.net.api\\src\\standard_PASS_ont_v_1.1.0.owl",
               "C:\\Users\\qs0196\\source\\repos\\alps.net.api\\src\\ALPS_ont_v_0.8.0.owl",
            };
            owlGraph.loadOWLParsingStructure(paths);
            IList<IPASSProcessModel> models = owlGraph.loadModels(new List<string> { "C:\\Users\\qs0196\\source\\repos\\alps.net.api\\src\\ExportImportTestSimple.owl" });
            IDictionary<string, IPASSProcessModelElement> allElements = models[0].getAllElements();
            ICollection<IPASSProcessModelElement> onlyElements = models[0].getAllElements().Values;
            IList<BasicPASSProcessModelElementFactory> onlyAdditionalFunctionalityElements = models[0].getAllElements().Values.OfType<BasicPASSProcessModelElementFactory>().ToList();
            Debug.WriteLine("Number ob Models loaded: " + models.Count);
            Debug.WriteLine("Found " + onlyAdditionalFunctionalityElements.Count +
                              " AdditionalFunctionalityElements in First model!");

        }

    }
}
