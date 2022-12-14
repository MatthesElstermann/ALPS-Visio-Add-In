using System;
using System.Collections.Generic;

namespace VisioAddIn.SiSi.GUI
{
    class SiSi_CockpitController
    {
        SiSi_CockpitWindow window;
        string output;
        string outputItem = "Output";
        string errorLogItem = "*** Show Error Log ***";
        Microsoft.Office.Interop.Visio.Page page;
        Dictionary<string, Microsoft.Office.Interop.Visio.Shape> subjects;

        public SiSi_CockpitController(ThisAddIn addin)
        {
            window = new SiSi_CockpitWindow(this);
            window.Show();

            window.setComboBoxEnabeld(false);

            window.addComboBoxItem(outputItem);
            window.setComboxBoxSelection(outputItem);
            window.addComboBoxItem(errorLogItem);

            page = (Microsoft.Office.Interop.Visio.Page)addin.Application.ActivePage;
            string pageType = "";
            subjects = new Dictionary<string, Microsoft.Office.Interop.Visio.Shape>();

            if (page.PageSheet.CellExistsU["Prop." + ALPSConstants.alpsPropertieTypePageType, 1] != 0)
            {
                pageType = page.PageSheet.CellsU["Prop." + ALPSConstants.alpsPropertieTypePageType].ResultStr["none"].Replace("\"", "");
            }
            else
            {
                System.Windows.MessageBox.Show("Warning! No Active PASS Model found!");
                window.Close();
                //This Class should also be terminated
            }

            if (pageType == ALPSConstants.alpsPropertieValueSBDPage)
            {
                if (page.PageSheet.CellExistsU["Hyperlink." + ALPSConstants.alpsHyperlinksLinkedSIDPage, 1] != 0)
                {
                    string pageSubadress = page.PageSheet.Hyperlinks.ItemU[ALPSConstants.alpsHyperlinksLinkedSIDPage].SubAddress;
                    if (pageSubadress != "")
                    {
                        page = addin.Application.ActiveDocument.Pages.ItemU[pageSubadress];
                    }
                }
            }

            foreach (Microsoft.Office.Interop.Visio.Shape shape in page.Shapes)
            {
                if (shape.HasCategory(ALPSConstants.alpsShapeCategorySIDactorWithSBD) | shape.HasCategory(ALPSConstants.alpsShapeCategoryInterfaceActor))
                {
                    string subjectName = shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeLabel].ResultStr["none"].Replace("\"", "");

                    //subjectNameField???
                    subjects.Add(subjectName, shape);
                    window.addComboBoxItem(subjectName);
                }
            }
        }



        public void writeInOutput(string line)
        {
            output += Environment.NewLine + line;
            window.addTextLine(line);
            window.Refresh();

        }

        public void cleanTextBox()
        {
            writeInTextBox("");
        }

        public void writeInTextBox(string text)
        {
            window.setText(text);
        }

        public void writeLineInTextBox(string line)
        {
            window.addTextLine(line);
        }

        internal void startSimulation_Clicked()
        {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            SiSi_SimpleSim.resetSimulation();
            output = "";
            window.setText("");

            int recursionDepth = Int32.Parse(window.getParameterRecursionDepth());
            if (recursionDepth < 1)
            {
                System.Windows.MessageBox.Show("Found invalid Recursion Depth Value. Recursion Depth set to 1");
                recursionDepth = 1;
            }
            int minMaxSigma = Int32.Parse(window.getParameterSigma());
            if (minMaxSigma < 0)
            {
                System.Windows.MessageBox.Show("Found invalid minMaxSigma Sigma Value. Min/Max Sigma Value set to 0");
                minMaxSigma = 0;
            }

            SiSi_SimpleSim.setMaxRecursionDepth(recursionDepth);
            SiSi_SimpleSim.setNumberOfSigmasForMinMax(minMaxSigma);
            SiSi_SimpleSim.setUseAbsolutMinMax(window.getParameterAbsolutMinMax());
            SiSi_SimpleSim.setWriteWaitingTimeToReceiveStates(window.getParameterWriteWaitingTimes());

            writeInOutput("Starting Simulation, Please Wait.");
            writeInOutput("This may take some time");

            SiSi_SimpleSim.startSimulationForSIDPage(page);

            watch.Stop();

            writeInOutput("Runtime of the Simulation: " + watch.Elapsed.ToString());

            window.setComboBoxEnabeld(true);
        }

        internal void showErrorLog_Clicked()
        {
            showErrorLog();
        }

        private void showErrorLog()
        {
            SiSi_ReportMessageDisplayController controller = new SiSi_ReportMessageDisplayController();
        }

        internal void comboBoxSelectionChanged()
        {
            window.setText("");
            string subjectName = window.getComboBoxSelection();
            if (subjectName == outputItem)
            {
                window.setText(output);

            }
            else if (subjectName == errorLogItem)
            {
                showErrorLog();
            }
            else
            {
                showSubjectDataOnScreen(subjectName);
            }
            window.Refresh();
        }

        private void showSubjectDataOnScreen(string subjectName)
        {
            SiSi_Subject sisiSubject = SiSi_SimpleSim.getSiSiSubject(subjectName);

            if (sisiSubject != null)
            {
                writeLineInTextBox("Simulation Results for: " + sisiSubject.getSubjectName());
                writeLineInTextBox("_____________________________________________________________________");
                writeLineInTextBox("");
                writeLineInTextBox("  Average Overall Active Time:  ");
                if (sisiSubject.getActiveDuration() != null)
                {
                    writeLineInTextBox(sisiSubject.getActiveDuration().toString(true));
                }

                writeLineInTextBox("");
                writeLineInTextBox("  Average Inactive/Waiting Time:  ");
                if (sisiSubject.getInactiveDuration() != null)
                {
                    writeLineInTextBox(sisiSubject.getInactiveDuration().toString(true));
                }

                writeLineInTextBox("");
                writeLineInTextBox("  Overall Time until first Activation:  ");
                if (sisiSubject.getWaitingTimeUntilActivation() != null)
                {
                    writeLineInTextBox(sisiSubject.getWaitingTimeUntilActivation().toString(true));
                }

                writeLineInTextBox("");
                writeLineInTextBox(" Active Time known for sure: " + sisiSubject.activeDurationKnown());
                writeLineInTextBox("");
                writeLineInTextBox(" First Send Times known: " + sisiSubject.allFirstSendPathsPerfectlyKnown());
                writeLineInTextBox("");
                writeLineInTextBox(" Response Times known: " + sisiSubject.allResponsePathsPerfectlyKnown());
                writeLineInTextBox("");

                if (window.getOutputReportResponseObjects())
                {
                    writeLineInTextBox(sisiSubject.allResponseObjectsToString());
                }
                writeLineInTextBox("");
                if (window.getOutputReportFirstSendObjects())
                {
                    writeLineInTextBox(sisiSubject.allFirstSendPathsToString());
                }


            }
        }

    }
}

