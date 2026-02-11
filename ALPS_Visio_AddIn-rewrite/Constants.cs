using System.Configuration;

namespace ALPS_Visio_AddIn_rewrite
{
    public static class Constants
    {
        public static class SIDMasters
        {
            public const string StandardActor = "StandardActor";

            public const string InterfaceActor = "InterfaceActor";
            public const string CommunicationRestriction = "CommunicationRestriction";
            public const string StandardMessageConnector = "StandardMessageConnector";
            public const string Message = "Message";
            public const string StandAloneMacro = "StandAloneMakro";
        }

        public static class SBDMasters
        {
            public const string DoState = "FunctionState";
            public const string ReceiveState = "ReceiveState";
            public const string SendState = "SendState";
            public const string GenericReturnToOriginReference = "GenericReturnToOriginReference";
            public const string StandardTransition = "StandardTransition";
            public const string ReceiveTransition = "ReceiveTransition";
            public const string SendingFailedTransition = "SendingFailedTransition";
            public const string SendTransition = "SendTransition";
            public const string FlowRestrictor = "FlowRestrictor";
            public const string TimeTransition = "TimeTransition";
            public const string UserCancelTransition = "UserCancelTransition";
        }

        public static class Properties
        {
            public const string Label = "lable";
            public const string Comment = "modelComponentComment";
            public const string ID = "modelComponentID";

            public static class Subject
            {
                public const string Multi = "multiSubject";
                public const string Implements = "implements";
                public const string Start = "startSubject";
                public const string Abstract = "abstract";
                public const string LinkedResource = "linked_Resource";

                // NOT IMPL
                public const string Final = "finalizedCommunication";
                public const string OrganizationalImplementation = "organizationalImplementation";
                public const string Extends = "extends";
                public const string SymbolType = "symbolType";
                public const string InputPoolConstraints = "inputPoolConstraints";
            }

            public static class State
            {
                public const string End = "isEndState";
                public const string Start = "isStartState";
                public const string Abstract = "isAbstract";
                public const string Finalized = "isFinalized";
            }

            public static class Transition
            {
                public const string ReceiverSenderListForSubject = "receiverSenderListForSubject";
                public const string ReceiverSenderListForSubjectID = "receiverSenderListForSubjectID";
                public const string MessageSender = "senderOfMessage";
                public const string PossibleMessageList = "possibleMessageList";
                public const string PossibleMessageListID = "possibleMessageListID";
                public const string Message = "message";
                public const string MultiReceiveLowerBound = "multiReceiveLowerBound";
                public const string MultiReceiveUpperBound = "multiReceiveUpperBound";
                public const string AlternativePriorityNumber = "alternativePriorityNumber";
                public const string ReceiveType = "receiveType";
                public const string DataMappingIncomming = "dataMappingIncomming";
                public const string ReceivingSubject = "receivingSubject";
                public const string MultiSendLowerBound = "multiSendLowerBound";
                public const string MultiSendUpperBound = "multiSendUpperBound";
                public const string SendType = "sendingType";
                public const string DataMappingOutgoing = "dataMappingOutgoing";

                public const string TimeOutType = "timeOutType"; //in timeout transitions to determin what typ it is.
                public const string TimeOutTime = "timeOutTime"; //in time out transitions a Prop. if a time duration is choosen
                public const string TimeOutDate = "timeOutDate"; // in time out transitions active if a calendar based time
                public const string TimeDisplayString = "timeDisplayString"; // in time out transitions a string fro the lables

                public const string BoxCanBeMovedFreely = "boxCanBeMovedFreely";
                public const string Implements = "implements";
                public const string Extends = "extends";
            }

            // TODO: organize
            public const string DocumentType = "abstractLayeredPASSProcessModel";
            public const string InteropWithVSTOShouldListenersRun = "interopWithVSTOShouldListenersRun";
            public const string PageType = "pageType";
            public const string PageLayer = "pageLayer";
            public const string PageModelURI = "modelURI";
            public const string PageModelVersion = "modelVersion";
            public const string SBDLinkedSubjectID = "subjectShapeID";
            public const string PriorityOrderNumber = "priorityOrder";
            public const string SBDPage = "SubjectBehavior";
            public const string SIDPage = "SubjectInteraction";
            public const string LinkedSBD = "linkedSBD";
            public const string LinkedSIDPage = "linkedSIDPage";
            public const string ExtendedSubject = "extendedSubject"; //subject extensions should have their extension marked here
            public const string ExtendedState = "linkToExtendedState";
            public const string MaximumNumberOfInstantiation = "maximumNumberOfInstantiation";
            public const string ExecutionMapping = "organizationalImplementation";
            public const string InputPoolConstraints = "inputPoolConstraints";
        }
    }
}
