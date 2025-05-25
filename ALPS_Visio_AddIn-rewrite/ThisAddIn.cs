using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Visio = Microsoft.Office.Interop.Visio;
using Office = Microsoft.Office.Core;
using Microsoft.Office.Interop.Visio;
using System.Windows.Forms;
using System.Diagnostics;

namespace ALPS_Visio_AddIn_rewrite
{
    public partial class ThisAddIn
    {

        private Visio.Document activeDoc;

        private static ThisAddIn currentInstance;
        public static ThisAddIn getInstance() {return currentInstance;}

        private void ThisAddIn_Startup(object sender, System.EventArgs e)
        {
            currentInstance = this;

            // create buttons
            new ALPSRibbon();

            // add update triggers
            Application.DocumentCreated += Application_DocumentCreated;
            Application.WindowActivated += Application_WindowActivated;
            Application.DocumentOpened += Application_DocumentOpened;

            // set current active document
            this.setActiveDocument();
        }

        private void ThisAddIn_Shutdown(object sender, System.EventArgs e)
        {
        }

        // ---------------------------------------------------------------------

        private void setActiveDocument()
        {
            this.activeDoc = Application.ActiveDocument;
        }

        private static string showFileDialog()
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            // set filter
            openFileDialog1.Filter = "Ontology Files (.owl)|*.owl|RDF Files (*.rdf)|*.rdf";

            openFileDialog1.ShowDialog();

            // return selected file name
            return openFileDialog1.FileName;
        }

        #region Ribbon calls

        // ---------------------------------------------------------------------

        public void loadOWLFile()
        {

            string fileName = showFileDialog();
            Document newDoc = Application.Documents.Add("");

            // Create new importer with file
            OWLImporter importer = new OWLImporter(fileName);
            VisioHelper.openStencil(VisioHelper.VisioStencils.SID_STENCIL); ;

            //Parse the first page in the document
            if (Application.ActiveDocument.Pages.Count > 0)
                importer.parse(VisioHelper.getPageInPages(Application.ActiveDocument.Pages, 0), activeDoc);
        }

        #endregion

        #region Visio triggers

        /* ---------------------------------------------------------------------
         *                 Methods triggered by changes in Visio 
         * --------------------------------------------------------------------- */

        /// <summary>
        /// Called when the active window in the document changes.
        /// Checks whether the active document is still the same or not.
        /// </summary>
        /// <param name="window">The active window, not used by this function</param>
        private void Application_WindowActivated(Window window)
        {
            this.setActiveDocument();
        }

        private void Application_DocumentOpened(IVDocument doc)
        {
            this.setActiveDocument();
        }

        private void Application_DocumentCreated(IVDocument doc)
        {
            this.setActiveDocument();
        }

        #endregion

        #region Von VSTO generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InternalStartup()
        {
            this.Startup += new System.EventHandler(ThisAddIn_Startup);
            this.Shutdown += new System.EventHandler(ThisAddIn_Shutdown);
        }
        
        #endregion
    }
}
