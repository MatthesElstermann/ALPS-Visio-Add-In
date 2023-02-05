using System;
using System.Collections.Generic;

namespace VisioAddIn.SiSi
{
    class SiSi_ResponseObject
    {
        private ICollection<SiSi_ResponsePath> responsePathCollection;
        private SiSi_Distribution averageDurationForResponse;
        private double chanceValueForResponse;
        private string correspondenceSubject;
        private string inquieryMessage;
        private string responseMessage;

        public SiSi_ResponseObject()
        {
            responsePathCollection = new List<SiSi_ResponsePath>();
            averageDurationForResponse = new SiSi_Distribution();
        }

        public void tryToCalculateChanceAndTimeForResponse()
        {
            averageDurationForResponse = new SiSi_Distribution();

            double chanceWeightSum = 0;

            if (responsePathCollection.Count > 0)
            {
                setChanceValueForResponse(0);

                foreach (SiSi_ResponsePath path in getResponsePathCollection())
                {
                    chanceWeightSum += path.getChanceValue();
                }

                double minValue = Double.MaxValue;
                double maxValue = Double.MinValue;

                foreach (SiSi_ResponsePath path in getResponsePathCollection())
                {
                    if (!path.getPathDuration().getWellKnownDuration())
                    {
                        path.calculateTimeAndChance();
                    }

                    if (getAverageDurationForResponse().getWellKnownDuration())
                    {
                        getAverageDurationForResponse().setWellKnownDuration(path.getPathDuration().getWellKnownDuration());
                    }

                    if (chanceWeightSum > 0)
                    {
                        getAverageDurationForResponse().addDistributionWeighted(path.getPathDuration(), path.getChanceValue() / chanceWeightSum);
                    }
                    else
                    {
                        SiSi_SimpleSim.addErrorMessageToReportCollection("Warning: 0 Chance Paths in " + path.getSubject());
                    }
                    if (SiSi_SimpleSim.getUseAbsoluteMinMax())
                    {
                        if (path.getPathDuration().getMinValue() < minValue)
                        {
                            minValue = path.getPathDuration().getMinValue();
                        }
                        if (path.getPathDuration().getMaxValue() > maxValue)
                        {
                            maxValue = path.getPathDuration().getMaxValue();
                        }
                    }

                    chanceValueForResponse += path.getChanceValue();
                }

                if (SiSi_SimpleSim.getUseAbsoluteMinMax())
                {
                    getAverageDurationForResponse().setMinValue(minValue);
                    getAverageDurationForResponse().setMaxValue(maxValue);
                }
            }
        }

        public string toString(bool includeAllPaths)
        {
            string result = " ---Response Object--- ";
            result += System.Environment.NewLine + " - Inquiery Subject: " + getCorrespondenceSubject() + " - Inquiery Message: " + getInquieryMessage() + " - Response Message: " + getResponseMessage();
            result += System.Environment.NewLine + " - Average Time for response: " + getAverageDurationForResponse().toString(true);
            result += System.Environment.NewLine + " - Time for response well known: " + getAverageDurationForResponse().getWellKnownDuration();
            result += System.Environment.NewLine + " - Chance Value for this response: " + getChanceValueForResponse();
            result += System.Environment.NewLine + " - Number of Response Paths: " + getResponsePathCollection().Count;

            if (includeAllPaths)
            {
                if ((responsePathCollection.Count > 0))
                {
                    foreach (SiSi_ResponsePath tempResponsePath in responsePathCollection)
                    {
                        result += System.Environment.NewLine + "   * responsePath state: " + tempResponsePath.toString();
                        result += System.Environment.NewLine + "   * responsePath known for sure: " + tempResponsePath.getPathDuration().getWellKnownDuration();
                        result += System.Environment.NewLine + "   * responsePath durtaion: " + tempResponsePath.getPathDuration().toString(false);
                        result += System.Environment.NewLine + "   * responsePath chance: " + tempResponsePath.getChanceValue();
                    }

                }
            }
            return result;
        }

        public bool allPathsInCollectionReady(ICollection<SiSi_ResponsePath> collection)
        {
            bool result = true;
            foreach (SiSi_ResponsePath path in collection)
            {
                if (!path.getPathDuration().getWellKnownDuration())
                {
                    result = false;
                    break;
                }
            }
            return result;
        }


        //+++
        //setter-methods
        //+++
        public void setResponsePathCollection(ICollection<SiSi_ResponsePath> collection) { responsePathCollection = collection; }
        public void setAverageDurationForResponse(SiSi_Distribution duration) { averageDurationForResponse = duration; }
        public void setChanceValueForResponse(double chance) { chanceValueForResponse = chance; }
        public void setCorrespondenceSubject(string subject) { correspondenceSubject = subject; }
        public void setInquieryMessage(string message) { inquieryMessage = message; }
        public void setResponseMessage(string message) { responseMessage = message; }
        //---
        //setter-methods
        //---

        //add method
        public void addResponsePath(SiSi_ResponsePath path) { responsePathCollection.Add(path); }

        //+++
        //getter-methods
        //+++
        public ICollection<SiSi_ResponsePath> getResponsePathCollection() { return responsePathCollection; }
        public SiSi_Distribution getAverageDurationForResponse() { return averageDurationForResponse; }
        public double getChanceValueForResponse() { return chanceValueForResponse; }
        public string getCorrespondenceSubject() { return correspondenceSubject; }
        public string getInquieryMessage() { return inquieryMessage; }
        public string getResponseMessage() { return responseMessage; }
        //---
        //getter-methods
        //---
    }
}
