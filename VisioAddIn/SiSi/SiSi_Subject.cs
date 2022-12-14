using Microsoft.Office.Interop.Visio;
using System;
using System.Collections.Generic;
using System.Xml;

namespace VisioAddIn.SiSi
{
    class SiSi_Subject
    {

        private Shape subjectShape;
        private string subjectName;
        private SiSi_PathTree rootPath;
        private bool rootPathValid;
        private Shape startState;

        private Dictionary<string, Dictionary<string, Dictionary<string, SiSi_ResponseObject>>> responseObjects; //inquierySubject, inquieryMessage, responseMessage, responseObject
        //private Dictionary<string, Dictionary<string, SiSi_ResponseObject>> firstSendPaths;
        private Dictionary<string, Dictionary<string, SiSi_ResponseObject>> firstSendPaths; //
        private Dictionary<long, Shape> allElementsInTree; //ShapeID , Shape

        private Dictionary<string, string> dictionaryOfAllMessagesSendInSBD;
        private Dictionary<string, string> dictionaryOfAllMessagesReceivedInSBD;
        private Dictionary<string, Shape> dictionaryOfAllMessagesReceivedInSID;
        private Dictionary<string, Shape> dictionaryOfAllMessagesSendInSID;


        private Dictionary<Shape, double> dictionaryOfSummedUpEndStateChances;

        private SiSi_Distribution activeDuration;
        private SiSi_Distribution waitingTimeUntilActivation;
        private SiSi_Distribution inactiveDuration;

        private bool responsesPermanentlyClear;
        private bool firstSendsPermanentlyClear;
        private bool runtimePermanentlyClear;

        public void initializeInterface(Shape subjectShape)
        {
            XmlDocument xmlDocument = new XmlDocument();

            responseObjects = new Dictionary<string, Dictionary<string, Dictionary<string, SiSi_ResponseObject>>>();
            firstSendPaths = new Dictionary<string, Dictionary<string, SiSi_ResponseObject>>();
            allElementsInTree = new Dictionary<long, Shape>();

            activeDuration = new SiSi_Distribution();
            waitingTimeUntilActivation = new SiSi_Distribution();
            inactiveDuration = new SiSi_Distribution();

            setRuntimePermanentlyClear(true);
            setResponsePermanentlyClear(true);
            setFirstSendsPermanentlyClear(true);

            setSubjectShape(subjectShape);
            setSubjectName(getSubjectShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeLabel].ResultStr["none"]);

            long counterInternalList = 0;
            bool error = true;

            try
            {
                xmlDocument.LoadXml(getSubjectShape().CellsU["Prop." + ALPSConstants.simpleSimInterfaceSubjectResponseXML].ResultStr["none"]);
                error = false;
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show("Error while Loading XML");
            }
            if (!error)
            {
                XmlNode rootNode;
                rootNode = xmlDocument.SelectSingleNode("responseObjects");
                if (rootNode == null)
                {
                    return;
                }

                // int maxLengthName = 28; never used
                //SiSi_Distribution tempDistribution;
                //SiSi_ResponseObject tempObject;

                if (rootNode.ChildNodes.Count > 0)
                {
                    foreach (XmlNode node in rootNode.ChildNodes)
                    {
                        string inquierySubject = node.ChildNodes.Item(0).ToString();
                        string inquieryMessage = node.ChildNodes.Item(1).ToString();
                        string responseMessage = node.ChildNodes.Item(2).ToString();

                        string systemDecimalDelimiter = ALPSGlobalFunctions.getSystemDecimalDelimiter();

                        SiSi_Distribution tempDurationDistribution = new SiSi_Distribution();

                        tempDurationDistribution.setMeanValue(Double.Parse(node.ChildNodes.Item(3).ToString()));
                        tempDurationDistribution.setStandardDeviation(Double.Parse(node.ChildNodes.Item(4).ToString()));
                        tempDurationDistribution.setMinValue(Double.Parse(node.ChildNodes.Item(5).ToString()));
                        tempDurationDistribution.setMaxValue(Double.Parse(node.ChildNodes.Item(6).ToString()));

                        checkForCorruptedValues(tempDurationDistribution);

                        Dictionary<string, Dictionary<string, SiSi_ResponseObject>> localResponsesToSubject; //There Should only be one with the certian responseMessage
                        Dictionary<string, SiSi_ResponseObject> localResponsesForMessage;
                        SiSi_ResponseObject localResponseObject = new SiSi_ResponseObject();

                        if (responseObjects.ContainsKey(inquierySubject))
                        {
                            responseObjects.TryGetValue(inquierySubject, out localResponsesToSubject);
                        }
                        else
                        {
                            localResponsesToSubject = new Dictionary<string, Dictionary<string, SiSi_ResponseObject>>();
                            responseObjects.Add(inquierySubject, localResponsesToSubject);
                        }

                        if (localResponsesToSubject.ContainsKey(inquieryMessage))
                        {
                            localResponsesToSubject.TryGetValue(inquieryMessage, out localResponsesForMessage);
                        }
                        else
                        {
                            localResponsesForMessage = new Dictionary<string, SiSi_ResponseObject>();
                            localResponsesToSubject.Add(inquieryMessage, localResponsesForMessage);
                        }

                        if (localResponsesForMessage.ContainsKey(responseMessage))
                        {
                            System.Windows.Forms.MessageBox.Show("Warning! Found Double Definition for Response to: " + inquierySubject + "'s message: " + inquieryMessage + " with message: " + responseMessage);
                        }
                        else
                        {
                            SiSi_ResponseObject tempResponseObject = new SiSi_ResponseObject();
                            tempResponseObject.setChanceValueForResponse(Double.Parse(node.ChildNodes.Item(7).ToString()));

                            tempResponseObject.setAverageDurationForResponse(tempDurationDistribution);
                            tempResponseObject.setCorrespondenceSubject(inquierySubject);
                            tempResponseObject.setInquieryMessage(inquieryMessage);
                            tempResponseObject.setResponseMessage(responseMessage);
                            tempResponseObject.getAverageDurationForResponse().setWellKnownDuration(true);

                            localResponsesForMessage.Add(responseMessage, tempResponseObject);
                        }

                        counterInternalList++;
                    }
                }
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Warning: XML Load Error");
            }
        }

        public void initalizeSubject(Shape subjectShape)
        {
            setSubjectShape(subjectShape);
            setSubjectName(getSubjectShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeLabel].ResultStr["none"]);

            waitingTimeUntilActivation = new SiSi_Distribution();

            responseObjects = new Dictionary<string, Dictionary<string, Dictionary<string, SiSi_ResponseObject>>>();
            firstSendPaths = new Dictionary<string, Dictionary<string, SiSi_ResponseObject>>();
            allElementsInTree = new Dictionary<long, Shape>();

            dictionaryOfSummedUpEndStateChances = new Dictionary<Shape, double>();
            dictionaryOfAllMessagesSendInSID = new Dictionary<string, Shape>();
            dictionaryOfAllMessagesSendInSBD = new Dictionary<string, string>();
            dictionaryOfAllMessagesReceivedInSID = new Dictionary<string, Shape>();
            dictionaryOfAllMessagesReceivedInSBD = new Dictionary<string, string>();

            setRuntimePermanentlyClear(false);
            setResponsePermanentlyClear(false);
            setFirstSendsPermanentlyClear(false);

            SiSi_SimpleSim.addErrorMessageToReportCollection(System.Environment.NewLine +
                "### Construction Problem Report for Subject: " + subjectName + " ###" + System.Environment.NewLine);

            findStartState();

            if (startState != null)
            {
                rootPath = new SiSi_PathTree();

                rootPathValid = rootPath.initialize(this, null, startState, 1);

                if (!rootPathValid)
                {
                    SiSi_SimpleSim.addErrorMessageToReportCollection("Could not find a route to an end state in SBD of: " + subjectName);
                    setRuntimePermanentlyClear(false);
                    setResponsePermanentlyClear(false);
                    setFirstSendsPermanentlyClear(false);
                }

                tryToCalculateTimesAndChancesForAllResponsePaths();

                tryToCalculateTimesAndChancesForAllFirstSendPaths();
            }
        }

        public bool responsePathExistsFor(string senderSubject, string inquieryMessageName, string responseMessageName)
        {
            bool result = false;

            if (responseObjects.ContainsKey(senderSubject))
            {
                Dictionary<string, Dictionary<string, SiSi_ResponseObject>> subjectResponses;
                responseObjects.TryGetValue(senderSubject, out subjectResponses);

                if (subjectResponses.ContainsKey(inquieryMessageName))
                {
                    Dictionary<string, SiSi_ResponseObject> subjectResponsesForMessage;
                    subjectResponses.TryGetValue(inquieryMessageName, out subjectResponsesForMessage);

                    if (subjectResponsesForMessage.ContainsKey(responseMessageName))
                    {
                        result = true;
                    }
                }
            }

            return result;
        }

        public bool firstSendPathsExistsFor(string receivingSubject, string expectedFirstSendMessage)
        {
            bool result = false;

            if (firstSendPaths.ContainsKey(receivingSubject))
            {
                Dictionary<string, SiSi_ResponseObject> firstSendMessages;
                firstSendPaths.TryGetValue(receivingSubject, out firstSendMessages);

                if (firstSendMessages.ContainsKey(expectedFirstSendMessage))
                {
                    result = true;
                }
            }

            return result;
        }

        public SiSi_ResponseObject getFirstSendPathsObject(string receivingSubject, string expectedFirstSendMessage)
        {
            SiSi_ResponseObject result = null;

            if (firstSendPaths.ContainsKey(receivingSubject))
            {
                Dictionary<string, SiSi_ResponseObject> firstSendMessages;
                firstSendPaths.TryGetValue(receivingSubject, out firstSendMessages);

                if (firstSendMessages.ContainsKey(expectedFirstSendMessage))
                {
                    firstSendMessages.TryGetValue(expectedFirstSendMessage, out result);
                }
            }
            else if (subjectShape.HasCategory(ALPSConstants.alpsShapeCategoryInterfaceActor))
            {
                result = new SiSi_ResponseObject();
                result.setAverageDurationForResponse(new SiSi_Distribution());
                result.setCorrespondenceSubject(receivingSubject);
                result.setResponseMessage(expectedFirstSendMessage);
            }

            return result;
        }

        public SiSi_ResponseObject getResponseObject(string senderSubject, string inquieryMessageName, string responseMessageName)
        {
            SiSi_ResponseObject result = null;

            if (responseObjects.ContainsKey(senderSubject))
            {
                Dictionary<string, Dictionary<string, SiSi_ResponseObject>> subjectResponses;
                responseObjects.TryGetValue(senderSubject, out subjectResponses);

                if (subjectResponses.ContainsKey(inquieryMessageName))
                {
                    Dictionary<string, SiSi_ResponseObject> subjectResponsesForMessage;
                    subjectResponses.TryGetValue(inquieryMessageName, out subjectResponsesForMessage);

                    if (subjectResponsesForMessage.ContainsKey(responseMessageName))
                    {
                        subjectResponsesForMessage.TryGetValue(responseMessageName, out result);
                    }
                }
            }

            if (result == null & subjectShape.HasCategory(ALPSConstants.alpsShapeCategoryInterfaceActor))
            {
                result = new SiSi_ResponseObject();
                result.setAverageDurationForResponse(new SiSi_Distribution());
                result.getAverageDurationForResponse().addDistribution(SiSi_SimpleSim.getDefaultTimeForInterfaceReplies());
                result.getAverageDurationForResponse().setWellKnownDuration(true);
                result.setChanceValueForResponse(1);
                result.setInquieryMessage(inquieryMessageName);
                result.setResponseMessage(responseMessageName);
                result.setCorrespondenceSubject(senderSubject);
            }

            return result;
        }

        //Never used? Didn't function

        /*
        private SiSi_Distribution computeAverageWeigthedDurationOverAllPaths(ICollection<SiSi_ResponsePath> pathCollection)
        {
            SiSi_Distribution result = new SiSi_Distribution();
            double sum = 0;

            foreach(SiSi_ResponsePath temp in pathCollection)
            {
                sum += temp.getChanceValue();
            }

            foreach(SiSi_ResponsePath temp in pathCollection)
            {
                temp.calculateTimeAndChance();
            }
        }
        */

        public void addFirstSendPath(SiSi_ResponsePath firstSendPath)
        {
            Dictionary<string, SiSi_ResponseObject> localFirstSendMessages;
            SiSi_ResponseObject localFirstReceiveObject;

            if (firstSendPaths.ContainsKey(firstSendPath.getCorrespondenceSubject()))
            {
                firstSendPaths.TryGetValue(firstSendPath.getCorrespondenceSubject(), out localFirstSendMessages);
            }
            else
            {
                localFirstSendMessages = new Dictionary<string, SiSi_ResponseObject>();
                firstSendPaths.Add(firstSendPath.getCorrespondenceSubject(), localFirstSendMessages);
            }

            if (localFirstSendMessages.ContainsKey(firstSendPath.getResponseMessage()))
            {
                localFirstSendMessages.TryGetValue(firstSendPath.getResponseMessage(), out localFirstReceiveObject);
            }
            else
            {
                localFirstReceiveObject = new SiSi_ResponseObject();
                localFirstSendMessages.Add(firstSendPath.getResponseMessage(), localFirstReceiveObject);
                localFirstReceiveObject.setCorrespondenceSubject(firstSendPath.getSubject());
                localFirstReceiveObject.setResponseMessage(firstSendPath.getResponseMessage());
            }
            localFirstReceiveObject.addResponsePath(firstSendPath);
        }

        public void addResponsePath(SiSi_ResponsePath responsePath)
        {
            Dictionary<string, Dictionary<string, SiSi_ResponseObject>> localResponsesToSubject;
            Dictionary<string, SiSi_ResponseObject> localResponsesForMessage;
            SiSi_ResponseObject localResponseObject;

            if (responseObjects.ContainsKey(responsePath.getCorrespondenceSubject()))
            {
                responseObjects.TryGetValue(responsePath.getCorrespondenceSubject(), out localResponsesToSubject);
            }
            else
            {
                localResponsesToSubject = new Dictionary<string, Dictionary<string, SiSi_ResponseObject>>();
                responseObjects.Add(responsePath.getCorrespondenceSubject(), localResponsesToSubject);
            }

            if (localResponsesToSubject.ContainsKey(responsePath.getInquieryMessage()))
            {
                localResponsesToSubject.TryGetValue(responsePath.getInquieryMessage(), out localResponsesForMessage);
            }
            else
            {
                localResponsesForMessage = new Dictionary<string, SiSi_ResponseObject>();
                localResponsesToSubject.Add(responsePath.getInquieryMessage(), localResponsesForMessage);
            }

            if (localResponsesForMessage.ContainsKey(responsePath.getResponseMessage()))
            {
                localResponsesForMessage.TryGetValue(responsePath.getResponseMessage(), out localResponseObject);
            }
            else
            {
                localResponseObject = new SiSi_ResponseObject();
                localResponseObject.setInquieryMessage(responsePath.getInquieryMessage());
                localResponseObject.setResponseMessage(responsePath.getResponseMessage());
                localResponseObject.setCorrespondenceSubject(responsePath.getCorrespondenceSubject());
                localResponsesForMessage.Add(responsePath.getResponseMessage(), localResponseObject);
            }

            localResponseObject.addResponsePath(responsePath);
        }



        private void findStartState()
        {
            Page correspondingSBDPage = ALPSGlobalFunctions.determineConnectedSbdPageForSubject(getSubjectShape());

            foreach (Shape shape in correspondingSBDPage.Shapes)
            {
                if (shape.HasCategory(ALPSConstants.alpsShapeCategorySBDState))
                {
                    string isStartState = shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeSBDStateIsStartState].ResultStr[""];
                    bool isStart = Boolean.TryParse(isStartState, out isStart);
                    if (isStart)
                    {
                        startState = shape;
                        return;
                    }
                }
            }

            SiSi_SimpleSim.addErrorMessageToReportCollection("Warning! No Start State found in SBD of: " + subjectName);
        }

        public void calculateMyActiveDuration()
        {
            if (rootPath != null & rootPathValid)
            {
                activeDuration = rootPath.getOverallDurationIncludingSubTrees();
                inactiveDuration = rootPath.getOverallWaitingDurationIncludingSubTrees();
            }
            else if (subjectShape.HasCategory(ALPSConstants.alpsShapeCategoryInterfaceActor))
            {
                activeDuration = new SiSi_Distribution();
                inactiveDuration = new SiSi_Distribution();
            }
        }

        public string allResponseObjectsToString()
        {
            string result = "";

            Dictionary<string, Dictionary<string, SiSi_ResponseObject>> s;
            Dictionary<string, SiSi_ResponseObject> i;
            SiSi_ResponseObject r;

            result = "  -------Response Objects ----------";
            result += Environment.NewLine + "- Number of Subjects that get answers: " + responseObjects.Count;
            foreach (string subject in responseObjects.Keys)
            {
                responseObjects.TryGetValue(subject, out s);

                result += Environment.NewLine + "  - Subject: " + subject;
                result += Environment.NewLine + "   - inquieries by Subject: " + s.Count;

                foreach (string inquieryMessage in s.Keys)
                {
                    s.TryGetValue(inquieryMessage, out i);

                    result += Environment.NewLine + "    - Inquiery Message: " + inquieryMessage;
                    result += Environment.NewLine + "     - Responses To inquiery Message: " + i.Count;

                    foreach (string responseMessage in i.Keys)
                    {
                        i.TryGetValue(responseMessage, out r);
                        result += Environment.NewLine + "            - Response Message: " + responseMessage;
                        result += Environment.NewLine + "              - ResponsePaths for Message: " + r.getResponsePathCollection().Count;
                        result += Environment.NewLine + "              - Response Chance: " + r.getChanceValueForResponse();
                        result += Environment.NewLine + "              - Response time: " + r.getAverageDurationForResponse().toString(false);
                        result += Environment.NewLine + "              - Response time sure: " + r.getAverageDurationForResponse().getWellKnownDuration();
                    }

                }

            }

            return result;
        }



        public string allFirstSendPathsToString()
        {
            string result = "";

            Dictionary<string, SiSi_ResponseObject> s;
            SiSi_ResponseObject r;

            result = "  -------First Send Paths ----------";

            if (firstSendPaths != null)
            {

                foreach (string firstSendPath in firstSendPaths.Keys)
                {
                    firstSendPaths.TryGetValue(firstSendPath, out s);

                    result += Environment.NewLine + "  - Subject: " + firstSendPath;
                    result += Environment.NewLine + "   - First Sends to Subject: " + s.Count;

                    foreach (string responseMessage in s.Keys)
                    {
                        s.TryGetValue(responseMessage, out r);

                        result += Environment.NewLine + "            - First Send Message: " + responseMessage;
                        result += Environment.NewLine + "              - FirstSendPaths for Message: " + r.getResponsePathCollection().Count;
                        result += Environment.NewLine + "              - First Send Chance: " + r.getChanceValueForResponse();
                        result += Environment.NewLine + "              - First Send time: " + r.getAverageDurationForResponse().toString(false);
                        result += Environment.NewLine + "              - First Send time sure: " + r.getAverageDurationForResponse().getWellKnownDuration();
                    }
                }
            }

            return result;
        }


        public void printOutAllResponseObjects()
        {
            System.Windows.Forms.MessageBox.Show("Not implemented yet, use both methods before this method.");
        }


        public void tryToCalculateTimesAndChancesForAllPaths()
        {
            if (rootPath != null)
            {
                rootPath.tryToDetermineTreeDuration();
            }
        }

        public void tryToCalculateTimesAndChancesForAllResponsePaths()
        {
            Dictionary<string, Dictionary<string, SiSi_ResponseObject>> responseDictSubjectLevel;
            Dictionary<string, SiSi_ResponseObject> responseDictInquieryLevel;
            SiSi_ResponseObject responseObject;

            foreach (string subjectKey in responseObjects.Keys)
            {
                responseObjects.TryGetValue(subjectKey, out responseDictSubjectLevel);
                foreach (string inquieryKey in responseDictSubjectLevel.Keys)
                {
                    responseDictSubjectLevel.TryGetValue(inquieryKey, out responseDictInquieryLevel);
                    foreach (string responseKey in responseDictInquieryLevel.Keys)
                    {
                        responseDictInquieryLevel.TryGetValue(responseKey, out responseObject);

                        responseObject.tryToCalculateChanceAndTimeForResponse();
                    }
                }
            }
        }



        public void tryToCalculateTimesAndChancesForAllFirstSendPaths()
        {
            Dictionary<string, SiSi_ResponseObject> dictSubjectLevel;
            SiSi_ResponseObject firstSendObject;

            foreach (string subjectKey in firstSendPaths.Keys)
            {
                firstSendPaths.TryGetValue(subjectKey, out dictSubjectLevel);
                foreach (string responseKey in dictSubjectLevel.Keys)
                {
                    dictSubjectLevel.TryGetValue(responseKey, out firstSendObject);

                    firstSendObject.tryToCalculateChanceAndTimeForResponse();
                }
            }
        }



        public bool activeDurationKnown()
        {
            if (runtimePermanentlyClear)
            {
                return true;
            }
            else if (rootPath != null & rootPathValid)
            {
                runtimePermanentlyClear = rootPath.getOverallDurationIncludingSubTrees().getWellKnownDuration();
                return runtimePermanentlyClear;
            }
            else
            {
                return false;
            }
        }


        public bool allResponsePathsPerfectlyKnown()
        {
            bool result = true;

            if (subjectShape.HasCategory(ALPSConstants.alpsShapeCategoryInterfaceActor))
            {
                responsesPermanentlyClear = true;
            }

            if (responsesPermanentlyClear)
            {
                result = true;
            }
            else
            {


                Dictionary<string, Dictionary<string, SiSi_ResponseObject>> responseDictSubjectLevel;
                Dictionary<string, SiSi_ResponseObject> responseDictInquieryLevel;
                SiSi_ResponseObject responseObject;

                foreach (string subjectKey in responseObjects.Keys)
                {
                    responseObjects.TryGetValue(subjectKey, out responseDictSubjectLevel);
                    foreach (string inquieryKey in responseDictSubjectLevel.Keys)
                    {
                        responseDictSubjectLevel.TryGetValue(inquieryKey, out responseDictInquieryLevel);
                        foreach (string responseKey in responseDictInquieryLevel.Keys)
                        {
                            responseDictInquieryLevel.TryGetValue(responseKey, out responseObject);

                            if (!responseObject.getAverageDurationForResponse().getWellKnownDuration())
                            {
                                //faster processing
                                return false;
                            }
                        }
                    }
                }

                if (result)
                {
                    responsesPermanentlyClear = true;
                }
            }
            return result;
        }


        public bool allFirstSendPathsPerfectlyKnown()
        {
            bool result = true;
            if (firstSendsPermanentlyClear)
            {
                result = true;
            }
            else
            {
                Dictionary<string, SiSi_ResponseObject> dictSubjectLevel;
                SiSi_ResponseObject firstSendObject;

                foreach (string subjectKey in firstSendPaths.Keys)
                {
                    firstSendPaths.TryGetValue(subjectKey, out dictSubjectLevel);
                    foreach (string responseKey in dictSubjectLevel.Keys)
                    {
                        dictSubjectLevel.TryGetValue(responseKey, out firstSendObject);

                        if (!firstSendObject.getAverageDurationForResponse().getWellKnownDuration())
                        {
                            //faster processing
                            return false;
                        }
                    }
                }
            }

            return result;
        }



        public void checkIfAllStatesAndTransitionsInSIDAreInAPath()
        {
            foreach (Shape shape in ALPSGlobalFunctions.determineConnectedSbdPageForSubject(subjectShape).Shapes)
            {
                if (shape.HasCategory(ALPSConstants.alpsShapeCategoryModelComponent))
                {
                    if (!allElementsInTree.ContainsKey(shape.ID))
                    {
                        string shapeLabel = shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeLabel].ResultStr["none"];
                        shapeLabel = ALPSGlobalFunctions.removeLineBreaks(shapeLabel);

                        SiSi_SimpleSim.addErrorMessageToReportCollection("shape: " + shape.Name + " - (Label: " + shapeLabel + ") not in valid path ");
                    }
                }
            }
        }


        public void tryToFindCorrespondigFirstSendTimeForInterface()
        {
            long[] subjectIDs = (long[])subjectShape.ConnectedShapes(VisConnectedShapesFlags.visConnectedShapesOutgoingNodes, ALPSConstants.alpsShapeCategorySIDactor);

            foreach (int id in subjectIDs)
            {
                Shape connectedShape = subjectShape.ContainingPage.Shapes.ItemFromID[id];

                if (connectedShape != null)
                {
                    SiSi_Subject correspondingSubject = SiSi_SimpleSim.getSiSiSubject(connectedShape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeLabel].ResultStr["none"]);
                }
            }
        }

        public void createListOfMessagesSend()
        {
            long[] idsOfConnectedShapes = (long[])subjectShape.GluedShapes(VisGluedShapesFlags.visGluedShapesOutgoing1D, "");

            foreach (long id in idsOfConnectedShapes)
            {
                Shape outgoingConnectorShape = subjectShape.ContainingPage.Shapes.ItemFromID[(int)id];

                if (outgoingConnectorShape.HasCategory(ALPSConstants.alpsShapeCategorySIDMessageConnector))
                {
                    string messagelist = "";
                    int messageBoxID = int.Parse(outgoingConnectorShape.CellsU["User.idOfCorrespondingShape"].ResultStr["none"]);

                    Shape messageBox = outgoingConnectorShape.ContainingPage.Shapes.ItemFromID[messageBoxID];

                    if (messageBox != null)
                    {
                        long[] messageIDs = (long[])messageBox.ContainerProperties.GetListMembers();

                        foreach (long messageID in messageIDs)
                        {
                            Shape messageShape = outgoingConnectorShape.ContainingPage.Shapes.ItemFromID[(int)messageID];
                            if (messageShape.HasCategory(ALPSConstants.alpsShapeCategorySIDMessage))
                            {
                                string labelOfMessage = messageShape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeLabel].ResultStr["none"].Replace('\"', ' ');
                                string idOfMessage = messageShape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentID].ResultStr["none"].Replace('\"', ' ');
                                idOfMessage = ALPSGlobalFunctions.makeStringUriCompatible(idOfMessage);

                                if (!dictionaryOfAllMessagesSendInSID.ContainsKey(labelOfMessage))
                                {
                                    dictionaryOfAllMessagesSendInSID.Add(labelOfMessage, messageShape);
                                }
                            }
                        }

                    }
                }
            }

        }

        public void createListOfMessagesReceived()
        {
            long[] idsOfConnectedShapes = (long[])subjectShape.GluedShapes(VisGluedShapesFlags.visGluedShapesIncoming1D, "");

            foreach (long id in idsOfConnectedShapes)
            {
                Shape incomingConnectorShape = subjectShape.ContainingPage.Shapes.ItemFromID[(int)id];

                if (incomingConnectorShape.HasCategory(ALPSConstants.alpsShapeCategorySIDMessageConnector))
                {
                    string messagelist = "";
                    int messageBoxID = int.Parse(incomingConnectorShape.CellsU["User.idOfCorrespondingShape"].ResultStr["none"]);

                    Shape messageBox = incomingConnectorShape.ContainingPage.Shapes.ItemFromID[messageBoxID];

                    if (messageBox != null)
                    {
                        long[] messageIDs = (long[])messageBox.ContainerProperties.GetListMembers();

                        foreach (long messageID in messageIDs)
                        {
                            Shape messageShape = incomingConnectorShape.ContainingPage.Shapes.ItemFromID[(int)messageID];
                            if (messageShape.HasCategory(ALPSConstants.alpsShapeCategorySIDMessage))
                            {
                                string labelOfMessage = messageShape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeLabel].ResultStr["none"].Replace('\"', ' ');
                                string idOfMessage = messageShape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentID].ResultStr["none"].Replace('\"', ' ');

                                idOfMessage = ALPSGlobalFunctions.makeStringUriCompatible(idOfMessage);

                                if (!dictionaryOfAllMessagesReceivedInSID.ContainsKey(labelOfMessage))
                                {
                                    dictionaryOfAllMessagesReceivedInSID.Add(labelOfMessage, messageShape);
                                }
                            }
                        }

                    }
                }
            }

        }

        public void compareMessagesSendInSIDandSBD()
        {
            foreach (string messageLabel in dictionaryOfAllMessagesSendInSID.Keys)
            {
                if (!dictionaryOfAllMessagesSendInSBD.ContainsKey(messageLabel))
                {
                    SiSi_SimpleSim.addErrorMessageToReportCollection("The message > " + messageLabel + " < is defined to be send in the SID but never send in SBD");
                }
            }

            foreach (string messageLabel in dictionaryOfAllMessagesReceivedInSID.Keys)
            {
                if (!dictionaryOfAllMessagesReceivedInSBD.ContainsKey(messageLabel))
                {
                    SiSi_SimpleSim.addErrorMessageToReportCollection("The message > " + messageLabel + " < is defined to be received in the SID but never received in SBD");
                }
            }
        }

        //Checks Distribution for broken Data
        private static void checkForCorruptedValues(SiSi_Distribution distr)
        {
            if (distr.getMeanValue() < 0)
            {
                distr.setMeanValue(0);
            }
            if (distr.getMinValue() > distr.getMeanValue())
            {
                distr.setMinValue(distr.getMeanValue());
            }
            if (distr.getMaxValue() < distr.getMeanValue())
            {
                distr.setMaxValue(distr.getMeanValue());
            }
        }

        //+++
        //setter-methods
        //+++
        public void setSubjectShape(Shape subjectShape) { this.subjectShape = subjectShape; }
        public void setSubjectName(string subjectName) { this.subjectName = subjectName.Replace("\"", ""); }
        public void setRootPath(SiSi_PathTree rootPath) { this.rootPath = rootPath; }
        public void setRootPathValid(bool rootPathValid) { this.rootPathValid = rootPathValid; }
        public void setStartState(Shape startState) { this.startState = startState; }

        public void setActiveDuration(SiSi_Distribution activeDuration) { this.activeDuration = activeDuration; }
        public void setWaitingTimeUntilActivation(SiSi_Distribution waitingTimeUntilActivation) { this.waitingTimeUntilActivation = waitingTimeUntilActivation; }
        public void setInactiveDuration(SiSi_Distribution inactiveDuration) { this.inactiveDuration = inactiveDuration; }

        public void setResponsePermanentlyClear(bool responsesPermanentlyClear) { this.responsesPermanentlyClear = responsesPermanentlyClear; }
        public void setFirstSendsPermanentlyClear(bool firstSendsPermanentlyClear) { this.firstSendsPermanentlyClear = firstSendsPermanentlyClear; }
        public void setRuntimePermanentlyClear(bool runtimePermanentlyClear) { this.runtimePermanentlyClear = runtimePermanentlyClear; }
        //---
        //setter-methods
        //---

        //add methods
        public void addResponseObject(string message, Dictionary<string, Dictionary<string, SiSi_ResponseObject>> responseObject) { responseObjects.Add(message, responseObject); }
        public void addFirstSendPath(string name, Dictionary<string, SiSi_ResponseObject> responseObject) { firstSendPaths.Add(name, responseObject); }
        public void addElementToList(long id, Shape element) { allElementsInTree.Add(id, element); }

        public void addElementToSummedUpEndStates(Shape endState, double chanceValue) { dictionaryOfSummedUpEndStateChances.Add(endState, chanceValue); }
        //+++
        //getter-methods
        //+++
        public Shape getSubjectShape() { return subjectShape; }
        public string getSubjectName() { return ALPSGlobalFunctions.removeQuotes(subjectName); }
        public SiSi_PathTree getRootPath() { return rootPath; }
        public bool getRootPathValid() { return rootPathValid; }
        public Shape getStartState() { return startState; }

        public SiSi_Distribution getActiveDuration() { return activeDuration; }
        public SiSi_Distribution getWaitingTimeUntilActivation() { return waitingTimeUntilActivation; }
        public SiSi_Distribution getInactiveDuration() { return inactiveDuration; }

        public bool getResponsePermanentlyClear() { return responsesPermanentlyClear; }
        public bool getFirstSendsPermanentlyClear() { return firstSendsPermanentlyClear; }
        public bool getRuntimePermanentlyClear() { return runtimePermanentlyClear; }

        public Dictionary<string, Dictionary<string, Dictionary<string, SiSi_ResponseObject>>> getResponseObjects() { return responseObjects; }
        public Dictionary<string, Dictionary<string, SiSi_ResponseObject>> getFirstSendPaths() { return firstSendPaths; }
        public Dictionary<long, Shape> getAllElementsInTree() { return allElementsInTree; }

        public Dictionary<Shape, double> getDictionaryOfSummedUpEndStateChances() { return dictionaryOfSummedUpEndStateChances; }

        public Dictionary<string, string> getDictionaryOfAllMessagesReceivedInSBD() { return dictionaryOfAllMessagesReceivedInSBD; }
        public Dictionary<string, Shape> getDictionaryOfAllMessagesReceivedInSID() { return dictionaryOfAllMessagesReceivedInSID; }
        public Dictionary<string, string> getDictionaryOfAllMessagesSendInSBD() { return dictionaryOfAllMessagesSendInSBD; }
        public Dictionary<string, Shape> getDictionaryOfAllMessagesSendInSID() { return dictionaryOfAllMessagesSendInSID; }
        //---
        //getter-methods
        //---
    }
}


