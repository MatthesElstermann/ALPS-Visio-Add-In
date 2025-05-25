using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using Microsoft.Office.Tools.Ribbon;

namespace ALPS_Visio_AddIn_rewrite
{
    public partial class ALPSRibbon
    {


        private void ALPSRibbon_Load(object sender, RibbonUIEventArgs e)
        {
        }

        private void loadOWLFile_Click(object sender, RibbonControlEventArgs e)
        {
            Globals.ThisAddIn.loadOWLFile();
        }

    }
}
