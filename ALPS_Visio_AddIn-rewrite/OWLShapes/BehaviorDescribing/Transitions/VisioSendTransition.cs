using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;
using System.Collections.Generic;
using System.Linq;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioSendTransition : SendTransition, IVisioExportableWithShape
    {
        private const string shapeType = Constants.SBDMasters.SendTransition;

        private readonly IShapeExport export;
        public VisioSendTransition(IState sourceState, IState targetState, string labelForID = null, ITransitionCondition transitionCondition = null, ITransition.TransitionType transitionType = ITransition.TransitionType.Standard, ISet<IDataMappingLocalToOutgoing> dataMappingLocalToOutgoing = null, string comment = null, string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null) : base(sourceState, targetState, labelForID, transitionCondition, transitionType, dataMappingLocalToOutgoing, comment, additionalLabel, additionalAttribute) { export = new TransitionExport(this); }
        protected VisioSendTransition() { export = new TransitionExport(this); }

        public void ExportToVisio(Visio.Page page)
        {
            export.Export(shapeType, page, VH.GetBounds(this));

            // TODO

            // set reciever
            ISubject receiver = getTransitionCondition().getRequiresMessageSentTo();
            if (receiver != null && receiver.getModelComponentLabels().Count > 0)
            {
                VH.SetUser(export.GetShape(), Constants.Properties.Transition.ReceiverSenderListForSubject, ";" + receiver.getModelComponentLabelsAsStrings()[0]);
                VH.SetUser(export.GetShape(), Constants.Properties.Transition.ReceiverSenderListForSubjectID, ";" + receiver.getModelComponentID());
                VH.SetPropertyFormulaU(export.GetShape(), Constants.Properties.Transition.ReceivingSubject, "INDEX(1,Prop.receivingSubject.Format)");
            }

            // message
            IMessageSpecification messageSpec = getTransitionCondition().getRequiresSendingOfMessage();
            if (messageSpec != null && messageSpec.getModelComponentLabels().Count > 0)
            {
                VH.SetUser(export.GetShape(), Constants.Properties.Transition.PossibleMessageList, ";" + messageSpec.getModelComponentLabelsAsStrings()[0]);
                
                VH.SetUser(export.GetShape(), Constants.Properties.Transition.PossibleMessageListID, ";" + messageSpec.getModelComponentID());
                VH.SetPropertyFormulaU(export.GetShape(), Constants.Properties.Transition.Message, "INDEX(1, Prop.Message.Format)");
            }

            // multiple sends
            VH.SetProperty(export.GetShape(), Constants.Properties.Transition.MultiSendLowerBound, "" + getTransitionCondition().getMultipleLowerBound());
            VH.SetProperty(export.GetShape(), Constants.Properties.Transition.MultiSendUpperBound, "" + getTransitionCondition().getMultipleUpperBound());

            // send type
            VH.SetPropertyU(export.GetShape(), Constants.Properties.Transition.SendType, "INDEX(" + (int)getTransitionCondition().getSendType() + ", Prop.sendingType.Format)");

            // add data mapping
            List<IDataMappingLocalToOutgoing> tempList = getDataMappingFunctions().Values.ToList();
            if (tempList.Count > 0)
            {
                string dataMappingString = tempList[0].getDataMappingString();

                // QuoteLiteral (via SetPropertyULiteral) escaped Anfuehrungszeichen korrekt;
                // der alte CHAR(13)-Workaround (prepareXMLLiteralForEntryIntoVisioShapeData)
                // ist dadurch ueberfluessig und wuerde doppelt escapen.
                if (getDataMappingFunctions().Count > 0) VH.SetPropertyULiteral(export.GetShape(), Constants.Properties.Transition.DataMappingOutgoing, dataMappingString);
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
            return export.GetShape();
        }
    }
}