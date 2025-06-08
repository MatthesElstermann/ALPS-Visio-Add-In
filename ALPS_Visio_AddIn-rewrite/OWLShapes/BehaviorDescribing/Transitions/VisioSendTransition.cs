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
    public class VisioSendTransition : SendTransition, IVisioExportableWithShape
    {
        private const string type = ALPSConstants.alpsSBDMasterSendTransition;
        private readonly IShapeExport export;

        public VisioSendTransition(IState sourceState, IState targetState, string labelForID = null, ITransitionCondition transitionCondition = null, ITransition.TransitionType transitionType = ITransition.TransitionType.Standard, ISet<IDataMappingLocalToOutgoing> dataMappingLocalToOutgoing = null, string comment = null, string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null) : base(sourceState, targetState, labelForID, transitionCondition, transitionType, dataMappingLocalToOutgoing, comment, additionalLabel, additionalAttribute)
        {
            export = new TransitionExport(this);
        }

        protected VisioSendTransition()
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

            // reciever
            ISubject receiver = getTransitionCondition().getRequiresMessageSentTo();
            if (receiver != null && receiver.getModelComponentLabels().Count > 0)
            {
                getShape().CellsU["User." + ALPSConstants.alpsPropertieTypeReceiverSenderListForSubject].Formula = "\";" + receiver.getModelComponentLabelsAsStrings()[0] + "\"";
                getShape().CellsU["User." + ALPSConstants.alpsPropertieTypeReceiverSenderListForSubjectID].Formula = "\";" + receiver.getModelComponentID() + "\"";
                getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeReceivingSubject].FormulaU = "=INDEX(1,Prop.receivingSubject.Format)";
            }

            // message
            IMessageSpecification messageSpec = getTransitionCondition().getRequiresSendingOfMessage();
            if (messageSpec != null && messageSpec.getModelComponentLabels().Count > 0)
            {
                getShape().CellsU["User." + ALPSConstants.alpsPropertieTypePossibleMessageList].Formula = "\";" + messageSpec.getModelComponentLabelsAsStrings()[0] + "\"";
                getShape().CellsU["User." + ALPSConstants.alpsPropertieTypePossibleMessageListID].Formula = "\";" + messageSpec.getModelComponentID() + "\"";
                getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeConnectorMessage].FormulaU = "=INDEX(1, Prop.Message.Format)";
            }

            // multiple sends
            getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeMultiSendLowerBound].Formula = "\"" + getTransitionCondition().getMultipleLowerBound() + "\"";
            getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeMultiSendUpperBound].Formula = "\"" + getTransitionCondition().getMultipleUpperBound() + "\"";

            // send type
            getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeSendType].FormulaU = "INDEX(" + (int)getTransitionCondition().getSendType() + ", Prop." + ALPSConstants.alpsPropertieTypeSendType + ".Format)";

            // add data mapping
            List<IDataMappingLocalToOutgoing> tempList = getDataMappingFunctions().Values.ToList();
            if (tempList.Count > 0)
            {
                string dataMappingString = tempList[0].getDataMappingString();
                dataMappingString = ALPSGlobalFunctions.prepareXMLLiteralForEntryIntoVisioShapeData(dataMappingString);

                if (getDataMappingFunctions().Count > 0)
                {
                    getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeDataMappingOutgoing].FormulaU = "\"" + dataMappingString + "\"";
                }
            }
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioSendTransition();
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