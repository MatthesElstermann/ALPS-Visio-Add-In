using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;
using System.Collections.Generic;
using System.Linq;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioReceiveTransition : ReceiveTransition, IVisioImportableWithShape
    {
        private const string shapeType = Constants.SBDMasters.ReceiveTransition;
        
        private readonly IShapeImport import;
        public VisioReceiveTransition(IState sourceState, IState targetState, string labelForID = null, ITransitionCondition transitionCondition = null, ITransition.TransitionType transitionType = ITransition.TransitionType.Standard, ISet<IDataMappingIncomingToLocal> dataMappingIncomingToLocal = null, int priorityNumber = 0, string comment = null, string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null) : base(sourceState, targetState, labelForID, transitionCondition, transitionType, dataMappingIncomingToLocal, priorityNumber, comment, additionalLabel, additionalAttribute) { import = new TransitionImport(this); }
        protected VisioReceiveTransition() { import = new TransitionImport(this); }

        public void ImportToVisio(Visio.Page page)
        {
            import.Import(shapeType, page, VH.GetBounds(this));

            // TODO: VH and stuff

            // sender
            ISubject sender = getTransitionCondition().getMessageSentFrom();
            if (sender != null && sender.getModelComponentLabels().Count > 0)
            {
                VH.SetUser(import.GetShape(), Constants.Properties.Transition.ReceiverSenderListForSubject, ";" + sender.getModelComponentLabelsAsStrings()[0]);
                VH.SetUser(import.GetShape(), Constants.Properties.Transition.ReceiverSenderListForSubjectID, ";" + sender.getModelComponentID());
                VH.SetPropFormula(import.GetShape(), Constants.Properties.Transition.MessageSender, "=INDEX(1, Prop.senderOfMessage.Format)");
            }

            // message
            IMessageSpecification messageSpec = getTransitionCondition().getReceptionOfMessage();
            if (messageSpec != null && messageSpec.getModelComponentLabels().Count > 0)
            {
                VH.SetUser(import.GetShape(), Constants.Properties.Transition.PossibleMessageList, ";" + messageSpec.getModelComponentLabelsAsStrings()[0]);
                VH.SetUser(import.GetShape(), Constants.Properties.Transition.PossibleMessageListID, ";" + messageSpec.getModelComponentID());
                VH.SetPropFormula(import.GetShape(), Constants.Properties.Transition.Message, "=INDEX(1, Prop.Message.Format)");
            }

            // multiple receives
            VH.SetProp(import.GetShape(), Constants.Properties.Transition.MultiReceiveLowerBound, getTransitionCondition().getMultipleLowerBound().ToString());
            VH.SetProp(import.GetShape(), Constants.Properties.Transition.MultiReceiveUpperBound, getTransitionCondition().getMultipleUpperBound().ToString());

            // priority number
            VH.SetProp(import.GetShape(), Constants.Properties.Transition.AlternativePriorityNumber, getPriorityNumber().ToString());

            // receive type
            VH.SetPropFormula(import.GetShape(), Constants.Properties.Transition.ReceiveType,
                "INDEX(" + (int)getTransitionCondition().getReceiveType() + ", Prop.receiveType.Format)");

            // data mapping
            if (getDataMappingFunctions().Count > 0)
            {
                List<IDataMappingIncomingToLocal> tempList = getDataMappingFunctions().Values.ToList();
                if (tempList.Count > 0)
                    VH.SetProp(import.GetShape(), Constants.Properties.Transition.DataMappingIncomming, tempList[0].getDataMappingString());
            }
        }

        public bool PrepareDimensions() // TODO: prepare dimensions
        {
            return false;
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioReceiveTransition();
        }

        public Visio.Shape GetShape()
        {
            return import.GetShape();
        }
    }
}