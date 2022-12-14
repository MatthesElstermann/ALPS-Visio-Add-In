using Microsoft.Office.Tools.Ribbon;

namespace VisioAddIn
{
    public partial class ALPSRibbon
    {


        private void Ribbon1_Load(object sender, RibbonUIEventArgs e)
        {
            this.openShapes.Image = VisioAddIn.Properties.Resources.openDocument7;
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

    }
}
