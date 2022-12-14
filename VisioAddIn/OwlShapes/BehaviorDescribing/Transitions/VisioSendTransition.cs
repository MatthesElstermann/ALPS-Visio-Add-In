using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using System.Collections.Generic;
using System.Linq;
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
                                new List<ISimple2DVisualizationPoint>(getElementsWithUnspecifiedRelation().Values.OfType<ISimple2DVisualizationPoint>()));


            // Fill in the name of the receiver
            ISubject receiver = getTransitionCondition().getRequiresMessageSentTo();
            if (receiver != null && receiver.getModelComponentLabels().Count > 0)
                getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeReceivingSubject].Formula =
                    "\"" + receiver.getModelComponentLabelsAsStrings()[0] + "\"";

            // Fill in the reference to the sent message
            IMessageSpecification messageSpec = getTransitionCondition().getRequiresSendingOfMessage();
            if (messageSpec != null && messageSpec.getModelComponentLabels().Count > 0)
                getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeConnectorMessage].Formula =
                    "\"" + messageSpec.getModelComponentLabelsAsStrings()[0] + "\"";

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
            if (getDataMappingFunctions().Count > 0)
                getShape().CellsU["Prop." + ALPSConstants.alpsPropertieTypeDataMappingOutgoing].Formula =
                    "\"" + getDataMappingFunctions().Values.ToList()[0].getDataMappingString() + "\"";

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
