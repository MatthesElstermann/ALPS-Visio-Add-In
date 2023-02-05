using System.Collections.Generic;
using System.Linq;

namespace VisioAddIn.SiSi
{
    class SiSi_ResponsePath
    {
        private SiSi_Distribution pathDuration;
        private double chanceValue;

        private string subject;

        private string correspondenceSubject;
        private string inquieryMessage;
        private string responseMessage;

        private ICollection<SiSi_PathTree> pathTreeObjects;
        private SiSi_Distribution previousFirstSendDuration;

        public SiSi_ResponsePath()
        {
            pathTreeObjects = new List<SiSi_PathTree>();
            setChanceValue(1);
            setPathDuration(new SiSi_Distribution());
        }

        public string toString()
        {
            string result = "Response Path for: " + getResponseMessage() + " as response to: " + getInquieryMessage() + " from: " + getCorrespondenceSubject();
            for (int i = 0; i <= getPathTreeObjects().Count; i++)
            {
                SiSi_PathTree tempPath = getPathTreeObjects().ElementAt(i);
                result += System.Environment.NewLine + "state " + i + ": " + tempPath.getState().Name;
            }

            return result;
        }

        public void calculateTimeAndChance()
        {
            pathDuration = new SiSi_Distribution();
            getPathDuration().setWellKnownDuration(true);
            setChanceValue(1);

            for (int i = 0; i < getPathTreeObjects().Count; i++)
            {
                SiSi_PathTree tempPath = getPathTreeObjects().ElementAt(i);

                setChanceValue(getChanceValue() * tempPath.getChanceValue());

                getPathDuration().addDistribution(tempPath.getInternalDuration());

                string subjectName = "-1";
                if (tempPath.getInputReceiveTransition() != null)
                {
                    subjectName = tempPath.getInputReceiveTransition().CellsU["Prop." + ALPSConstants.alpsPropertieTypeSenderOfMessage].ResultStr["none"];

                }
                if (getCorrespondenceSubject() != subjectName)
                {
                    getPathDuration().addDistribution(tempPath.getResultWaitingTime());
                }
                if (this.getPathDuration().getWellKnownDuration())
                {
                    this.getPathDuration().setWellKnownDuration(tempPath.getResultWaitingTime().getWellKnownDuration());
                }

            }
        }

        //+++
        //setter-methods
        //+++
        public void setPathDuration(SiSi_Distribution pathDuration) { this.pathDuration = pathDuration; }
        public void setChanceValue(double chanceValue) { this.chanceValue = chanceValue; }
        public void setSubject(string subject) { this.subject = subject; }
        public void setCorrespondenceSubject(string correspondenceSubject) { this.correspondenceSubject = correspondenceSubject; }
        public void setInquieryMessage(string inquieryMessage) { this.inquieryMessage = inquieryMessage; }
        public void setResponseMessage(string responseMessage) { this.responseMessage = responseMessage; }
        public void setPathTreeObjects(ICollection<SiSi_PathTree> pathTreeObjects) { this.pathTreeObjects = pathTreeObjects; }
        public void setPreviousFirstSendDuration(SiSi_Distribution duration) { previousFirstSendDuration = duration; }
        //---
        //setter-methods
        //---

        //add-methods
        public void addPathTreeObject(SiSi_PathTree pathTree) { pathTreeObjects.Add(pathTree); }

        //+++
        //getter-methods
        //+++
        public SiSi_Distribution getPathDuration() { return pathDuration; }
        public double getChanceValue() { return chanceValue; }
        public string getSubject() { return subject; }
        public string getCorrespondenceSubject() { return correspondenceSubject; }
        public string getInquieryMessage() { return inquieryMessage; }
        public string getResponseMessage() { return responseMessage; }
        public ICollection<SiSi_PathTree> getPathTreeObjects() { return pathTreeObjects; }
        public SiSi_Distribution getPreviousFirstSendDuration() { return previousFirstSendDuration; }
        //---
        //getter-methods
        //--

    }

}
