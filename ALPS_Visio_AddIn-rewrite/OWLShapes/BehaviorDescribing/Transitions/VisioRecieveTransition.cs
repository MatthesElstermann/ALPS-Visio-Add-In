using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Visio = Microsoft.Office.Interop.Visio;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;
using System.Collections.Generic;
using System.Linq;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioReceiveTransition : ReceiveTransition, IVisioExportableWithShape
    {
        private const string shapeType = Constants.SBDMasters.ReceiveTransition;
        
        private readonly IShapeExport export;
        public VisioReceiveTransition(IState sourceState, IState targetState, string labelForID = null, ITransitionCondition transitionCondition = null, ITransition.TransitionType transitionType = ITransition.TransitionType.Standard, ISet<IDataMappingIncomingToLocal> dataMappingIncomingToLocal = null, int priorityNumber = 0, string comment = null, string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null) : base(sourceState, targetState, labelForID, transitionCondition, transitionType, dataMappingIncomingToLocal, priorityNumber, comment, additionalLabel, additionalAttribute) { export = new TransitionExport(this); }
        protected VisioReceiveTransition() { export = new TransitionExport(this); }

        public void ExportToVisio(Visio.Page page)
        {
            export.Export(shapeType, page, VH.GetBounds(this));

            // TODO: VH and stuff

            // sender
            ISubject sender = getTransitionCondition().getMessageSentFrom();
            if (sender != null && sender.getModelComponentLabels().Count > 0)
            {
                VH.SetUser(export.GetShape(), Constants.Properties.Transition.ReceiverSenderListForSubject, ";" + sender.getModelComponentLabelsAsStrings()[0]);
                VH.SetUser(export.GetShape(), Constants.Properties.Transition.ReceiverSenderListForSubjectID, ";" + sender.getModelComponentID());
                export.GetShape().CellsU["Prop." + Constants.Properties.Transition.MessageSender].FormulaU = "=INDEX(1, Prop.senderOfMessage.Format)";
            }

            // message
            IMessageSpecification messageSpec = getTransitionCondition().getReceptionOfMessage();
            if (messageSpec != null && messageSpec.getModelComponentLabels().Count > 0)
            {
                VH.SetUser(export.GetShape(), Constants.Properties.Transition.PossibleMessageList, ";" + messageSpec.getModelComponentLabelsAsStrings()[0]);
                VH.SetUser(export.GetShape(), Constants.Properties.Transition.PossibleMessageListID, ";" + messageSpec.getModelComponentID());
                export.GetShape().CellsU["Prop." + Constants.Properties.Transition.Message].FormulaU = "=INDEX(1, Prop.Message.Format)";
            }

            // multiple sends
            export.GetShape().CellsU["Prop." + Constants.Properties.Transition.MultiReceiveLowerBound].Formula = VH.QuoteLiteral(getTransitionCondition().getMultipleLowerBound());
            export.GetShape().CellsU["Prop." + Constants.Properties.Transition.MultiReceiveUpperBound].Formula = VH.QuoteLiteral(getTransitionCondition().getMultipleUpperBound());

            // priority number
            export.GetShape().CellsU["Prop." + Constants.Properties.Transition.AlternativePriorityNumber].Formula = VH.QuoteLiteral(getPriorityNumber());

            // recieve type
            export.GetShape().CellsU["Prop." + Constants.Properties.Transition.ReceiveType].FormulaU = "INDEX(" + (int)getTransitionCondition().getReceiveType() + ", Prop.receiveType.Format)";

            // add data mapping
            if (getDataMappingFunctions().Count > 0)
            {
                List<IDataMappingIncomingToLocal> tempList = getDataMappingFunctions().Values.ToList();
                if (tempList.Count > 0)
                {
                    string dataMappingString = tempList[0].getDataMappingString();

                    // QuoteLiteral escaped Anfuehrungszeichen korrekt; der alte
                    // CHAR(13)-Workaround (prepareXMLLiteralForEntryIntoVisioShapeData) entfaellt.
                    export.GetShape().CellsU["Prop." + Constants.Properties.Transition.DataMappingIncomming].Formula = VH.QuoteLiteral(dataMappingString);
                }
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
            return export.GetShape();
        }
    }
}