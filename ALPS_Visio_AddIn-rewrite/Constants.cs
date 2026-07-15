namespace ALPS_Visio_AddIn_rewrite
{
    public static class Constants
    {
        // ShapeSheet cell name prefixes / suffixes
        public const string PropPrefix = "Prop.";
        public const string HyperlinkPrefix = "Hyperlink.";
        public const string ValueSuffix = ".Value";
        public const string SubAddressSuffix = ".SubAddress";

        // Misc
        public const string ExtensionSeparatorMasterName = "alpsExtensionSeperator";
        public const string MacroExtension = "MacroExtension";
        public const string StandardPassOntNamespace = "http://www.i2pm.net/standard-pass-ont#";
        public const string TypeUri = "http://www.w3.org/1999/02/22-rdf-syntax-ns#type";
        public const int LayoutSpacing = 60;

        // Stencil master names (SID)
        public static class SIDMasters
        {
            public const string StandardActor = "StandardActor";
            public const string InterfaceActor = "InterfaceActor";
            public const string CommunicationRestriction = "CommunicationRestriction";
            public const string StandardMessageConnector = "StandardMessageConnector";
            public const string Message = "Message";
            public const string MessageBox = "MessageBox";
            public const string StandAloneMacro = "StandAloneMakro";
            public const string ActorPlaceHolder = "ActorPlaceHolder";
            public const string ActorExtension = "ActorExtension";
            public const string MacroExtension = "MakroExtension";
            public const string GuardExtension = "GuardExtension";
            public const string AbstractCommunicationChannel = "AbstractCommunicationChannel";
            public const string SystemInterfaceSubject = "SystemInterfaceSubject";
            public const string SubjectGroup = "SubjectGroup";
            public const string ConnectorDirectionPattern = "ConnectorDirectionPattern";
        }

        // Stencil master names (SBD)
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
            public const string GeneralAbstractState = "GeneralAbstractState";
            public const string StatePlaceHolder = "StatePlaceHolder";
            public const string StateExtension = "StateExtension";
            public const string StateGroup = "GroupState";
            public const string TimeOutTransition = "TimeOutTransition";
            public const string Checklist = "Checklist";
            public const string CheckListPath = "CheckListPath";
            public const string GuardReceive = "GuardReceive";
            public const string InitialTransition = "InitialTransition";
        }

        // ShapeSheet cell names
        public static class ShapeCells
        {
            public const string PinX = "PinX";
            public const string PinY = "PinY";
            public const string Width = "Width";
            public const string Height = "Height";
            public const string PageWidth = "PageWidth";
            public const string PageHeight = "PageHeight";
            public const string NoObjHandles = "NoObjHandles";
            public const string NoCtlHandles = "NoCtlHandles";
            public const string NoAlignBox = "NoAlignBox";
            public const string ObjType = "ObjType";
            public const string MsvShapeCategories = "User.msvShapeCategories";
            public const string FillForegndTrans = "FillForegndTrans";
        }

        // Layer names used in Visio documents
        public static class Layers
        {
            public const string BackgroundSeparator = "BackgroundSeparatorLayer";
            // Typo preserved: must match the actual layer name in existing Visio documents
            public const string BackgroundSeparatorAlt = "BackgroundSeperatorLayer";
        }

        // Shape category strings (used in HasCategory() calls)
        public static class ShapeCategories
        {
            public static class General
            {
                public const string ModelComponent = "alpsModelComponent";
                public const string SIDComponent = "alpsSIDcomponent";
                public const string SIDConnector = "alpsSIDconnector";
                public const string SIDActor = "alpsSIDactor";
                public const string SIDActorWithSBD = "alpsSIDactorWithSBD";
                public const string SBDComponent = "alpsSBDcomponent";
                public const string SBDState = "alpsSBDstate";
                public const string SBDConnector = "alpsSBDconnector";
                public const string SBDInteractionState = "alpsSBDinteractionState";
                public const string SBDInteractionTransition = "alpsInteractionTransition";
                public const string AbstractElement = "alpsAbstractPassElement";
                public const string StandardPassElement = "alpsStandardPassElement";
                public const string ExtensionSeparator = "alpsExtensionSeperator";
            }

            public static class SID
            {
                public const string StandardActor = "StandardActor";
                public const string AbstractActor = "AbstractActor";
                public const string InterfaceActor = "InterfaceActor";
                public const string ActorExtension = "ActorExtension";
                public const string ActorPlaceHolder = "ActorPlaceHolder";
                public const string Message = "alpsMessage";
                public const string MessageConnector = "alpsSIDMessageConnector";
                public const string MessageConnectorBox = "messageConnectorBox";
                public const string StandardMessageConnector = "standardMessageConnector";
                public const string AbstractMessageConnector = "AbstractMessageConnector";
                public const string ExclusiveMessageConnector = "ExclusiveMessageConnector";
                public const string CommunicationRestriction = "CommunicationRestriction";
            }

            public static class SBD
            {
                public const string FunctionState = "functionState";
                public const string AbstractFunctionState = "abstractFunctionState";
                public const string SendState = "sendState";
                public const string AbstractSendState = "abstractSendState";
                public const string ReceiveState = "ReceiveState";
                public const string AbstractReceiveState = "abstractReceiveState";
                public const string GuardReceiveState = "GuardReceiveState";
                public const string GeneralAbstractState = "GeneralAbstractState";
                public const string StatePlaceHolder = "statePlaceHolder";
                public const string StateExtension = "StateExtension";
                public const string StandardTransition = "standardTransition";
                public const string TriggerTransition = "triggerTransition";
                public const string SuccessionTransition = "successionTransition";
                public const string FinalTransition = "finalTransition";
                public const string ReceiveTransition = "ReceiveTransition";
                public const string TriggerReceiveTransition = "triggerReceiveTransition";
                public const string SuccessionReceiveTransition = "successionReceiveTransition";
                public const string FinalReceiveTransition = "finalReceiveTransition";
                public const string SendTransition = "SendTransition";
                public const string TriggerSendTransition = "triggerSendTransition";
                public const string SuccessionSendTransition = "successionSendTransition";
                public const string FinalSendTransition = "finalSendTransition";
                public const string StateGroup = "stateGroup";
                public const string UserCancelTransition = "UserCancelTransition";
                public const string TimeOutTransition = "timeOutTransition";
                public const string Checklist = "Checklist";
                public const string CheckListPath = "CheckListPath";
                public const string GuardReceive = "GuardReceive";
                public const string CheckboxPathInitialTransition = "checkboxPathInitialTransition";
            }
        }

        // ShapeSheet property row names (used as "Prop." + ...)
        public static class Properties
        {
            public const string Label = "lable";
            public const string Comment = "modelComponentComment";
            public const string ID = "modelComponentID";
            public const string ModelComponentType = "modelComponentType";
            public const string UIAddress = "uiAddress";

            public static class Subject
            {
                public const string Multi = "multiSubject";
                public const string Implements = "implements";
                public const string Start = "startSubject";
                public const string Abstract = "abstract";
                public const string LinkedResource = "linked_Resource";
                public const string MessageListSend = "messageListSend";
                public const string MessageListReceive = "messageListReceive";
                public const string ActorsphereOuterSphere = "dispalyOuterSphere";

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
                public const string Implements = "implements";
                public const string HasRefinement = "hasRefinement";
                public const string InCycle = "inCycle";
                public const string MultiplicityLowerBound = "multiplicityLowerBound";
                public const string MultiplicityUpperBound = "multiplicityUpperBound";
                public const string OptionalChecklistPath = "optionalPath";
            }

            public static class Transition
            {
                public const string ReceiverSenderListForSubject = "receiverSenderListForSubject";
                public const string ReceiverSenderListForSubjectID = "receiverSenderListForSubjectID";
                public const string MessageSender = "senderOfMessage";
                public const string PossibleMessageList = "possibleMessageList";
                public const string PossibleMessageListID = "possibleMessageListID";
                public const string Message = "message";
                public const string MessageList = "messageList";
                public const string OriginSubject = "originSubject";
                public const string TargetSubject = "targetSubject";
                public const string OriginState = "originState";
                public const string TargetState = "targetState";
                public const string ReceivingSubject = "receivingSubject";
                public const string MultiReceiveLowerBound = "multiReceiveLowerBound";
                public const string MultiReceiveUpperBound = "multiReceiveUpperBound";
                public const string MultiSendLowerBound = "multiSendLowerBound";
                public const string MultiSendUpperBound = "multiSendUpperBound";
                public const string AlternativePriorityNumber = "alternativePriorityNumber";
                public const string ReceiveType = "receiveType";
                public const string SendType = "sendingType";
                public const string DataMappingIncomming = "dataMappingIncomming";
                public const string DataMappingOutgoing = "dataMappingOutgoing";
                public const string ConnectorErrorDisplayMode = "connectorErrorDisplayMode";
                public const string BiDirectionalChannel = "biDirectionalChannel";

                public const string TimeOutType = "timeOutType";
                public const string TimeOutTime = "timeOutTime";
                public const string TimeOutDate = "timeOutDate";
                public const string TimeDisplayString = "timeDisplayString";

                public const string BoxCanBeMovedFreely = "boxCanBeMovedFreely";
                public const string Implements = "implements";
                public const string Extends = "extends";
            }

            // String values for Prop.sendingType / Prop.receiveType cells
            public static class SendReceiveValues
            {
                public const string ReceiveStandard = "standard";
                public const string ReceiveMultiple = "multiple";
                public const string ReceiveAll = "From all known";
                public const string SendStandard = "standard";
                public const string SendToNew = "send to new";
                public const string SendToKnown = "send to known";
                public const string SendToAll = "send to all (known)";

                public const string UriReceiveMultiple = "http://www.imi.kit.edu/abstract-pass-ont//ReceiveMultiple";
                public const string UriReceiveAll = "http://www.imi.kit.edu/abstract-pass-ont//ReceiveFromAllKnown";
                public const string UriSendToNew = "http://www.imi.kit.edu/abstract-pass-ont//SendToNew";
                public const string UriSendToKnown = "http://www.imi.kit.edu/abstract-pass-ont//SendToKnown";
                public const string UriSendToAll = "http://www.imi.kit.edu/abstract-pass-ont//SendToAllKnown";
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
            public const string ExtendedSubject = "extendedSubject";
            public const string ExtendedState = "linkToExtendedState";
            public const string MaximumNumberOfInstantiation = "maximumNumberOfInstantiation";
            public const string ExecutionMapping = "organizationalImplementation";
            public const string InputPoolConstraints = "inputPoolConstraints";
        }

        // Simple simulation properties
        public static class SimpleSim
        {
            public const string DurationMeanValue = "simpleSimDurationMeanValue";
            public const string DurationStandardDeviation = "simpleSimDurationStandardDeviation";
            public const string DurationDistributionType = "simpleSimDurationDistributionType";
            public const string DurationMinValue = "simpleSimDurationMinValue";
            public const string DurationMaxValue = "simpleSimDurationMaxValue";
            public const string TransitionChoiceChance = "simpleSimTranstionChoiceChance";
            public const string StayChance = "simpleSimStayChance";
            public const string InterfaceSubjectResponseXML = "interfaceSubjectResponseXML";
            public const string InterfaceSubjectResponseDefinitionAction = "responseDefintionAction";
            public const string WaitingTimeFromLastRun = "simpleSimWaitingTimeFromLastRun";
        }
    }
}
