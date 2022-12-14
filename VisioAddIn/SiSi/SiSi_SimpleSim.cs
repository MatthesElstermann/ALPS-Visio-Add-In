using Microsoft.Office.Interop.Visio;
using System;
using System.Collections.Generic;
using VisioAddIn.SiSi;
using VisioAddIn.SiSi.GUI;

namespace VisioAddIn
{
    class SiSi_SimpleSim
    {
        private static ThisAddIn addin;

        public static DateTime start, stop;
        static TimeSpan simulationTime;

        static Dictionary<string, SiSi_Subject> interfaceSubjects;
        static Dictionary<string, SiSi_Subject> internalSubjects;
        static Dictionary<string, SiSi_Distribution> messageTransmissionDurationDictionary;

        internal static void addReportMessage(SiSi_ReportMessage siSi_ReportMessage)
        {
            if (reportMessageList == null)
            {
                reportMessageList = new List<SiSi_ReportMessage>();
            }
            reportMessageList.Add(siSi_ReportMessage);
        }

        private static int maxRecursionDepth;
        private static int numberOfSigmasForMinMax;
        private static bool useAbsolutMinMax;
        private static bool writeWaitingTimeToReceiveStates;
        private static SiSi_Distribution defaultResponseTimeForInterfaceReplies;

        private static bool isErrorReportRun;
        private static List<string> collectionOfProblems;
        private static List<SiSi_ReportMessage> reportMessageList;
        private static byte maximumNumberOfResponseContextSkips;


        public static void setAddin(ThisAddIn newAddin)
        {
            addin = newAddin;
        }

        public static void setMaxRecursionDepth(int max)
        {
            maxRecursionDepth = max;
        }

        public static void setNumberOfSigmasForMinMax(int number)
        {
            numberOfSigmasForMinMax = number;
        }

        public static void setUseAbsolutMinMax(bool use)
        {
            useAbsolutMinMax = use;
        }

        public static void setWriteWaitingTimeToReceiveStates(bool write)
        {
            writeWaitingTimeToReceiveStates = write;
        }


        public static void resetSimulation()
        {
            interfaceSubjects = new Dictionary<string, SiSi_Subject>();
            internalSubjects = new Dictionary<string, SiSi_Subject>();
            messageTransmissionDurationDictionary = new Dictionary<string, SiSi_Distribution>();
            collectionOfProblems = new List<string>();
            reportMessageList = new List<SiSi_ReportMessage>();
            isErrorReportRun = false;
            defaultResponseTimeForInterfaceReplies = new SiSi_Distribution();
            defaultResponseTimeForInterfaceReplies.setWellKnownDuration(true);
            maxRecursionDepth = 3;
            numberOfSigmasForMinMax = 6;
            maximumNumberOfResponseContextSkips = 5;
        }

        internal static void startSimpleSimClicked()
        {

            SiSi_CockpitController controller = new SiSi_CockpitController(addin);
        }

        public static void runSimulationForCurrentDokument()
        {
            start = DateTime.Now;

            interfaceSubjects = new Dictionary<string, SiSi_Subject>();
            internalSubjects = new Dictionary<string, SiSi_Subject>();
            messageTransmissionDurationDictionary = new Dictionary<string, SiSi_Distribution>();
            collectionOfProblems = new List<string>();
            reportMessageList = new List<SiSi_ReportMessage>();
            isErrorReportRun = false;
            maxRecursionDepth = 3;
            numberOfSigmasForMinMax = 6;
            maximumNumberOfResponseContextSkips = 5;



            startSimulationForSIDPage(addin.Application.ActivePage);

            printSubjects();

            reIterateSubjectsToDetermineRuntime();

            printSubjects();

            stop = DateTime.Now;
            simulationTime = stop - start;

        }

        public static void startSimulationForSIDPage(Page sidPage)
        {
            start = DateTime.Now;

            if (sidPage.PageSheet.CellExistsU["Prop." + ALPSConstants.alpsPropertieTypePageType, 1] == 0)
            {
                System.Windows.Forms.MessageBox.Show("Please select an SID Page to execute the simulation");
                return;
            }
            if (sidPage.PageSheet.CellsU["Prop." + ALPSConstants.alpsPropertieTypePageType].Formula == ALPSConstants.alpsPropertieValueSIDPage)
            {
                System.Windows.Forms.MessageBox.Show("Please select an SID Page to execute the simulation");
                return;
            }

            addErrorMessageToReportCollection("##########################################################");
            addErrorMessageToReportCollection("### Construction Problems ################################");
            addErrorMessageToReportCollection("##########################################################");

            foreach (Shape shape in sidPage.Shapes)
            {
                if (shape.HasCategory(ALPSConstants.alpsShapeCategorySIDactorWithSBD))
                {
                    SiSi_Subject tempSiSiSubject = new SiSi_Subject();
                    //ToDo: Output: "Analysing Subject: ..."
                    tempSiSiSubject.initalizeSubject(shape);
                    internalSubjects.Add(tempSiSiSubject.getSubjectName().Replace("\"", ""), tempSiSiSubject);

                }
                else if (shape.HasCategory(ALPSConstants.alpsShapeCategoryInterfaceActor))
                {
                    SiSi_Subject tempSiSiSubject = new SiSi_Subject();
                    //ToDo: Output "Analysing Interface: ..."
                    tempSiSiSubject.initializeInterface(shape);
                    interfaceSubjects.Add(tempSiSiSubject.getSubjectName(), tempSiSiSubject);
                }
                else if (shape.HasCategory(ALPSConstants.alpsShapeCategorySIDMessage))
                {
                    //ToDo: Output "Analysing Message: ..."
                    getTransmissionTimeFromMessageShape(shape);
                }
            }

            addErrorMessageToReportCollection("");
            addErrorMessageToReportCollection("");
            addErrorMessageToReportCollection("##########################################################");
            addErrorMessageToReportCollection("### Additional Problems ##################################");
            addErrorMessageToReportCollection("##########################################################");

            reIterateSubjectsToDetermineRuntime();

            stop = DateTime.Now;
            simulationTime = stop - start;
            //Todo: Output: "Simulation run done!"
        }

        internal static List<SiSi_ReportMessage> getReportMessageList()
        {
            return reportMessageList;
        }

        public static void printSubjects() //No Output yet. Need Way to Output Data in Realtime!!
        {
            foreach (string key in internalSubjects.Keys)
            {
                SiSi_Subject subject;
                internalSubjects.TryGetValue(key, out subject);
                subject.calculateMyActiveDuration();
            }

            foreach (string key in interfaceSubjects.Keys)
            {
                SiSi_Subject subject;
                interfaceSubjects.TryGetValue(key, out subject);
                subject.calculateMyActiveDuration();
            }
        }

        private static void reIterateSubjectsToDetermineRuntime()
        {
            //ToDo: Output: "### reiterating ###"

            int numberOfSubjects = internalSubjects.Count + interfaceSubjects.Count;
            bool allSubjectsSet = false;
            bool allKnown = true;

            for (long i = 0; i <= numberOfSubjects + 2; i++)
            {
                allKnown = true;

                foreach (string key in internalSubjects.Keys)
                {
                    SiSi_Subject temSiSiSubject;
                    internalSubjects.TryGetValue(key, out temSiSiSubject);
                    if (!temSiSiSubject.allResponsePathsPerfectlyKnown())
                    {
                        allKnown = false;
                        temSiSiSubject.tryToCalculateTimesAndChancesForAllResponsePaths();
                    }
                    if (!temSiSiSubject.allFirstSendPathsPerfectlyKnown())
                    {
                        allKnown = false;
                        temSiSiSubject.tryToCalculateTimesAndChancesForAllFirstSendPaths();
                    }

                    if (!temSiSiSubject.activeDurationKnown() | !temSiSiSubject.getWaitingTimeUntilActivation().getWellKnownDuration())
                    {
                        allKnown = false;
                        temSiSiSubject.tryToCalculateTimesAndChancesForAllPaths();
                    }
                }

                if (allKnown)
                {
                    i = long.MaxValue; // Exit For-Loop
                }
            }

            isErrorReportRun = true;

            foreach (string key in internalSubjects.Keys)
            {

                SiSi_Subject temSiSiSubject;
                internalSubjects.TryGetValue(key, out temSiSiSubject);

                addErrorMessageToReportCollection("");
                addErrorMessageToReportCollection("### Additional Problem Report for: " + key + " ###################");
                temSiSiSubject.checkIfAllStatesAndTransitionsInSIDAreInAPath();
                if (!allKnown)
                {

                    if (!temSiSiSubject.allResponsePathsPerfectlyKnown())
                    {
                        temSiSiSubject.tryToCalculateTimesAndChancesForAllResponsePaths();
                        if (!temSiSiSubject.allResponsePathsPerfectlyKnown())
                        {
                            allKnown = false;
                        }
                    }
                    if (!temSiSiSubject.allFirstSendPathsPerfectlyKnown())
                    {
                        allKnown = false;
                        temSiSiSubject.tryToCalculateTimesAndChancesForAllFirstSendPaths();
                    }

                    if (!temSiSiSubject.activeDurationKnown() | !temSiSiSubject.getWaitingTimeUntilActivation().getWellKnownDuration())
                    {
                        allKnown = false;
                        temSiSiSubject.tryToCalculateTimesAndChancesForAllPaths();
                    }
                }
            }

            foreach (SiSi_Subject tempSiSiSubject in internalSubjects.Values)
            {
                tempSiSiSubject.calculateMyActiveDuration();
            }

        }

        private static void getTransmissionTimeFromMessageShape(Shape msgShape)
        {
            if (msgShape.HasCategory(ALPSConstants.alpsShapeCategorySIDMessage))
            {
                SiSi_Distribution tempDuration = new SiSi_Distribution();
                tempDuration.parseStateOrTransition(msgShape);

                string msgName = msgShape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeLabel].ResultStr["none"];

                if (!messageTransmissionDurationDictionary.ContainsKey(msgName))
                {
                    messageTransmissionDurationDictionary.Add(msgName, tempDuration);
                }
            }
        }

        public static double getNumberOfSigmasForMinMax()
        {
            return numberOfSigmasForMinMax;
        }

        internal static int getMaxRecursionDepth()
        {
            return maxRecursionDepth;
        }

        internal static bool getErrorReportRun()
        {
            return isErrorReportRun;
        }

        internal static void addErrorMessageToReportCollection(string errorText)
        {
            collectionOfProblems.Add(errorText);
        }

        internal static bool getUseAbsoluteMinMax()
        {
            return useAbsolutMinMax;
        }

        internal static SiSi_Distribution getTansmissionTimeFor(string messageName)
        {
            SiSi_Distribution result = new SiSi_Distribution();
            if (messageName != null)
                messageTransmissionDurationDictionary.TryGetValue(messageName, out result);
            return result;
        }

        public static SiSi_Subject getSiSiSubject(string subjectName)
        {
            SiSi_Subject result = null;

            if (internalSubjects == null)
            {
                internalSubjects = new Dictionary<string, SiSi_Subject>();
            }
            if (interfaceSubjects == null)
            {
                interfaceSubjects = new Dictionary<string, SiSi_Subject>();
            }

            if (subjectName == null)
            {
                return null;
            }

            if (internalSubjects.ContainsKey(subjectName))
            {
                internalSubjects.TryGetValue(subjectName, out result);
            }
            else if (interfaceSubjects.ContainsKey(subjectName))
            {
                interfaceSubjects.TryGetValue(subjectName, out result);
            }

            return result;

        }



        internal static byte getMaximumNumberOfResponseContextSkips()
        {
            return maximumNumberOfResponseContextSkips;
        }

        internal static bool getWriteWaitingTimeForReceiveStates()
        {
            return writeWaitingTimeToReceiveStates;
        }

        internal static SiSi_Distribution getDefaultTimeForInterfaceReplies()
        {
            return defaultResponseTimeForInterfaceReplies;
        }

        public static List<string> getErrorReportList()
        {
            return collectionOfProblems;
        }
    }
}
