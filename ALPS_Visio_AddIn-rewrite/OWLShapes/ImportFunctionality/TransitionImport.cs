using System.Collections.Generic;
using alps.net.api.ALPS;
using alps.net.api.StandardPASS;
using Visio = Microsoft.Office.Interop.Visio;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class TransitionImport : PASSProcessModelElementImport
    {
		private readonly ITransition transition;

        /// <summary>
        /// Shape import for transition
        /// </summary>
        public TransitionImport(ITransition transition) : base(transition)
        {
            this.transition = transition;
        }

        public override void Import(string shapeType, Visio.Page page, IList<ISimple2DVisualizationPoint> bounds)
        {
            base.Import(shapeType, page, bounds);

            // TODO: hasSourceState only State @ originState
            // TODO: hasTargetState only State @ targetState
            // TODO: hasTransitionCondition exactly 1 TransitionCondition
            // -> hasToolSpecificDefinition max 1 string

            // DoTransition
            // TODO: hasPriorityNumber max 1 int(>=0)
            // -> DoTransitionCondition
            // label mit condition string �berschreiben

            // CommunicationTransition (contains Receive and Send)
            // -> MessageExchangeCondition (requiresPerformedMessageExchange exactly 1 MessageExchange)

            // ReceiveTransition
            // TODO: hasDataMappingFunction min 1 DataMappingIncomingToLocal (hasFeelExpressionAsDataMapping OR hasToolSpecificDefinition exactly 1 string, hasDataMappingString exactly 1 string)
            // TODO: hasPriorityNumber max 1 int(>=0)
            // -> ReceiveTransitionCondition (hasMultiReceiveLower/UpperBound max 1 int(>0), hasReceiveType max 1 ReceiveType(MultiReceiveFromAllKnown, MultiReceiveFromKnown, Standard), requiresMessageSentFrom max 1 Subject, requiresReceptionOfMessage max 1 MessageSpecification)

            // SendTransition
            // TODO: hasDataMappingFunction min 1 DataMappingLocalToOutgoing (hasFeelExpressionAsDataMapping OR hasToolSpecificDefinition exactly 1 string, hasDataMappingString exactly 1 string)
            // -> SendTransitionCondition (hasMultiSendLower/UpperBound max 1 int(>0), hasSendType max 1 SendType(MultiSendToAll, MultiSendToKnown, MultiSendToNew, Standard), requiresMessageSentTo max 1 Subject, requiresSendingOfMessage max 1 MessageSpecification)

            // SendingFailedTransition
            // TODO: hasSourceState only SendState @ originState
            // -> SendingFailedCondition

            // UserCancelTransition

            // TimeTransition (Reminder (CalendarBased, TimeBased) hasSourceState max 0 SendState, Timer (BusinessDay, DayTime, YearMonth))
            // -> TimeTransitionCondition (hasTimeValue exactly 1 string or (DayTimeTimer)dateTime or (YearMonthTimer)yearMonthDuration)

            // TODO: AbstractPASSTransition (AdviceTransitionType, FinalizedTransitionType, PrecedenceTransitionType, TriggerTransitionType, FlowRestrictor)
            // modelComponentType

            //////////////////////////////////////////////////////////
            // TODO: in visio:
            // Sender of Message: senderOfMessage
            // Message to be Received/Sent: message
            //((ReceiveTransition)transition).getTransitionCondition().getRequiresPerformedMessageExchange()
            // Receive Type: receiveType
            // Lower Bound if Multi Receive: multiReceiveLowerBound
            // Upper Bound if Multi Receive: multiReceiveUpperBound
            // Priority of Message Receive: alternativePriorityNumber
            // Type of Time based Transition: timeoutType
            // timeOutTime: timeOutTime
            // time Date/Frequency: timeOutDate
            // implements/represents: implements
            // Receiver of Message: receivingSubject
            // Sending Type: sendingType
            // Lower Bound if Multi Send: multiSendLowerBound
            // Upper Bound if Multi Send: multiSendUpperBound

            // set path (auto arrange)
            if (transition.getSourceState() is IVisioImportableWithShape importableSender)
                this.GetShape().CellsU["BeginX"].GlueToPos(importableSender.GetShape(), 1, 0.5);
            if (transition.getTargetState() is IVisioImportableWithShape importableReceiver)
                this.GetShape().CellsU["EndY"].GlueToPos(importableReceiver.GetShape(), 0, 0.5);

            // set box movement
            VH.SetProp(shape, Constants.Properties.Transition.BoxCanBeMovedFreely, "FALSE");

            // set implements
            if (transition.getImplementedInterfaces().Count > 0)
                VH.SetProp(shape, Constants.Properties.Transition.Implements, string.Join(";", transition.getImplementedInterfaces().Keys));

            // set the transition type dropdown (Standard/Trigger/Precedence/Finalized/Advice)
            int typeIndex;
            switch (transition.getTransitionType())
            {
                case ITransition.TransitionType.Trigger: typeIndex = 1; break;
                case ITransition.TransitionType.Precedence: typeIndex = 2; break;
                case ITransition.TransitionType.Finalized: typeIndex = 3; break;
                case ITransition.TransitionType.Advice: typeIndex = 4; break;
                default: typeIndex = 0; break; // Standard
            }
            VH.SetPropFormula(shape, Constants.Properties.ModelComponentType,
                "=INDEX(" + typeIndex + ",Prop." + Constants.Properties.ModelComponentType + ".Format)");
        }
    }
}
