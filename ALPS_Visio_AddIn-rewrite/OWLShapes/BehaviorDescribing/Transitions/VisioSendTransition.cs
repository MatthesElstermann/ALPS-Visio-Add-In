using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;
using System.Collections.Generic;
using System.Linq;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioSendTransition : SendTransition, IVisioImportableWithShape
    {
        private const string shapeType = Constants.SBDMasters.SendTransition;

        private readonly IShapeImport import;
        public VisioSendTransition(IState sourceState, IState targetState, string labelForID = null, ITransitionCondition transitionCondition = null, ITransition.TransitionType transitionType = ITransition.TransitionType.Standard, ISet<IDataMappingLocalToOutgoing> dataMappingLocalToOutgoing = null, string comment = null, string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null) : base(sourceState, targetState, labelForID, transitionCondition, transitionType, dataMappingLocalToOutgoing, comment, additionalLabel, additionalAttribute) { import = new TransitionImport(this); }
        protected VisioSendTransition() { import = new TransitionImport(this); }

        public void ImportToVisio(Visio.Page page)
        {
            import.Import(shapeType, page, VH.GetBounds(this));

            // TODO

            // set reciever
            ISubject receiver = getTransitionCondition().getRequiresMessageSentTo();
            if (receiver != null && receiver.getModelComponentLabels().Count > 0)
            {
                VH.SetUser(import.GetShape(), Constants.Properties.Transition.ReceiverSenderListForSubject, ";" + receiver.getModelComponentLabelsAsStrings()[0]);
                VH.SetUser(import.GetShape(), Constants.Properties.Transition.ReceiverSenderListForSubjectID, ";" + receiver.getModelComponentID());
                VH.SetPropFormula(import.GetShape(), Constants.Properties.Transition.ReceivingSubject, "INDEX(1,Prop.receivingSubject.Format)");
            }

            // message
            IMessageSpecification messageSpec = getTransitionCondition().getRequiresSendingOfMessage();
            if (messageSpec != null && messageSpec.getModelComponentLabels().Count > 0)
            {
                VH.SetUser(import.GetShape(), Constants.Properties.Transition.PossibleMessageList, ";" + messageSpec.getModelComponentLabelsAsStrings()[0]);
                VH.SetUser(import.GetShape(), Constants.Properties.Transition.PossibleMessageListID, ";" + messageSpec.getModelComponentID());
                VH.SetPropFormula(import.GetShape(), Constants.Properties.Transition.Message, "INDEX(1, Prop.Message.Format)");
            }

            // multiple sends
            VH.SetProp(import.GetShape(), Constants.Properties.Transition.MultiSendLowerBound, getTransitionCondition().getMultipleLowerBound().ToString());
            VH.SetProp(import.GetShape(), Constants.Properties.Transition.MultiSendUpperBound, getTransitionCondition().getMultipleUpperBound().ToString());

            // send type
            VH.SetPropFormula(import.GetShape(), Constants.Properties.Transition.SendType, "INDEX(" + (int)getTransitionCondition().getSendType() + ", Prop.sendingType.Format)");

            // add data mapping
            List<IDataMappingLocalToOutgoing> tempList = getDataMappingFunctions().Values.ToList();
            if (tempList.Count > 0)
            {
                string dataMappingString = tempList[0].getDataMappingString();

                if (getDataMappingFunctions().Count > 0) VH.SetProp(import.GetShape(), Constants.Properties.Transition.DataMappingOutgoing, dataMappingString);
            }
        }

        public bool PrepareDimensions() // TODO: prepare dimensions
        {
            return false;
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioSendTransition();
        }

        public Visio.Shape GetShape()
        {
            return import.GetShape();
        }
    }
}