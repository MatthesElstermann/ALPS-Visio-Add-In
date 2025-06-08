using alps.net.api.ALPS;
using System.Collections.Generic;
using System.Linq;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using Microsoft.Office.Interop.Visio;
using alps.net.api.util;
using static alps.net.api.StandardPASS.ITransition;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioReceiveTransition : ReceiveTransition, IVisioExportableWithShape
    {
        private const string type = ALPSConstants.alpsSBDMasterReceiveTransition;
        private readonly IShapeExport export;

        public VisioReceiveTransition(IState sourceState, IState targetState, string labelForID = null, ITransitionCondition transitionCondition = null, ITransition.TransitionType transitionType = ITransition.TransitionType.Standard, ISet<IDataMappingIncomingToLocal> dataMappingIncomingToLocal = null, int priorityNumber = 0, string comment = null, string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null) : base(sourceState, targetState, labelForID, transitionCondition, transitionType, dataMappingIncomingToLocal, priorityNumber, comment, additionalLabel, additionalAttribute)
        {
            export = new TransitionExport(this);
        }

        protected VisioReceiveTransition()
        {
            export = new TransitionExport(this);
        }

        public void exportToVisio(Page currentPage)
        {
            export.export(VisioHelper.ShapeType.SBD, currentPage, type, new List<ISimple2DVisualizationPoint>(getElementsWithUnspecifiedRelation().Values.OfType<ISimple2DVisualizationPoint>()), this);

            // model component type
            int indexNumber = 0;
            switch (getTransitionType())
            {
                case TransitionType.Standard: indexNumber = 0; break;
                case TransitionType.Trigger: indexNumber = 1; break;
                case TransitionType.Precedence: indexNumber = 2; break;
                case TransitionType.Finalized: indexNumber = 3; break;
                case TransitionType.Advice: indexNumber = 4; break;
            }
            getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentType].FormulaU = "=INDEX(" + indexNumber + ",Prop.modelComponentType.Format)";

            // sender
            ISubject sender = getTransitionCondition().getMessageSentFrom();
            if (sender != null && sender.getModelComponentLabels().Count > 0)
            {
                getShape().CellsU["User." + ALPSConstants.alpsPropertieTypeReceiverSenderListForSubject].Formula = "\";" + sender.getModelComponentLabelsAsStrings()[0] + "\"";
                getShape().CellsU["User." + ALPSConstants.alpsPropertieTypeReceiverSenderListForSubjectID].Formula = "\";" + sender.getModelComponentID() + "\"";
                getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeSenderOfMessage].FormulaU = "=INDEX(1, Prop.senderOfMessage.Format)";
            }

            // message
            IMessageSpecification messageSpec = getTransitionCondition().getReceptionOfMessage();
            if (messageSpec != null && messageSpec.getModelComponentLabels().Count > 0)
            {
                getShape().CellsU["User." + ALPSConstants.alpsPropertieTypePossibleMessageList].Formula = "\";" + messageSpec.getModelComponentLabelsAsStrings()[0] + "\"";
                getShape().CellsU["User." + ALPSConstants.alpsPropertieTypePossibleMessageListID].Formula = "\";" + messageSpec.getModelComponentID() + "\"";
                getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeConnectorMessage].FormulaU = "=INDEX(1, Prop.Message.Format)";
            }

            // multiple sends
            getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeMultiReceiveLowerBound].Formula = "\"" + getTransitionCondition().getMultipleLowerBound() + "\"";
            getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeMultiReceiveUpperBound].Formula = "\"" + getTransitionCondition().getMultipleUpperBound() + "\"";

            // priority number
            getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeConnectorAlternativePriority].Formula = "\"" + getPriorityNumber() + "\"";

            // recieve type
            getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeReceiveType].FormulaU = "INDEX(" + (int)getTransitionCondition().getReceiveType() + ", Prop.receiveType.Format)";

            // add data mapping
            if (getDataMappingFunctions().Count > 0)
            {
                List<IDataMappingIncomingToLocal> tempList = getDataMappingFunctions().Values.ToList();
                if (tempList.Count > 0)
                {
                    string dataMappingString = tempList[0].getDataMappingString();
                    dataMappingString = ALPSGlobalFunctions.prepareXMLLiteralForEntryIntoVisioShapeData(dataMappingString);

                    getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeDataMappingIncoming].Formula = "\"" + dataMappingString + "\"";
                }
            }
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioReceiveTransition();
        }

        public Shape getShape()
        {
            return export.getShape();
        }

        public void setShape(Shape shape)
        {
            export.setShape(shape);
        }

        public bool prep2DInfo()
        {
            return false;
        }
    }
}