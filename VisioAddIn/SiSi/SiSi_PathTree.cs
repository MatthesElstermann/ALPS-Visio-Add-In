using Microsoft.Office.Interop.Visio;
using System;
using System.Collections.Generic;

namespace VisioAddIn.SiSi
{
    class SiSi_PathTree
    {
        //+++
        //Global Variables
        //+++
        private SiSi_Subject simpleSimSubject;
        private Shape state;
        private string stateLabel;

        private SiSi_PathTree parentPathTree;
        private Shape inputReceiveTransition;
        private double inputReceiveTransitionTimeOutTimeChance;
        private double inputReceivetransitionUserCancelChance;


        private Dictionary<Shape, SiSi_PathTree> childPaths; //Outgoing Shape to Pathtree
        private int numberOfChildPaths;
        private double sumOfChildPathChances;

        private double chanceValue;
        private double originalReceiveChanceValue;

        private SiSi_Distribution internalDuration;
        private SiSi_Distribution timeFromSendToReceptionOfReply;
        private SiSi_Distribution resultingWaitingTime;

        private SiSi_Distribution overallDurationIncludingSubTrees;
        private SiSi_Distribution overallWaitingDurationIncludingSubTrees;

        private bool isEndState;
        private bool isTerminalPath;

        private int stateInPathOccurence;

        private string responseContextSubjectName;
        private string responseContextMessageName;

        private string inquieryContextSubjectName;
        private string inquieryContextMessageName;
        //---
        //Global Variables
        //---


        public SiSi_PathTree()
        {
            childPaths = new Dictionary<Shape, SiSi_PathTree>();
            numberOfChildPaths = 0;
            isTerminalPath = false;
            timeFromSendToReceptionOfReply = new SiSi_Distribution();
            resultingWaitingTime = new SiSi_Distribution();
            resultingWaitingTime.setWellKnownDuration(false);
            overallDurationIncludingSubTrees = new SiSi_Distribution();
            overallDurationIncludingSubTrees.setWellKnownDuration(false);
            originalReceiveChanceValue = 1;
            internalDuration = new SiSi_Distribution();

            overallWaitingDurationIncludingSubTrees = new SiSi_Distribution();
            overallWaitingDurationIncludingSubTrees.setWellKnownDuration(false);
        }

        public bool initialize(SiSi_Subject subject, SiSi_PathTree parentPathTree, Shape state, double chanceValue)
        {
            //if there is no state, exit the method
            if (state == null)
            {
                return false;
            }
            setSimpleSimSubject(subject);
            setParentTree(parentPathTree);
            setState(state);
            setChanceValue(chanceValue);
            checkStateOccurence();

            setStateLabel(ALPSGlobalFunctions.removeLineBreaks(getState().CellsU["Prop." + ALPSConstants.alpsPropertieTypeLabel].ResultStr["none"]));

            bool result;

            if (getStateInPathOccurence() > SiSi_SimpleSim.getMaxRecursionDepth())
            {
                result = false;
            }
            else
            {
                //Not Sure if it Works
                setIsEndState(Boolean.Parse(getState().CellsU["Prop." + ALPSConstants.alpsPropertieTypeSBDStateIsEndState].ResultStr["none"]));
                result = createFollowUpPaths();

                tryToDetermineDuration();
            }

            return result;
        }


        //is bool in VBA, but there was no return value
        //Duration of Terminal PathNode is zero.
        public void initalizeAsTerminalPath(double newChoicePercentage, SiSi_Subject simpleSimSubject, SiSi_PathTree parentPathTree)
        {
            setChanceValue(newChoicePercentage);
            this.simpleSimSubject = simpleSimSubject;
            this.parentPathTree = parentPathTree;

            setIsTerminalPath(true);

            setOverallDurationIncludingSubTrees(new SiSi_Distribution());
            getOverallDurationIncludingSubTrees().setWellKnownDuration(true);

            setOverallWaitingDurationIncludingSubTrees(new SiSi_Distribution());
            getOverallWaitingDurationIncludingSubTrees().setWellKnownDuration(true);

        }

        public void tryToDetermineTreeDuration()
        {
            double sumOfReceivePathChildChances = 0;

            foreach (Shape key in getChildPaths().Keys)
            {
                SiSi_PathTree tempChildTree;
                childPaths.TryGetValue(key, out tempChildTree);

                if (!tempChildTree.getOverallDurationIncludingSubTrees().getWellKnownDuration())
                {
                    tempChildTree.tryToDetermineTreeDuration();
                }

                if (tempChildTree.getInputReceiveTransition() != null)
                {
                    sumOfReceivePathChildChances += tempChildTree.getOriginalReceiveChanceValue();
                }
            }

            foreach (Shape key in getChildPaths().Keys)
            {
                SiSi_PathTree tempChildTree;
                childPaths.TryGetValue(key, out tempChildTree);

                if (tempChildTree.getInputReceiveTransition() != null)
                {
                    if (sumOfChildPathChances > 0)
                    {
                        double chanceValue = (tempChildTree.getOriginalReceiveChanceValue() / sumOfReceivePathChildChances) * (1 - getInputReceiveTransitionTimeOutTimeChance()) * (1 - getInputReceiveTransitionUserCancelChance());
                        tempChildTree.setChanceValue(chanceValue);
                    }
                    else
                    {
                        tempChildTree.setChanceValue(0);
                        SiSi_SimpleSim.addErrorMessageToReportCollection("Warning: 0 Chance Paths in Subject: " + this.getSimpleSimSubject().getSubjectName() + " - " + this.getState().Name);
                        SiSi_SimpleSim.addReportMessage(new SiSi_ReportMessage(SiSi_ReportMessage.ReportType.Warning, this.getSimpleSimSubject().getSubjectName(), this.getState().Name, "Warning: 0 Chance Paths in Subject: " + this.getSimpleSimSubject().getSubjectName() + " - " + this.getState().Name, "PathTree Z.159"));
                    }
                }

            }

            tryToDetermineDuration();

            if (getParentTree() == null)
            {
                if (!getSimpleSimSubject().getWaitingTimeUntilActivation().getWellKnownDuration())
                {
                    if (!getState().HasCategory(ALPSConstants.alpsShapeCategoryReceiveState))
                    {
                        getSimpleSimSubject().getWaitingTimeUntilActivation().setWellKnownDuration(true);
                    }
                    else //is RecieveState
                    {

                        getSimpleSimSubject().getWaitingTimeUntilActivation().setWellKnownDuration(true);

                        double minValue = Double.MaxValue;
                        double maxValue = Double.MinValue;


                        SiSi_PathTree tempChildTree = null; //To get the InitialMessage after the loop, null for inilization
                        SiSi_Subject correspondenceSubject = null;

                        foreach (Shape key in getChildPaths().Keys)
                        {

                            childPaths.TryGetValue(key, out tempChildTree);

                            bool newSetYet = false;

                            correspondenceSubject = SiSi_SimpleSim.getSiSiSubject(tempChildTree.getResponseContextSubjectName());

                            if (correspondenceSubject != null)
                            {
                                SiSi_ResponseObject tempFirstSendObject = correspondenceSubject.getFirstSendPathsObject(this.getSimpleSimSubject().getSubjectName(), tempChildTree.getResponseContextMessageName());

                                if (!newSetYet)
                                {
                                    getSimpleSimSubject().setWaitingTimeUntilActivation(new SiSi_Distribution());
                                    getSimpleSimSubject().getWaitingTimeUntilActivation().setWellKnownDuration(true);
                                    newSetYet = true;
                                }

                                if (tempFirstSendObject != null)
                                {
                                    getSimpleSimSubject().getWaitingTimeUntilActivation().addDistributionWeighted(tempFirstSendObject.getAverageDurationForResponse(), tempFirstSendObject.getChanceValueForResponse());

                                    if (SiSi_SimpleSim.getUseAbsoluteMinMax())
                                    {
                                        if (tempFirstSendObject.getAverageDurationForResponse().getMinValue() < minValue)
                                        {
                                            minValue = tempFirstSendObject.getAverageDurationForResponse().getMinValue();
                                        }
                                        if (tempFirstSendObject.getAverageDurationForResponse().getMaxValue() > maxValue)
                                        {
                                            maxValue = tempFirstSendObject.getAverageDurationForResponse().getMaxValue();
                                        }
                                    }
                                    else
                                    {
                                        simpleSimSubject.getWaitingTimeUntilActivation().setWellKnownDuration(false);

                                        ///Only for Error Report Run
                                        if (SiSi_SimpleSim.getErrorReportRun())
                                        {
                                            if (tempChildTree.getResponseContextMessageName().Equals(""))
                                            {
                                                SiSi_SimpleSim.addErrorMessageToReportCollection("intial receive state: " + getState().Name + " - Missing receive Message Name.");
                                            }
                                            else
                                            {
                                                SiSi_SimpleSim.addErrorMessageToReportCollection("intial receive state: " + getState().Name + " - could get a first send time from: " + getSimpleSimSubject().getSubjectName() + " asking for message: " + tempChildTree.getResponseContextMessageName() + " (May not be send?)");
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (SiSi_SimpleSim.getUseAbsoluteMinMax())
                        {
                            getSimpleSimSubject().getWaitingTimeUntilActivation().setMinValue(minValue);
                            getSimpleSimSubject().getWaitingTimeUntilActivation().setMaxValue(maxValue);
                        }

                        SiSi_Distribution transmissionTimeOfInitialMessage = SiSi_SimpleSim.getTansmissionTimeFor(tempChildTree.getResponseContextMessageName());

                        if (transmissionTimeOfInitialMessage != null)
                        {
                            getSimpleSimSubject().getWaitingTimeUntilActivation().addDistribution(transmissionTimeOfInitialMessage);

                        }
                        else
                        {
                            getSimpleSimSubject().getWaitingTimeUntilActivation().setWellKnownDuration(false);
                        }

                        if (correspondenceSubject != null)
                        {
                            getSimpleSimSubject().getWaitingTimeUntilActivation().addDistribution(correspondenceSubject.getWaitingTimeUntilActivation());
                        }
                        else
                        {
                            getSimpleSimSubject().getWaitingTimeUntilActivation().setWellKnownDuration(false);
                        }
                    }
                }
            }
        }


        private void tryToDetermineDuration()
        {
            if (!getOverallDurationIncludingSubTrees().getWellKnownDuration())
            {
                if (getInternalDuration() == null || !getInternalDuration().getWellKnownDuration())
                {
                    internalDuration = new SiSi_Distribution();
                    internalDuration.parseStateOrTransition(state);
                    internalDuration.setWellKnownDuration(true);
                }

                if (inputReceiveTransition != null)
                {
                    if (!resultingWaitingTime.getWellKnownDuration()) //determine resulting waiting time
                    {
                        string subjectName = inputReceiveTransition.CellsU["Prop." + ALPSConstants.alpsPropertieTypeSenderOfMessage].ResultStr["none"];
                        string receivedMessageName = inputReceiveTransition.CellsU["Prop." + ALPSConstants.alpsPropertieTypeConnectorMessage].ResultStr["none"];

                        SiSi_Subject correspondingSiSiSubject = SiSi_SimpleSim.getSiSiSubject(subjectName);

                        string lastMessageSend = determineLastMessageSendTo(subjectName);

                        if (correspondingSiSiSubject != null)
                        {
                            SiSi_ResponseObject responseObject = correspondingSiSiSubject.getResponseObject(this.simpleSimSubject.getSubjectName(), lastMessageSend, receivedMessageName);

                            if (responseObject == null)
                            {
                                for (byte i = 1; responseObject == null & i <= SiSi_SimpleSim.getMaximumNumberOfResponseContextSkips(); i++)
                                {
                                    lastMessageSend = determinePreviousLastMessageSendTo(subjectName, i, 0);
                                    responseObject = correspondingSiSiSubject.getResponseObject(simpleSimSubject.getSubjectName(), lastMessageSend, receivedMessageName);
                                }
                            }

                            if (responseObject == null) //if still null, try to find a first send object
                            {
                                responseObject = correspondingSiSiSubject.getFirstSendPathsObject(simpleSimSubject.getSubjectName(), receivedMessageName);
                            }

                            if (responseObject != null)
                            {
                                setOriginalReceiveChanceValue(responseObject.getChanceValueForResponse());

                                SiSi_Distribution transmissionTimeInquiery = SiSi_SimpleSim.getTansmissionTimeFor(lastMessageSend);
                                if (transmissionTimeInquiery == null)
                                {
                                    transmissionTimeInquiery = new SiSi_Distribution();
                                    transmissionTimeInquiery.setWellKnownDuration(false);
                                }

                                SiSi_Distribution transmissionTimeIncoming = SiSi_SimpleSim.getTansmissionTimeFor(receivedMessageName);
                                if (transmissionTimeIncoming == null)
                                {
                                    transmissionTimeIncoming = new SiSi_Distribution();
                                    transmissionTimeIncoming.setWellKnownDuration(false);
                                }

                                timeFromSendToReceptionOfReply = new SiSi_Distribution();

                                timeFromSendToReceptionOfReply.addDistribution(transmissionTimeInquiery);
                                timeFromSendToReceptionOfReply.addDistribution(responseObject.getAverageDurationForResponse());
                                timeFromSendToReceptionOfReply.addDistribution(transmissionTimeIncoming);

                                SiSi_Distribution myOwnTimeToGetHere = parentPathTree.determineTimeToGetHere(correspondingSiSiSubject, lastMessageSend);

                                resultingWaitingTime = getTimeFromSendToReceiptionOfReply().substractDurationAndGiveResult(myOwnTimeToGetHere);
                                resultingWaitingTime.setWellKnownDuration(responseObject.getAverageDurationForResponse().getWellKnownDuration() &
                                    myOwnTimeToGetHere.getWellKnownDuration()); ///There maybe an response object but it not be certian yet
                            }
                            else //if responseObject is still nothing --> no response could be found yet
                            {
                                if (resultingWaitingTime == null)
                                {
                                    resultingWaitingTime = new SiSi_Distribution();
                                }

                                if (parentPathTree.parentPathTree != null) //if this is not a path second from an initial receivestate
                                {
                                    resultingWaitingTime.setWellKnownDuration(false);

                                    if (SiSi_SimpleSim.getErrorReportRun())
                                    {
                                        if (receivedMessageName == "")
                                        {
                                            SiSi_SimpleSim.addErrorMessageToReportCollection("receive transition: " + inputReceiveTransition.Name + " - Missing Message Name.");
                                            SiSi_SimpleSim.addReportMessage(new SiSi_ReportMessage(SiSi_ReportMessage.ReportType.Error, this.getSimpleSimSubject().getSubjectName(), this.getState().Name, "receive transition: " + inputReceiveTransition.Name + " - Missing Message Name.", "PathTree Z.361"));
                                        }
                                        else
                                        {
                                            SiSi_SimpleSim.addErrorMessageToReportCollection("receive transition: " + inputReceiveTransition.Name + " - could not get a response from: " + subjectName + " asking for message: " + receivedMessageName);
                                            SiSi_SimpleSim.addReportMessage(new SiSi_ReportMessage(SiSi_ReportMessage.ReportType.Error, this.getSimpleSimSubject().getSubjectName(), this.getState().Name, "receive transition: " + inputReceiveTransition.Name + " - could not get a response from: " + subjectName + " asking for message: " + receivedMessageName, "PathTree Z.366"));
                                        }
                                    }
                                }
                                else
                                {
                                    resultingWaitingTime.setWellKnownDuration(true);
                                }
                            }
                        }
                        else
                        {
                            if (parentPathTree.parentPathTree != null)
                            {
                                resultingWaitingTime.setWellKnownDuration(false);

                                if (SiSi_SimpleSim.getErrorReportRun())
                                {
                                    SiSi_SimpleSim.addErrorMessageToReportCollection("receive transition: " + inputReceiveTransition.Name + " - could not find corresponding Subject: " + subjectName);
                                    SiSi_SimpleSim.addReportMessage(new SiSi_ReportMessage(SiSi_ReportMessage.ReportType.Error, this.getSimpleSimSubject().getSubjectName(), this.getState().Name, "receive transition: " + inputReceiveTransition.Name + " - could not find corresponding Subject: " + subjectName, "PathTree Z.385"));
                                }
                            }
                        }
                    }
                    else
                    {
                        resultingWaitingTime.setWellKnownDuration(true);
                    }
                }

                getDurationIncludingSubTrees();
            }
        }


        private SiSi_Distribution determineTimeToGetHere(SiSi_Subject correspondenceSubject, string lastSendMessage)
        {
            SiSi_Distribution result = new SiSi_Distribution();
            result.addDistribution(internalDuration);
            result.setWellKnownDuration(internalDuration.getWellKnownDuration());


            SiSi_Distribution recursiveCalledDistribution;
            if (inquieryContextSubjectName != correspondenceSubject.getSubjectName() | inquieryContextMessageName != lastSendMessage) //original in VBA a Not(&)
            {
                if (parentPathTree != null)
                {
                    recursiveCalledDistribution = parentPathTree.determineTimeToGetHere(correspondenceSubject, lastSendMessage);
                    result.addDistribution(recursiveCalledDistribution);
                    if (result.getWellKnownDuration())
                    {
                        result.setWellKnownDuration(recursiveCalledDistribution.getWellKnownDuration());
                    }
                }
            }

            return result;
        }

        private string determineLastMessageSendTo(string subjectName)
        {
            string result = "";
            if (inquieryContextSubjectName == subjectName)
            {
                result = inquieryContextMessageName;
            }
            else
            {
                if (parentPathTree != null)
                {
                    result = parentPathTree.determineLastMessageSendTo(subjectName);
                }
            }
            return result;
        }


        //if a subject send more than one message (one after the other) to a correspondant must reply only to one of them
        // inquieryContext before the previous message must be asked. 
        private string determinePreviousLastMessageSendTo(string subjectName, byte inquieryContextToSkip, byte inquieryContextSkippedSoFar)
        {
            string result = "";

            if (inquieryContextSubjectName == subjectName)
            {
                if (inquieryContextSkippedSoFar < inquieryContextToSkip)
                {
                    if (parentPathTree != null)
                    {
                        result = parentPathTree.determinePreviousLastMessageSendTo(subjectName, inquieryContextToSkip, (byte)(inquieryContextSkippedSoFar + 1));
                    }
                }
                else
                {
                    result = inquieryContextMessageName;
                }
            }
            else if (checkIfThisIsAnotherReceiveFromTheSameSubject(inquieryContextSkippedSoFar, subjectName))
            {
                //if after a skip
                //do nothing anymore because if you have found a Response context matching to the original subjectName then
                //there there simply is no other message send
            }
            else
            {
                if (parentPathTree != null)
                {
                    result = parentPathTree.determinePreviousLastMessageSendTo(subjectName, inquieryContextToSkip, inquieryContextSkippedSoFar);
                }
            }

            return result;
        }


        private bool checkIfThisIsAnotherReceiveFromTheSameSubject(byte inquieryContextsSkippedSoFar, string subjectName)
        {
            bool result = false;

            if (inquieryContextsSkippedSoFar > 0)
            {
                if (inputReceiveTransition != null)
                {
                    string subjectNameOfReceive = inputReceiveTransition.CellsU["Prop." + ALPSConstants.alpsPropertieTypeSenderOfMessage].ResultStr["none"];
                    if (subjectName == subjectNameOfReceive)
                    {
                        result = true;
                    }
                }
            }
            return result;
        }


        private bool timeOfThisAndChildPathsIsKnown()
        {
            bool result = false;

            SiSi_PathTree path;

            if (childPaths.Count > 0)
            {
                result = true;
                foreach (Shape pathKey in childPaths.Keys)
                {
                    childPaths.TryGetValue(pathKey, out path);

                    if (path.getOverallDurationIncludingSubTrees().getWellKnownDuration() == false)
                    {
                        result = false;
                        break;
                    }
                }
            }
            result = result & internalDuration.getWellKnownDuration() & resultingWaitingTime.getWellKnownDuration();
            return result;
        }


        public int countStateOccurance(Shape state)
        {
            int result;
            if (state == this.state)
            {
                result = 1;
            }
            else
            {
                result = 0;
            }

            if (parentPathTree != null)
            {
                result += parentPathTree.countStateOccurance(state);
            }

            return result;
        }


        private SiSi_Distribution getIndividualCompleteDuration()
        {
            SiSi_Distribution result = new SiSi_Distribution();
            result.addDistribution(internalDuration);
            return result;
        }

        private SiSi_Distribution getDurationIncludingSubTrees()
        {
            if (!overallDurationIncludingSubTrees.getWellKnownDuration())
            {
                overallDurationIncludingSubTrees = new SiSi_Distribution();
                overallDurationIncludingSubTrees.setWellKnownDuration(true);

                overallWaitingDurationIncludingSubTrees = new SiSi_Distribution();
                overallWaitingDurationIncludingSubTrees.setWellKnownDuration(true);

                overallDurationIncludingSubTrees.addDistribution(internalDuration);
                overallDurationIncludingSubTrees.addDistribution(resultingWaitingTime);

                overallWaitingDurationIncludingSubTrees.addDistribution(resultingWaitingTime);

                SiSi_PathTree path;

                bool firstSetYet = false;
                double minValue = 0, minWait = 0, maxValue = 0, maxWait = 0;

                if (childPaths.Count > 0)
                {
                    SiSi_Distribution tempDuration;
                    SiSi_Distribution tempWaitingDuration;

                    foreach (Shape pathKey in childPaths.Keys)
                    {
                        childPaths.TryGetValue(pathKey, out path);

                        tempDuration = path.getDurationIncludingSubTrees();
                        tempWaitingDuration = path.getOverallWaitingDurationIncludingSubTrees();

                        overallDurationIncludingSubTrees.addDistributionWeighted(tempDuration, path.chanceValue);
                        overallWaitingDurationIncludingSubTrees.addDistributionWeighted(tempWaitingDuration, path.chanceValue);

                        if (SiSi_SimpleSim.getUseAbsoluteMinMax())
                        {
                            if (firstSetYet)
                            {
                                minValue = tempDuration.getMinValue();
                                maxValue = tempDuration.getMaxValue();
                                minWait = tempWaitingDuration.getMinValue();
                                maxWait = tempWaitingDuration.getMaxValue();
                                firstSetYet = true;
                            }
                            else
                            {
                                if (tempDuration.getMinValue() < minValue)
                                {
                                    minValue = tempDuration.getMinValue();
                                }

                                if (tempDuration.getMaxValue() > maxValue)
                                {
                                    maxValue = tempDuration.getMaxValue();
                                }

                                if (tempWaitingDuration.getMinValue() < minWait)
                                {
                                    minWait = tempWaitingDuration.getMinValue();
                                }

                                if (tempWaitingDuration.getMaxValue() > maxWait)
                                {
                                    maxWait = tempWaitingDuration.getMaxValue();
                                }
                            }
                        }

                    }

                }

                if (SiSi_SimpleSim.getUseAbsoluteMinMax())
                {
                    overallDurationIncludingSubTrees.setMinValue(minValue + internalDuration.getMinValue() + resultingWaitingTime.getMinValue());
                    overallDurationIncludingSubTrees.setMaxValue(maxValue + internalDuration.getMaxValue() + resultingWaitingTime.getMaxValue());
                    overallWaitingDurationIncludingSubTrees.setMinValue(minWait + resultingWaitingTime.getMinValue());
                    overallWaitingDurationIncludingSubTrees.setMaxValue(maxWait + resultingWaitingTime.getMaxValue());
                }
            }

            if (SiSi_SimpleSim.getWriteWaitingTimeForReceiveStates())
            {
                if (overallWaitingDurationIncludingSubTrees.getWellKnownDuration() & state != null)
                {
                    if (parentPathTree != null)
                    {
                        SiSi_Distribution timeToWrite = new SiSi_Distribution();
                        if (childPaths.Count > 0)
                        {
                            SiSi_PathTree path;

                            double minValue = Double.MaxValue;
                            double maxValue = Double.MinValue;

                            foreach (Shape pathKey in childPaths.Keys)
                            {
                                childPaths.TryGetValue(pathKey, out path);
                                timeToWrite.addDistributionWeighted(path.resultingWaitingTime, path.chanceValue);

                                if (SiSi_SimpleSim.getUseAbsoluteMinMax())
                                {
                                    if (path.resultingWaitingTime.getMinValue() < minValue)
                                    {
                                        minValue = path.resultingWaitingTime.getMinValue();
                                    }

                                    if (path.resultingWaitingTime.getMaxValue() > maxValue)
                                    {
                                        maxValue = path.resultingWaitingTime.getMaxValue();
                                    }
                                }
                            }

                            if (SiSi_SimpleSim.getUseAbsoluteMinMax())
                            {
                                timeToWrite.setMinValue(minValue);
                                timeToWrite.setMaxValue(maxValue);
                            }
                        }

                        if (state.CellExistsU["Prop." + ALPSConstants.simpleSimWaitingTimeFromLastRun, 1] != 0)
                        {
                            state.CellsU["Prop." + ALPSConstants.simpleSimWaitingTimeFromLastRun].Formula = "\"" + timeToWrite.toString(true) + "\"";
                        }
                    }
                    else
                    {
                        if (state.CellExistsU["Prop." + ALPSConstants.simpleSimWaitingTimeFromLastRun, 1] != 0)
                        {
                            state.CellsU["Prop." + ALPSConstants.simpleSimWaitingTimeFromLastRun].Formula = "\"" + simpleSimSubject.getWaitingTimeUntilActivation().toString(true) + "\"";
                        }
                    }
                }
            }

            return overallDurationIncludingSubTrees;
        }



        private bool createFollowUpPaths()
        {
            int[] outgoingConnectorShapeIDs = (int[])state.GluedShapes(VisGluedShapesFlags.visGluedShapesOutgoing1D, "");

            Page possibleTimeOutTransitionWithDuration = null;
            double timeOutChance = 0;
            double choiceChance = 100;

            double newChoicePercentage;

            Shape outgoingConnectorShape;
            foreach (int outgoingConnectorShapeID in outgoingConnectorShapeIDs)
            {
                outgoingConnectorShape = state.ContainingPage.Shapes.ItemFromID[(int)outgoingConnectorShapeID];

                if (outgoingConnectorShape.CellExistsU["Prop." + ALPSConstants.simpleSimTranstionChoiceChance, 1] != 0)
                {
                    choiceChance = Double.Parse(outgoingConnectorShape.CellsU["Prop." + ALPSConstants.simpleSimTranstionChoiceChance].ResultStr["none"]);
                }

                sumOfChildPathChances += choiceChance;
                numberOfChildPaths++;

                if (outgoingConnectorShape.HasCategory(ALPSConstants.alpsShapeCategoryTimeOutTransition))
                {
                    if (outgoingConnectorShape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeTimeOutType].ResultStr["none"].Contains("duration"))
                    {
                        if (possibleTimeOutTransitionWithDuration == null)
                        {
                            possibleTimeOutTransitionWithDuration = (Page)outgoingConnectorShape;

                            timeOutChance = computeTimeOutChanceForFunctionState(outgoingConnectorShape);

                        }
                        else
                        {
                            SiSi_SimpleSim.addErrorMessageToReportCollection("state: " + state.Name + " - (Label: " + stateLabel + "): only one duration based timeout Transition allowed");
                            SiSi_SimpleSim.addReportMessage(new SiSi_ReportMessage(SiSi_ReportMessage.ReportType.Error, this.getSimpleSimSubject().getSubjectName(), this.getState().Name, "state: " + state.Name + " - (Label: " + stateLabel + "): only one duration based timeout Transition allowed", "PathTree Z.732"));
                        }
                    }
                }
            }

            if (isEndState)
            {
                choiceChance = ALPSGlobalFunctions.convertPercentageFormulaToDouble(state.CellsU["Prop." + ALPSConstants.simpleSimStayChance].ResultStr["none"]);
                sumOfChildPathChances += choiceChance;
                numberOfChildPaths++;
            }

            foreach (long outgoingConnectorShapeID in outgoingConnectorShapeIDs)
            {
                outgoingConnectorShape = state.ContainingPage.Shapes.ItemFromID[(int)outgoingConnectorShapeID];

                Shape targetState = determineTargetStateOf(outgoingConnectorShape);
                SiSi_PathTree newChildPath = new SiSi_PathTree();

                choiceChance = 100;

                if (outgoingConnectorShape.CellExistsU["Prop." + ALPSConstants.simpleSimTranstionChoiceChance, 1] != 0)
                {
                    choiceChance = Double.Parse(outgoingConnectorShape.CellsU["Prop." + ALPSConstants.simpleSimTranstionChoiceChance].ResultStr["none"]);
                }

                newChoicePercentage = (choiceChance / sumOfChildPathChances) * (1 - timeOutChance);

                if (outgoingConnectorShape.HasCategory(ALPSConstants.alpsShapeCategoryTimeOutTransition))
                {
                    if (outgoingConnectorShape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeTimeOutType].ResultStr["none"].Contains("duration"))
                    {
                        newChoicePercentage = timeOutChance;
                    }
                }

                string sendingSubjectName = "";
                string receivedMessageName = "";

                if (outgoingConnectorShape.HasCategory(ALPSConstants.alpsShapeCategoryReceiveTransition))
                {
                    sendingSubjectName = outgoingConnectorShape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeSenderOfMessage].ResultStr["none"];
                    receivedMessageName = outgoingConnectorShape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeConnectorMessage].ResultStr["none"];

                    newChildPath.responseContextSubjectName = sendingSubjectName;
                    newChildPath.responseContextMessageName = receivedMessageName;

                    newChildPath.setInputReceiveTransition(outgoingConnectorShape);
                    newChildPath.setInputReceiveTransitionTimeOutTimeChance(timeOutChance);
                    newChildPath.setInputReceiveTransitionUserCancelChance(sumOfChildPathChances);

                    if (!simpleSimSubject.getDictionaryOfAllMessagesReceivedInSBD().ContainsKey(receivedMessageName))
                    {
                        simpleSimSubject.getDictionaryOfAllMessagesReceivedInSBD().Add(receivedMessageName, receivedMessageName);
                    }
                }
                string receivingSubjectName = "";
                string sendMessageName = "";
                if (outgoingConnectorShape.HasCategory(ALPSConstants.alpsShapeCategorySendTransition))
                {
                    receivingSubjectName = outgoingConnectorShape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeReceivingSubject].ResultStr["none"];
                    sendMessageName = outgoingConnectorShape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeConnectorMessage].ResultStr["none"];

                    newChildPath.inquieryContextSubjectName = receivingSubjectName; //this is always true because even if this send is response to a previous message it may also inuquier a counter response from the original asking subject
                    newChildPath.inquieryContextMessageName = sendMessageName;

                    if (this.isInAResponseContextForSubject(receivingSubjectName))
                    {
                        backtraceAndRegisterResponseTime(null, receivingSubjectName, sendMessageName);
                    }
                    else
                    {
                        backtraceAndRegisterFirstSendPath(null, receivingSubjectName, sendMessageName);
                    }

                    if (!simpleSimSubject.getDictionaryOfAllMessagesSendInSBD().ContainsKey(sendMessageName))
                    {
                        simpleSimSubject.getDictionaryOfAllMessagesSendInSBD().Add(sendMessageName, sendMessageName);
                    }
                }

                bool pathvalid = newChildPath.initialize(simpleSimSubject, this, targetState, newChoicePercentage);

                if (pathvalid)
                {
                    childPaths.Add(outgoingConnectorShape, newChildPath);
                }

                if (!simpleSimSubject.getAllElementsInTree().ContainsKey(outgoingConnectorShapeID))
                {
                    simpleSimSubject.getAllElementsInTree().Add(outgoingConnectorShapeID, outgoingConnectorShape);

                    string connectorLabel = outgoingConnectorShape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeLabel].ResultStr["none"];
                    connectorLabel = ALPSGlobalFunctions.removeLineBreaks(connectorLabel);

                    if (!pathvalid & targetState == null)
                    {
                        SiSi_SimpleSim.addErrorMessageToReportCollection("transition: " + outgoingConnectorShape.Name + " - (Label: " +
                            connectorLabel + ") has no valid target");
                        SiSi_SimpleSim.addReportMessage(new SiSi_ReportMessage(SiSi_ReportMessage.ReportType.Error, this.getSimpleSimSubject().getSubjectName(), this.getState().Name, "transition: " + outgoingConnectorShape.Name + " - (Label: " +
                            connectorLabel + ") has no valid target", "Pathtree Z.832"));
                    }

                    if (state.HasCategory(ALPSConstants.alpsShapeCategorySendState))
                    {
                        if (outgoingConnectorShape.HasCategory(ALPSConstants.alpsShapeCategoryReceiveTransition) |
                            outgoingConnectorShape.HasCategory(ALPSConstants.alpsShapeCategoryStandardTransition))
                        {
                            SiSi_SimpleSim.addErrorMessageToReportCollection("state. " + state.Name + " - (Label: " + stateLabel
                                + ") has invalid outgoing Transition: " + outgoingConnectorShape.Name + " - (Label: " + connectorLabel + ")");
                            SiSi_SimpleSim.addReportMessage(new SiSi_ReportMessage(SiSi_ReportMessage.ReportType.Error, this.getSimpleSimSubject().getSubjectName(), this.getState().Name, "state. " + state.Name + " - (Label: " + stateLabel
                                + ") has invalid outgoing Transition: " + outgoingConnectorShape.Name + " - (Label: " + connectorLabel + ")", "Pathtree Z.843"));

                        }
                    }
                    else if (state.HasCategory(ALPSConstants.alpsShapeCategoryReceiveState))
                    {
                        if (outgoingConnectorShape.HasCategory(ALPSConstants.alpsShapeCategorySendTransition) |
                            outgoingConnectorShape.HasCategory(ALPSConstants.alpsShapeCategoryStandardTransition))
                        {
                            SiSi_SimpleSim.addErrorMessageToReportCollection("state. " + state.Name + " - (Label: " + stateLabel
                                + ") has invalid outgoing Transition: " + outgoingConnectorShape.Name + " - (Label: " + connectorLabel + ")");
                            SiSi_SimpleSim.addReportMessage(new SiSi_ReportMessage(SiSi_ReportMessage.ReportType.Error, this.getSimpleSimSubject().getSubjectName(), this.getState().Name, "state. " + state.Name + " - (Label: " + stateLabel
                                + ") has invalid outgoing Transition: " + outgoingConnectorShape.Name + " - (Label: " + connectorLabel + ")", "Pathtree Z.855"));

                        }
                    }
                    else if (state.HasCategory(ALPSConstants.alpsShapeCategoryFunctionState))
                    {
                        if (outgoingConnectorShape.HasCategory(ALPSConstants.alpsShapeCategoryReceiveTransition) |
                            outgoingConnectorShape.HasCategory(ALPSConstants.alpsShapeCategorySendTransition))
                        {
                            SiSi_SimpleSim.addErrorMessageToReportCollection("state. " + state.Name + " - (Label: " + stateLabel
                                + ") has invalid outgoing Transition: " + outgoingConnectorShape.Name + " - (Label: " + connectorLabel + ")");
                            SiSi_SimpleSim.addReportMessage(new SiSi_ReportMessage(SiSi_ReportMessage.ReportType.Error, this.getSimpleSimSubject().getSubjectName(), this.getState().Name, "state. " + state.Name + " - (Label: " + stateLabel
                                + ") has invalid outgoing Transition: " + outgoingConnectorShape.Name + " - (Label: " + connectorLabel + ")", "Pathtree Z.867"));

                        }
                    }

                    if (outgoingConnectorShape.HasCategory(ALPSConstants.alpsShapeCategorySendTransition))
                    {
                        if (receivingSubjectName == "")
                        {
                            SiSi_SimpleSim.addErrorMessageToReportCollection("transition: " + outgoingConnectorShape.Name + " - Missing receiving subject");
                            SiSi_SimpleSim.addReportMessage(new SiSi_ReportMessage(SiSi_ReportMessage.ReportType.Error, this.getSimpleSimSubject().getSubjectName(), this.getState().Name, "transition: " + outgoingConnectorShape.Name + " - Missing receiving subject", "Pathtree Z.877"));
                        }

                        if (sendMessageName == "")
                        {
                            SiSi_SimpleSim.addErrorMessageToReportCollection("transition: " + outgoingConnectorShape.Name + " - Missing send message");
                            SiSi_SimpleSim.addReportMessage(new SiSi_ReportMessage(SiSi_ReportMessage.ReportType.Error, this.getSimpleSimSubject().getSubjectName(), this.getState().Name, "transition: " + outgoingConnectorShape.Name + " - Missing send message", "Pathtree Z.883"));
                        }
                    }

                    else if (outgoingConnectorShape.HasCategory(ALPSConstants.alpsShapeCategoryReceiveTransition))
                    {
                        if (sendingSubjectName == "")
                        {
                            SiSi_SimpleSim.addErrorMessageToReportCollection("transition: " + outgoingConnectorShape.Name + " - Missing sending subject");
                            SiSi_SimpleSim.addReportMessage(new SiSi_ReportMessage(SiSi_ReportMessage.ReportType.Error, this.getSimpleSimSubject().getSubjectName(), this.getState().Name, "transition: " + outgoingConnectorShape.Name + " - (Label: " +
                            connectorLabel + ") has no valid target", "Pathtree Z.832"));
                        }

                        if (receivedMessageName == "")
                        {
                            SiSi_SimpleSim.addErrorMessageToReportCollection("transition: " + outgoingConnectorShape.Name + " - Missing received message");
                            SiSi_SimpleSim.addReportMessage(new SiSi_ReportMessage(SiSi_ReportMessage.ReportType.Error, this.getSimpleSimSubject().getSubjectName(), this.getState().Name, "transition: " + outgoingConnectorShape.Name + " - (Label: " +
                            connectorLabel + ") has no valid target", "Pathtree Z.832"));
                        }
                    }
                }
            }

            if (isEndState)
            {
                choiceChance = ALPSGlobalFunctions.convertPercentageFormulaToDouble(state.CellsU["Prop." + ALPSConstants.simpleSimStayChance].ResultStr["none"]);
                newChoicePercentage = choiceChance / sumOfChildPathChances;

                SiSi_PathTree newChildPath = new SiSi_PathTree();
                newChildPath.initalizeAsTerminalPath(newChoicePercentage, simpleSimSubject, this);
                childPaths.Add(state, newChildPath);
            }

            if (state.HasCategory(ALPSConstants.alpsShapeCategorySendState))
            {
                if (outgoingConnectorShapeIDs.Length != 1)
                {
                    SiSi_SimpleSim.addErrorMessageToReportCollection("state: " + state.Name + " - (Label: " +
                        stateLabel + ") - send states should have one outgoing send transition! Transition counted: " + outgoingConnectorShapeIDs.Length);
                    SiSi_SimpleSim.addReportMessage(new SiSi_ReportMessage(SiSi_ReportMessage.ReportType.Error, this.getSimpleSimSubject().getSubjectName(), this.getState().Name, "state: " + state.Name + " - (Label: " +
                        stateLabel + ") - send states should have one outgoing send transition! Transition counted: " + outgoingConnectorShapeIDs.Length, "Pathtree Z.922"));
                }
            }

            if (state.HasCategory(ALPSConstants.alpsShapeCategoryReceiveState))
            {
                if (outgoingConnectorShapeIDs.Length <= 0)
                {
                    SiSi_SimpleSim.addErrorMessageToReportCollection("state: " + state.Name + " - (Label: " +
                        stateLabel + ") - receive states should have at least one outgoing receive transition! Transition counted: " + outgoingConnectorShapeIDs.Length);
                    SiSi_SimpleSim.addReportMessage(new SiSi_ReportMessage(SiSi_ReportMessage.ReportType.Error, this.getSimpleSimSubject().getSubjectName(), this.getState().Name, "state: " + state.Name + " - (Label: " +
                        stateLabel + ") - receive states should have at least one outgoing receive transition! Transition counted: " + outgoingConnectorShapeIDs.Length, "Pathtree Z.934"));
                }
            }

            bool result = false;

            if (childPaths.Count > 0)
            {
                result = true;

                if (!simpleSimSubject.getAllElementsInTree().ContainsKey(state.ID))
                {
                    simpleSimSubject.getAllElementsInTree().Add(state.ID, state);
                }
            }
            return result;
        }


        private double computeTimeOutChanceForFunctionState(Shape timeOutConnectorShape) //why state as parameter? should be static?
        {
            double result = 0;

            if (internalDuration.getMeanValue() == 0)
            {
                internalDuration.parseStateOrTransition(state);
            }

            double timeOutTimeForPossibleTransition = ALPSGlobalFunctions.decodeXmlDayTimeDurationToFractionsOfDays(
                timeOutConnectorShape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeTimeOutTime].ResultStr["none"]);

            if (internalDuration.getMaxValue() < timeOutTimeForPossibleTransition)
            {
                if (internalDuration.getMeanValue() > 0)
                {
                    SiSi_SimpleSim.addErrorMessageToReportCollection("state: " + state.Name + " - Label: " + stateLabel + "). Warning! State max Value < timeOutValue! time-out branch will not be counted");
                    SiSi_SimpleSim.addReportMessage(new SiSi_ReportMessage(SiSi_ReportMessage.ReportType.Warning, this.getSimpleSimSubject().getSubjectName(), this.getState().Name, "state: " + state.Name + " - Label: " + stateLabel + "). " +
                        "Warning! State max Value < timeOutValue! time-out branch will not be counted", "Pathtree Z.970"));

                }
            }
            else
            {
                double tempStandardDeviationToUse = internalDuration.getStandardDeviation();
                if (tempStandardDeviationToUse < 0)
                {
                    tempStandardDeviationToUse = (internalDuration.getMaxValue() - internalDuration.getMeanValue()) / SiSi_SimpleSim.getNumberOfSigmasForMinMax();
                }

                double deviationOfTimeouttimeFromMeanValueInSigmas = (timeOutTimeForPossibleTransition - internalDuration.getMeanValue()) / tempStandardDeviationToUse;

                if (deviationOfTimeouttimeFromMeanValueInSigmas <= 0.67449)
                    result = 0.25;
                else if (deviationOfTimeouttimeFromMeanValueInSigmas <= 0.994458)
                    result = 0.16;
                else if (deviationOfTimeouttimeFromMeanValueInSigmas <= 1)
                    result = 0.158655254;
                else if (deviationOfTimeouttimeFromMeanValueInSigmas <= 1.281552)
                    result = 0.1;
                else if (deviationOfTimeouttimeFromMeanValueInSigmas <= 1.1644854)
                    result = 0.05;
                else if (deviationOfTimeouttimeFromMeanValueInSigmas <= 1.959964)
                    result = 0.025;
                else if (deviationOfTimeouttimeFromMeanValueInSigmas <= 2)
                    result = 0.022750132;
                else if (deviationOfTimeouttimeFromMeanValueInSigmas <= 2.575829)
                    result = 0.005;
                else if (deviationOfTimeouttimeFromMeanValueInSigmas <= 3)
                    result = 0.001349898;
                else if (deviationOfTimeouttimeFromMeanValueInSigmas <= 3.290527)
                    result = 0.0005;
                else if (deviationOfTimeouttimeFromMeanValueInSigmas <= 3.890592)
                    result = 0.00005;
                else if (deviationOfTimeouttimeFromMeanValueInSigmas <= 4)
                    result = 0.00003167;
                else if (deviationOfTimeouttimeFromMeanValueInSigmas <= 4.417173)
                    result = 0.000005;
                else if (deviationOfTimeouttimeFromMeanValueInSigmas <= 4.891638)
                    result = 0.0000005;
                else if (deviationOfTimeouttimeFromMeanValueInSigmas <= 5)
                    result = 0.0000002866515;
                else if (deviationOfTimeouttimeFromMeanValueInSigmas <= 5.326724)
                    result = 0.00000005;
                else if (deviationOfTimeouttimeFromMeanValueInSigmas <= 5.730729)
                    result = 0.000000005;
                else if (deviationOfTimeouttimeFromMeanValueInSigmas <= 6)
                    result = 0.0000000009865;
                else
                    result = 0;
            }

            return result;
        }


        private Shape determineTargetStateOf(Shape connectorShape)
        {
            Shape result = null;
            foreach (Connect connection in connectorShape.Connects)
            {
                if (connection.FromCell.Name != "BeginX")
                {
                    result = connection.ToSheet;
                    if (result.HasCategory(ALPSConstants.alpsShapeCategorySBDState))
                    {
                        return result;
                    }
                }
            }
            return result;
        }

        private int maxDepth()
        {
            int result = 0;
            if (!isTerminalPath)
            {
                result = 1;

                int tempMaxDepth = Int32.MinValue;
                int tempDepth;
                SiSi_PathTree path;

                foreach (Shape pathKey in childPaths.Keys)
                {
                    childPaths.TryGetValue(pathKey, out path);
                    tempDepth = path.maxDepth();

                    if (tempDepth > tempMaxDepth)
                    {
                        tempMaxDepth = tempDepth;
                    }
                }

                result += tempMaxDepth;

            }

            return result;
        }


        private bool isInAInquieryContextFromSubject(string sendingSubject)
        {
            bool result = false;

            if (String.Compare(inquieryContextSubjectName, sendingSubject) == 0)
            {
                result = true;
            }
            else
            {
                if (parentPathTree != null)
                {
                    result = parentPathTree.isInAInquieryContextFromSubject(sendingSubject);
                }
            }
            return result;
        }


        private bool isInAResponseContextForSubject(string receivingSubjectName)
        {
            bool result = false;

            if (String.Compare(responseContextSubjectName, receivingSubjectName) == 0)
            {
                result = true;
            }
            else
            {
                if (parentPathTree != null)
                {
                    result = parentPathTree.isInAResponseContextForSubject(receivingSubjectName);
                }
            }
            return result;
        }


        private void backtraceAndRegisterResponseTime(SiSi_ResponsePath responsePath, string receivingSubjectName, string responseMessageName)
        {
            SiSi_ResponsePath localResponsePath;


            //Should be implemented in Constructor
            if (internalDuration == null)
                internalDuration = new SiSi_Distribution();

            if (responsePath == null)
            {
                localResponsePath = new SiSi_ResponsePath();
                localResponsePath.getPathDuration().copyValuesOf(internalDuration);
            }
            else
            {
                localResponsePath = responsePath;
                localResponsePath.getPathDuration().addDistribution(internalDuration);
            }

            localResponsePath.addPathTreeObject(this);
            localResponsePath.setChanceValue(chanceValue * localResponsePath.getChanceValue());

            if (!resultingWaitingTime.getWellKnownDuration())
            {
                localResponsePath.getPathDuration().setWellKnownDuration(false);
            }

            if (receivingSubjectName == responseContextSubjectName)
            {
                localResponsePath.setInquieryMessage(responseContextMessageName);
                localResponsePath.setCorrespondenceSubject(receivingSubjectName);
                localResponsePath.setResponseMessage(responseMessageName);
                ///Register response Object with this Object as responsepath

                simpleSimSubject.addResponsePath(localResponsePath);
            }
            else if (parentPathTree != null)
            {
                parentPathTree.backtraceAndRegisterResponseTime(localResponsePath, receivingSubjectName, responseMessageName);
            }
            else
            {
                ///Debug Case
            }
        }


        public void backtraceAndRegisterFirstSendPath(SiSi_ResponsePath firstSendPath, string receivingSubjectName, string firstSendMessageName)
        {
            SiSi_ResponsePath localFirstSendPath;

            if (!internalDuration.getWellKnownDuration()) //by error add internalDuration == null
            {
                internalDuration = new SiSi_Distribution();
                internalDuration.parseStateOrTransition(state);
                internalDuration.setWellKnownDuration(true);
            }

            if (firstSendPath == null)
            {
                localFirstSendPath = new SiSi_ResponsePath();
                localFirstSendPath.getPathDuration().copyValuesOf(internalDuration);
            }
            else
            {
                localFirstSendPath = firstSendPath;
                localFirstSendPath.getPathDuration().addDistribution(internalDuration);
            }

            localFirstSendPath.addPathTreeObject(this);

            localFirstSendPath.setChanceValue(this.getChanceValue() * localFirstSendPath.getChanceValue());

            if (!resultingWaitingTime.getWellKnownDuration())
            {
                localFirstSendPath.getPathDuration().setWellKnownDuration(false);
            }

            if (parentPathTree == null) //Root of Tree have been reached
            {
                localFirstSendPath.setCorrespondenceSubject(receivingSubjectName);
                localFirstSendPath.setResponseMessage(firstSendMessageName);

                if (simpleSimSubject.getWaitingTimeUntilActivation() != null)
                {
                    localFirstSendPath.setPreviousFirstSendDuration(simpleSimSubject.getWaitingTimeUntilActivation().getCopy());
                }
                simpleSimSubject.addFirstSendPath(localFirstSendPath);
            }
            else
            {
                parentPathTree.backtraceAndRegisterFirstSendPath(localFirstSendPath, receivingSubjectName, firstSendMessageName);
            }
        }

        //Checks how often the current state is already occured
        private void checkStateOccurence()
        {
            stateInPathOccurence = this.countStateOccurance(getState());
        }

        public void addChildPath(Shape shape, SiSi_PathTree childPath)
        {
            childPaths.Add(shape, childPath);
        }

        public void sumUpChancesForPathsToEndState(double multipliedChancesSoFar)
        {
            if (!isTerminalPath)
            {
                SiSi_PathTree myPath;
                foreach (Shape key in childPaths.Keys)
                {
                    childPaths.TryGetValue(key, out myPath);
                    myPath.sumUpChancesForPathsToEndState(multipliedChancesSoFar * chanceValue);
                }
            }
            else
            {
                Shape endState = parentPathTree.getState();
                Dictionary<Shape, double> tempDict = simpleSimSubject.getDictionaryOfSummedUpEndStateChances();
                if (tempDict.ContainsKey(endState))
                {
                    double valueSoFar;
                    tempDict.TryGetValue(endState, out valueSoFar);
                    valueSoFar += multipliedChancesSoFar * chanceValue;
                    tempDict.Remove(endState);
                    tempDict.Add(endState, valueSoFar);
                }
                else
                {
                    tempDict.Add(endState, multipliedChancesSoFar * chanceValue);
                }
            }
        }

        //+++
        //getter methods
        //+++
        public SiSi_Subject getSimpleSimSubject() { return simpleSimSubject; }
        public Shape getState() { return state; }
        public string getStateLabel() { return stateLabel; }

        public SiSi_PathTree getParentTree() { return parentPathTree; }
        public Shape getInputReceiveTransition() { return inputReceiveTransition; }
        public double getInputReceiveTransitionTimeOutTimeChance() { return inputReceiveTransitionTimeOutTimeChance; }
        public double getInputReceiveTransitionUserCancelChance() { return inputReceivetransitionUserCancelChance; }


        public Dictionary<Shape, SiSi_PathTree> getChildPaths() { return childPaths; }
        public int getNumberOfChildPaths() { return numberOfChildPaths; }
        public double getSumOfChildPathChances() { return sumOfChildPathChances; }

        public double getChanceValue() { return chanceValue; }
        public double getOriginalReceiveChanceValue() { return originalReceiveChanceValue; }

        public SiSi_Distribution getInternalDuration() { return internalDuration; }
        public SiSi_Distribution getTimeFromSendToReceiptionOfReply() { return timeFromSendToReceptionOfReply; }
        public SiSi_Distribution getResultWaitingTime() { return resultingWaitingTime; }

        public SiSi_Distribution getOverallDurationIncludingSubTrees() { return overallDurationIncludingSubTrees; }
        public SiSi_Distribution getOverallWaitingDurationIncludingSubTrees() { return overallWaitingDurationIncludingSubTrees; }

        public bool getIsEndState() { return isEndState; }
        public bool getIsTerminalPath() { return isTerminalPath; }

        public int getStateInPathOccurence() { return stateInPathOccurence; }

        public string getResponseContextSubjectName() { return responseContextSubjectName; }
        public string getResponseContextMessageName() { return responseContextMessageName; }

        public string getInquieryContextSubjectName() { return inquieryContextSubjectName; }
        public string getInquieryContextMessageName() { return inquieryContextMessageName; }
        //---
        //getter methods
        //---

        //+++
        //setter methods
        //+++
        public void setSimpleSimSubject(SiSi_Subject subject) { simpleSimSubject = subject; }
        public void setState(Shape state) { this.state = state; }
        public void setStateLabel(string label) { stateLabel = label; }

        public void setParentTree(SiSi_PathTree parentTree) { parentPathTree = parentTree; }
        public void setInputReceiveTransition(Shape transition) { inputReceiveTransition = transition; }
        public void setInputReceiveTransitionTimeOutTimeChance(double chance) { inputReceiveTransitionTimeOutTimeChance = chance; }
        public void setInputReceiveTransitionUserCancelChance(double chance) { inputReceivetransitionUserCancelChance = chance; }


        public void setChildPaths(Dictionary<Shape, SiSi_PathTree> childPaths) { this.childPaths = childPaths; }
        public void setNumberOfChildPaths(int number) { numberOfChildPaths = number; }
        public void setSumOfChildPathChances(double sum) { sumOfChildPathChances = sum; }

        public void setChanceValue(double chance) { chanceValue = chance; }
        public void setOriginalReceiveChanceValue(double chance) { originalReceiveChanceValue = chance; }

        public void setInternalDuration(SiSi_Distribution duration) { internalDuration = duration; }
        public void setTimeFromSendToReceiptionOfReply(SiSi_Distribution time) { timeFromSendToReceptionOfReply = time; }
        public void setResultWaitingTime(SiSi_Distribution time) { resultingWaitingTime = time; }

        public void setOverallDurationIncludingSubTrees(SiSi_Distribution duration) { overallDurationIncludingSubTrees = duration; }
        public void setOverallWaitingDurationIncludingSubTrees(SiSi_Distribution duration) { overallWaitingDurationIncludingSubTrees = duration; }

        public void setIsEndState(bool endState) { isEndState = endState; }
        public void setIsTerminalPath(bool terminalPath) { isTerminalPath = terminalPath; }

        public void setResponseContextSubjectName(string name) { responseContextSubjectName = name; }
        public void setResponseContextMessageName(string name) { responseContextMessageName = name; }

        public void setInquieryContextSubjectName(string name) { inquieryContextSubjectName = name; }
        public void setInquieryContextMessageName(string name) { inquieryContextMessageName = name; }
        //---
        //setter methods
        //---
    }
}
