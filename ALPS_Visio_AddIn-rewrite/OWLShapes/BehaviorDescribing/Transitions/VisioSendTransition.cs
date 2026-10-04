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

            // add data mapping
            List<IDataMappingLocalToOutgoing> tempList = getDataMappingFunctions().Values.ToList();
            if (tempList.Count > 0)
                VH.SetProp(import.GetShape(), Constants.Properties.Transition.DataMappingOutgoing, tempList[0].getDataMappingString());

            // Die Transition-Condition ist in OWL optional — ohne sie bleibt es bei der Grundform.
            ISendTransitionCondition condition = getTransitionCondition();
            if (condition == null) return;

            // set reciever
            ISubject receiver = condition.getRequiresMessageSentTo();
            if (receiver != null && receiver.getModelComponentLabels().Count > 0)
            {
                VH.SetUser(import.GetShape(), Constants.Properties.Transition.ReceiverSenderListForSubject, ";" + receiver.getModelComponentLabelsAsStrings()[0]);
                VH.SetUser(import.GetShape(), Constants.Properties.Transition.ReceiverSenderListForSubjectID, ";" + receiver.getModelComponentID());
                VH.SetPropFormula(import.GetShape(), Constants.Properties.Transition.ReceivingSubject, "INDEX(1,Prop.receivingSubject.Format)");
            }

            // message
            IMessageSpecification messageSpec = condition.getRequiresSendingOfMessage();
            if (messageSpec != null && messageSpec.getModelComponentLabels().Count > 0)
            {
                VH.SetUser(import.GetShape(), Constants.Properties.Transition.PossibleMessageList, ";" + messageSpec.getModelComponentLabelsAsStrings()[0]);
                VH.SetUser(import.GetShape(), Constants.Properties.Transition.PossibleMessageListID, ";" + messageSpec.getModelComponentID());
                VH.SetPropFormula(import.GetShape(), Constants.Properties.Transition.Message, "INDEX(1, Prop.Message.Format)");
            }

            // multiple sends
            VH.SetProp(import.GetShape(), Constants.Properties.Transition.MultiSendLowerBound, condition.getMultipleLowerBound().ToString());
            VH.SetProp(import.GetShape(), Constants.Properties.Transition.MultiSendUpperBound, condition.getMultipleUpperBound().ToString());

            // send type
            VH.SetPropFormula(import.GetShape(), Constants.Properties.Transition.SendType, "INDEX(" + (int)condition.getSendType() + ", Prop.sendingType.Format)");
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