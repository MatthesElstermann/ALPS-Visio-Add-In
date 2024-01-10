
using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Microsoft.Office.Core;
using System.Collections.Generic;
using System.Linq;
using static alps.net.api.StandardPASS.ITransition;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisioAddIn.OwlShapes
{
    public class VisioReceiveTransition : ReceiveTransition, IVisioExportableWithShape
    {
        private const string type = ALPSConstants.alpsSBDMasterReceiveTransition;
        private readonly IExportFunctionality export;

        public VisioReceiveTransition() { export = new TransitionExport(this); }

        public VisioReceiveTransition(IState sourceState, IState targetState, string labelForID = null,
            ITransitionCondition transitionCondition = null, ITransition.TransitionType transitionType = ITransition.TransitionType.Standard,
            ISet<IDataMappingIncomingToLocal> dataMappingIncomingToLocal = null, int priorityNumber = 0, string comment = null,
            string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null)
            : base(sourceState, targetState, labelForID, transitionCondition, transitionType, dataMappingIncomingToLocal,
                  priorityNumber, comment, additionalLabel, additionalAttribute)
        { export = new TransitionExport(this); }

        public void exportToVisio(Visio.Page currentPage, ISimple2DVisualizationBounds bounds = null)
        {
            export.export(VisioHelper.ShapeType.SBD, currentPage, type,
                                new List<ISimple2DVisualizationPoint>(getElementsWithUnspecifiedRelation().Values.OfType<ISimple2DVisualizationPoint>()), this);
            //Model componten Type adjustment
            byte indexNumber = 0;
            switch (getTransitionType())
            {
                case TransitionType.Standard: indexNumber = 0; break;
                case TransitionType.Trigger: indexNumber = 1; break;
                case TransitionType.Precedence: indexNumber = 2; break;
                case TransitionType.Finalized: indexNumber = 3; break;
                case TransitionType.Advice: indexNumber = 4; break;
            }
            getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentType].FormulaU = "=INDEX(" + indexNumber + ",Prop.modelComponentType.Format)";


            // Fill in the name of the sender
            ISubject sender = getTransitionCondition().getMessageSentFrom();
            if (sender != null && sender.getModelComponentLabels().Count > 0)
            {
                //set possbile senderList
                getShape().CellsU["User." + ALPSConstants.alpsPropertieTypeReceiverSenderListForSubject].Formula = "\";" + sender.getModelComponentLabelsAsStrings()[0] +"\"";
                getShape().CellsU["User." + ALPSConstants.alpsPropertieTypeReceiverSenderListForSubjectID].Formula = "\";" + sender.getModelComponentID() +"\"";
                getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeSenderOfMessage].FormulaU =   "=INDEX(1, Prop.senderOfMessage.Format)";
            }

            // Fill in the reference to the sent message
            IMessageSpecification messageSpec = getTransitionCondition().getReceptionOfMessage();
            if (messageSpec != null && messageSpec.getModelComponentLabels().Count > 0) 
            {
                getShape().CellsU["User." + ALPSConstants.alpsPropertieTypePossibleMessageList].Formula = "\";" + messageSpec.getModelComponentLabelsAsStrings()[0] + "\"";
                getShape().CellsU["User." + ALPSConstants.alpsPropertieTypePossibleMessageListID].Formula = "\";" + messageSpec.getModelComponentID() + "\"";
                getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeConnectorMessage].FormulaU = "=INDEX(1, Prop.Message.Format)";
      
            }
            // Set the lower bound for multiple sends
            getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeMultiReceiveLowerBound].Formula =
                    "\"" + getTransitionCondition().getMultipleLowerBound() + "\"";

            // Set the upper bound for multiple sends
            getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeMultiReceiveUpperBound].Formula =
                    "\"" + getTransitionCondition().getMultipleUpperBound() + "\"";

            // Set the priority number
            getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeConnectorAlternativePriority].Formula =
                    "\"" + getPriorityNumber() + "\"";

            // Set the receive type
            getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeReceiveType].FormulaU =
                "INDEX(" + (int)getTransitionCondition().getReceiveType() + ", Prop.receiveType.Format)";

            // Add the data mapping
            // We assume that there is only on data mapping function

            if (getDataMappingFunctions().Count > 0)
            {
                List<IDataMappingIncomingToLocal> tempList = getDataMappingFunctions().Values.ToList();
                if (tempList.Count > 0)
                {
                    string dataMappingString = tempList[0].getDataMappingString();
                    dataMappingString = ALPSGlobalFunctions.prepareXMLLiteralForEntryIntoVisioShapeData(dataMappingString);

                    getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeDataMappingIncoming].Formula =
                        "\"" + dataMappingString + "\"";

                }
            }
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioReceiveTransition();
        }

        public Visio.Shape getShape()
        {
            return export.getShape();
        }

        public void setShape(Visio.Shape shape)
        {
            export.setShape(shape);
        }
    }
}
