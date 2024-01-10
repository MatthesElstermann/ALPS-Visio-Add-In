using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static alps.net.api.StandardPASS.ITransition;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisioAddIn.OwlShapes
{
    public class VisioSendTransition : SendTransition, IVisioExportableWithShape
    {
        private const string type = ALPSConstants.alpsSBDMasterSendTransition;
        private readonly IExportFunctionality export;

        public VisioSendTransition() { export = new TransitionExport(this); }

        public VisioSendTransition(IState sourceState, IState targetState, string labelForID = null, ITransitionCondition transitionCondition = null,
            ITransition.TransitionType transitionType = ITransition.TransitionType.Standard,
            ISet<IDataMappingLocalToOutgoing> dataMappingLocalToOutgoing = null, string comment = null,
            string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null)
            : base(sourceState, targetState, labelForID, transitionCondition, transitionType, dataMappingLocalToOutgoing, comment, additionalLabel, additionalAttribute)
        { export = new TransitionExport(this); }

        public void exportToVisio(Visio.Page currentPage, ISimple2DVisualizationBounds bounds = null)
        {
            export.export(VisioHelper.ShapeType.SBD, currentPage, type,
                                new List<ISimple2DVisualizationPoint>(getElementsWithUnspecifiedRelation().Values.OfType<ISimple2DVisualizationPoint>()), this );

            //Model componten Type adjustment
            byte indexNumber = 0;
            switch(getTransitionType()){
                case TransitionType.Standard: indexNumber = 0;break;
                case TransitionType.Trigger: indexNumber = 1; break;
                case TransitionType.Precedence: indexNumber = 2; break;
                case TransitionType.Finalized: indexNumber = 3; break;
                case TransitionType.Advice: indexNumber = 4; break;
            }
            getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentType].FormulaU = "=INDEX(" + indexNumber + ",Prop.modelComponentType.Format)";

            

            // Fill in the name of the receiver
            ISubject receiver = getTransitionCondition().getRequiresMessageSentTo();
            if (receiver != null && receiver.getModelComponentLabels().Count > 0)
            {
                getShape().CellsU["User." + ALPSConstants.alpsPropertieTypeReceiverSenderListForSubject].Formula = "\";" + receiver.getModelComponentLabelsAsStrings()[0] + "\"";
                getShape().CellsU["User." + ALPSConstants.alpsPropertieTypeReceiverSenderListForSubjectID].Formula = "\";" + receiver.getModelComponentID() + "\"";
                getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeReceivingSubject].FormulaU =  "=INDEX(1,Prop.receivingSubject.Format)";

            }
            // Fill in the reference to the sent message
            IMessageSpecification messageSpec = getTransitionCondition().getRequiresSendingOfMessage();
            if (messageSpec != null && messageSpec.getModelComponentLabels().Count > 0)
            {
                getShape().CellsU["User." + ALPSConstants.alpsPropertieTypePossibleMessageList].Formula = "\";" + messageSpec.getModelComponentLabelsAsStrings()[0] + "\"";
                getShape().CellsU["User." + ALPSConstants.alpsPropertieTypePossibleMessageListID].Formula = "\";" + messageSpec.getModelComponentID() + "\"";
                getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeConnectorMessage].FormulaU = "=INDEX(1, Prop.Message.Format)";
            }

            // Set the lower bound for multiple sends
            getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeMultiSendLowerBound].Formula =
                    "\"" + getTransitionCondition().getMultipleLowerBound() + "\"";

            // Set the upper bound for multiple sends
            getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeMultiSendUpperBound].Formula =
                    "\"" + getTransitionCondition().getMultipleUpperBound() + "\"";

            // Set the send type
            getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeSendType].FormulaU =
                "INDEX(" + (int)getTransitionCondition().getSendType() + ", Prop." + ALPSConstants.alpsPropertieTypeSendType + ".Format)";

            // Add the data mapping
            // We assume that there is only on data mapping function
            List<IDataMappingLocalToOutgoing> tempList = getDataMappingFunctions().Values.ToList();
            if (tempList.Count > 0)
            {
                string dataMappingString = tempList[0].getDataMappingString();
                dataMappingString = ALPSGlobalFunctions.prepareXMLLiteralForEntryIntoVisioShapeData(dataMappingString);

                if (getDataMappingFunctions().Count > 0)
                {
                    getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeDataMappingOutgoing].FormulaU =
                        "\"" + dataMappingString + "\"";
                }
            }
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioSendTransition();
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
