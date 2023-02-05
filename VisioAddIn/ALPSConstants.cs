namespace VisioAddIn
{
    public static class ALPSConstants
    {

        // Needed??
        public const string StandardPassOntNamespace = "http://www.i2pm.net/standard-pass-ont#";
        public const string typeUri = "http://www.w3.org/1999/02/22-rdf-syntax-ns#type";
        public const int layoutSpacing = 60;
        //

        public const string pageTypeSubjectBehaviorDiagram = "SubjectBehavior";
        public const string pageTypeSubjectInteractionDiagram = "SubjectInteraction";

        public const string cellValuePropertyExtends = cellPropertyCategoryPrefix
                                                       + alpsPropertieTypeExtends
                                                       + cellValueSuffix;
        public const string cellValuePropertyModelComponentId = cellPropertyCategoryPrefix
                                                                + alpsPropertieTypeModelComponentID
                                                                + cellValueSuffix;
        public const string cellSubAdressHyperlinkLinkedSBD = cellHyperlinkCategoryPrefix
                                                              + alpsHyperlinkTypeLinkedSBD
                                                              + cellHyperlinkCategorySubAdressSuffix;
        public const string cellSubAdressHyperlinkExtendedSubject = cellHyperlinkCategoryPrefix +
                                                                    alpsHyperlinksExtendedSubject +
                                                                    cellHyperlinkCategorySubAdressSuffix;
        public const string cellValuePropertyLabel = cellPropertyCategoryPrefix
                                                     + alpsPropertieTypeLabel
                                                     + cellValueSuffix;
        public const string cellValuePropertyModelComponentType = cellPropertyCategoryPrefix 
                                                          + alpsPropertieTypeModelComponentType
                                                          + cellValueSuffix;
        public const string cellValuePropertyPageModelURI = cellPropertyCategoryPrefix
                                                            + alpsPropertieTypePageModelURI
                                                            + cellValueSuffix;
        public const string cellValuePropertyPageType = cellPropertyCategoryPrefix
                                                            + alpsPropertieTypePageType
                                                            + cellValueSuffix;
        public const string cellValuePropertyPageModelVersion = cellPropertyCategoryPrefix
                                                        + alpsPropertieTypePageModelVersion
                                                        + cellValueSuffix;

        public const string cellValuePropertyPriorityOrderNumber = cellPropertyCategoryPrefix
                                                                   + alpsPropertieTypePriorityOrderNumber
                                                                   + cellValueSuffix;
        public const string cellValuePropertyPageLayer = cellPropertyCategoryPrefix
                                                         + alpsPropertieTypePageLayer
                                                         + cellValueSuffix;

        public const string cellValuePropertySBDLinkedSubjectID = cellPropertyCategoryPrefix
                                                                  + alpsPropertieTypeSBDLinkedSubjectID
                                                                  + cellValueSuffix;

        public const string shapeCellShapeTransformPinX = "PinX";
        public const string shapeCellShapeTransformPinY = "PinY";
        public const string shapeCellShapeTransformWidth = "Width";
        public const string shapeCellShapeTransformHeight = "Height";

        public const string pageCellPagePropertiesPageWidth = "PageWidth";
        public const string pageCellPagePropertiesPageHeight = "PageHeight";


        public const string shapeCellMiscellaneousNoObjectHandle = "NoObjHandles";
        public const string shapeCellMiscellaneousNoCtlHandle = "NoCtlHandles";
        public const string shapeCellMiscellaneousNoAlignBox = "NoAlignBox";
        public const string shapeCellMiscellaneousObjType = "ObjType";

        public const string shapeCellUserDefinedMsvShapeCategories = "User.msvShapeCategories";

        public const string shapeCellBackRectangleForegroundTransparency = "FillForegndTrans";

        public const string backgroundSeparatorLayerName = "BackgroundSeparatorLayer";



        //public const string LayerBackgroundName = "Hintergrund Visualisierungs layer";
        public const string LayerBackgroundName = "BackgroundSeperatorLayer";
        public const string BackRectangle = "alpsExtensionSeperator";

        public const string InputNotFound = "Eingabe \"{0}\" wurde nicht gefunden. Ort der fehlerhaften Eingabe: \"{1}\"";


        public const string MacroExtension = "MacroExtension";


        //ActNConnect Mods
        public const bool useAdditionalShapesActNConnect = false;
        //const string useAdditionalShapesActNConnectCompile = False


        //public const string actNConnectStencil = "actNConnect Data And UI v0.6.0.vssx";
        public const string actNConnectXMLNamespace = "http://www.actnconnect.com/schema/businessactor";


        //General Shape Categorys
        public const string alpsShapeCategoryModelComponent = "alpsModelComponent";
        public const string alpsShapeCategorySIDComponent = "alpsSIDcomponent";
        public const string alpsShapeCategorySIDconnector = "alpsSIDconnector";
        public const string alpsShapeCategorySIDactor = "alpsSIDactor";
        public const string alpsShapeCategorySIDactorWithSBD = "alpsSIDactorWithSBD";
        public const string alpsShapeCategorySBDComponent = "alpsSBDcomponent";
        public const string alpsShapeCategorySBDState = "alpsSBDstate";
        public const string alpsShapeCategorySBDconnector = "alpsSBDconnector";
        public const string alpsShapeCategorySBDinteractionState = "alpsSBDinteractionState";
        public const string alpsShapeCategorySBDinteractionTransition = "alpsInteractionTransition";
        public const string alpsShapeCategoryAbstractElement = "alpsAbstractPassElement";
        public const string alpsShapeCategoryStandardPassElement = "alpsStandardPassElement";
        public const string alpsShapeCategoryExtensionSeperator = "alpsExtensionSeperator";


        //ActNConnect Shape Categorys
        public const string ancShapeCategoryActNConnectShape = "actNConnectShape";
        public const string ancShapeCategoryLinkConnector = "actNConnectLinkConnector";
        public const string ancShapeCategoryAdditionalInformationHolder = "actNConnectAdditionalInformationHolder";
        public const string ancShapeCategoryUIElementContainer = "actNConnectUIElementContainer";
        public const string ancShapeCategoryUIElement = "actNConnectUIElement";
        public const string ancShapeCategoryDataAccessDefinition = "actNConnectDataAccessDefinition";
        public const string ancShapeCategoryStateRulesDefinition = "actNConnectStateRulesDefinition";
        public const string ancShapeCategoryServiceDefinition = "actNConnectServiceDefinition";
        public const string ancShapeCategoryActorDataDefinition = "actNConnectActorDataDefinition";
        public const string ancShapeCategoryRulesDefintion = "actNConnectRulesDefinition";
        public const string ancShapeCategoryServiceElementContainer = "actNConnectServiceElementContainer";
        public const string ancShapeCategoryServiceElement = "actNConnectServiceElement";
        public const string ancShapeCategoryUiContainerImagePath = ";uiContainerImagePath";

        //ActNConnect Properties
        public const string ancPropertieTypeXmlUIDefinition = "xmlUIDefinition";
        public const string ancPropertieTypeXmlDataDefintion = "xmlDataDefintion";
        public const string ancPropertieTypeXmlDataAccess = "xmlDataAccess";
        public const string ancPropertieTypeXmlRules = "xmlRules";
        public const string ancPropertieTypeXmlMetaData = "xmlMetaData";
        public const string ancPropertieTypeXmlContext = "xmlContext";
        public const string ancPropertieTypeXmlMapping = "xmlMapping";
        public const string ancPropertieTypeXmlService = "xmlService"; //for a sercvice def Shape
        public const string ancPropertieTypeOriginElement = "originElement"; //for link connectors
        public const string ancPropertieTypeTargetElement = "targetElement"; //for link connectors
        public const string ancPropertieTypeIsDefault = "isDefault"; //for UI Elements
        public const string ancPropertieTypeIsReceiveDefault = "isReceiveDefault"; //for UI Elements
        public const string ancPropertieTypeIsServiceDefault = "isServiceDefault";
        public const string ancPropertieTypeUIorServiceList = "uiOrServiceList"; //";uiList"; //for states in ANC Behavior Diagrams
        public const string ancPropertieTypePossibleUIorServiceList = "possibleUIorServiceList"; // //ehemals ";possibleUIList"for states in ANC Behavior Diagrams
        public const string ancPropertieTypeFixedActorUUID = "fixedActorUUID";
        public const string ancPropertieTypeIsServiceActor = "isServiceActor";
        public const string ancPropertieTypeActorVersion = "actorVersion";
        public const string ancPropertieTypeExistingServices = "existingServices"; //the name of a User.Data field containing the master list for currently available services
        public const string ancPropertieTypeServiceID = "serviceID"; //name of the shape Data row in a service Def Shape containing the current Service
        public const string ancPropertieTypeServiceType = "serviceType";
        public const string ancPropertieTypeServiceActorIsPersistent = "serviceActorIsPersistent";

        public const string ancPropertieValueActorProperty = "actorProperty";
        public const string ancPropertieValueTenantProperty = "tenantProperty";

        //////// Prop.Types //////////////////////////////////////////////////////////////////////////////
        //////// labels as they are used in the Shapes on the Shape Sheet

        //Every Model Component
        public const string alpsPropertieTypeModelComponentID = "modelComponentID";
        public const string alpsPropertieTypeModelComponentType = "modelComponentType";
        public const string alpsPropertieTypeComment = "modelComponentComment";

        //ImplementsExtends Mechanisma
        public const string alpsPropertyTypeImplements = "implements";
        public const string alpsPropertieTypeExtends = "extends";

        //RowNameFor UI Address
        public const string alpsPropertieTypeUIAddress = "uiAddress";

        //DocumentLablesAsALPS
        public const string alpsPropertieTypeDocumentType = "abstractLayeredPASSProcessModel";

        //ModelPages Property Types
        public const string alpsPropertieTypePageType = "pageType";
        public const string alpsPropertieTypePageLayer = "pageLayer";
        public const string alpsPropertieTypePageModelURI = "modelURI";
        public const string alpsPropertieTypePageModelVersion = "modelVersion";
        public const string alpsPropertieTypeSBDLinkedSubjectID = "subjectShapeID";
        public const string alpsPropertieTypePriorityOrderNumber = "priorityOrder";
        
        //States AND Subjects
        public const string alpsPropertieTypeLabel = "lable";


        public const string cellPropertyCategoryPrefix = "Prop.";
        public const string cellHyperlinkCategoryPrefix = "Hyperlink.";
        public const string cellValueSuffix = ".Value";
        public const string cellHyperlinkCategorySubAdressSuffix = ".SubAdress";

        // Subject Property Types
        public const string alpsPropertieTypeMessageListSend = "messageListSend";
        public const string alpsPropertieTypeMessageListReceived = "messageListReceive";
        public const string alpsPropertieTypeMultiSubject = "multiSubject";
        public const string alpsPropertieTypeStartSubject = "startSubject";
        public const string alpsPropertieTypeActorsphereDisplayOuterSpehere = "dispalyOuterSphere";
        public const string alpsPropertieTypeLinkedInterfaceResource = "linked_Resource";
        public const string alpsPropertieTypeMaximumNumberOfInstantiation = "maximumNumberOfInstantiation";

        //Connector Property Types
        public const string alpsPropertieTypeConnectorErrorDisplayMode = "connectorErrorDisplayMode";
        public const string alpsPropertieTypeBoxCanBeMovedFreely = "boxCanBeMovedFreely";

        //Message Connectors/Exchanges Property Types
        public const string alpsPropertieTypeMessageList = "messageList";
        public const string alpsPropertieTypeOriginSubject = "originSubject";
        public const string alpsPropertieTypeTargetSubject = "targetSubject";

        // Communication Channel
        public const string alpsPropertieTypeBiDirectionalChannel = "biDirectionalChannel";

        //SBD Connectors Propertie Types
        public const string alpsPropertieTypeOriginState = "originState";
        public const string alpsPropertieTypeTargetState = "targetState";
        public const string alpsPropertieTypeReceivingSubject = "receivingSubject";
        public const string alpsPropertieTypeSenderOfMessage = "senderOfMessage";
        public const string alpsPropertieTypeConnectorMessage = "message";
        public const string alpsPropertieTypeConnectorAlternativePriority = "alternativePriorityNumber"; // old version"messageReceivePriorityNumber";
        public const string alpsPropertieTypeMultiSendLowerBound = "multiSendLowerBound";
        public const string alpsPropertieTypeMultiSendUpperBound = "multiSendUpperBound";
        public const string alpsPropertieTypeMultiReceiveLowerBound = "multiReceiveLowerBound";
        public const string alpsPropertieTypeMultiReceiveUpperBound = "multiReceiveUpperBound";
        public const string alpsPropertieTypeReceiverSenderListForSubject = "receiverSenderListForSubject";
        public const string alpsPropertieTypePossibleMessageList = "possibleMessageList";
        public const string alpsPropertieTypeSendType = "sendingType";
        public const string alpsPropertieTypeReceiveType = "receiveType";
        public const string alpsPropertieTypeTimeOutType = "timeOutType"; //in timeout transitions to determin what typ it is.
        public const string alpsPropertieTypeTimeOutTime = "timeOutTime"; //in time out transitions a Prop. if a time duration is choosen
        public const string alpsPropertieTypeTimeOutDate = "timeOutDate"; // in time out transitions active if a calendar based time
        public const string alpsPropertieTypeTimeDisplayString = "timeDisplayString"; // in time out transitions a string fro the lables
        public const string alpsPropertieTypeDataMappingIncoming = "dataMappingIncomming";
        public const string alpsPropertieTypeDataMappingOutgoing = "dataMappingOutgoing";


        //SBD States Propertie Types
        public const string alpsPropertieTypeSBDStateIsStartState = "isStartState";
        public const string alpsPropertieTypeSBDStateIsEndState = "isEndState";
        public const string alpsPropertieTypeSBDStateIsAbstract = "isAbstract";
        public const string alpsPropertieTypeSBDStateIsFinalized = "isFinalized";
        public const string alpsPropertieTypeSBDStateHasRefinement = "hasRefinement";
        public const string alpsPropertieTypeSBDStateInCycle = "inCycle";
        public const string alpsPropertieTypeSBDStateMultiplicityLowerBound = "multiplicityLowerBound";
        public const string alpsPropertieTypeSBDStateMultiplicityUpperBound = "multiplicityUpperBound";
        public const string alpsPropertieTypeOptionalChecklistPath = "optionalPath";

        //Hyperlink Names
        public const string alpsHyperlinkTypeLinkedSBD = "linkedSBD";
        public const string alpsHyperlinksLinkedSIDPage = "linkedSIDPage";
        public const string alpsHyperlinksExtendedSubject = "extendedSubject"; //subject extensions should have their extension marked here
        public const string alpsHyperlinkExtendedState = "linkToExtendedState";

        //Prop.Valuesfor Pages
        public const string alpsPropertieValueSBDPage = "SubjectBehavior";
        public const string alpsPropertieValueSIDPage = "SubjectInteraction";

        //Prop.Values for Send and Receive Types
        public const string alpsPropertieValueSendTypeReceiveStandard = "standard";
        public const string alpsPropertieValueSendTypeReceiveMultiple = "multiple";
        public const string alpsPropertieValueSendTypeReceiveAll = "From all known";
        public const string alpsPropertieValueSendTypeSendStandard = "standard";
        public const string alpsPropertieValueSendTypeSendToNew = "send to new";
        public const string alpsPropertieValueSendTypeSendToKnown = "send to known";
        public const string alpsPropertieValueSendTypeSendToAll = "send to all (known)";

        public const string alpsDefaultValueURIReceiveMultiple = "http://www.imi.kit.edu/abstract-pass-ont//ReceiveMultiple";
        public const string alpsDefaultValueURIReceiveAll = "http://www.imi.kit.edu/abstract-pass-ont//ReceiveFromAllKnown";
        //http://www.imi.kit.edu/abstract-pass-ont//ReceiveStandard
        //http://www.imi.kit.edu/abstract-pass-ont//SendStandard
        public const string alpsDefaultValueURISendToNew = "http://www.imi.kit.edu/abstract-pass-ont//SendToNew";
        public const string alpsDefaultValueURISendToKnwon = "http://www.imi.kit.edu/abstract-pass-ont//SendToKnown";
        public const string alpsDefaultValueURISendToAll = "http://www.imi.kit.edu/abstract-pass-ont//SendToAllKnown";

        //Shape categories on SID
        public const string alpsShapeCategoryStandardActor = "StandardActor"; // "standardActor"
        public const string alpsShapeCategoryAbstractActor = "AbstractActor";
        public const string alpsShapeCategoryInterfaceActor = "InterfaceActor"; // "interfaceActor"
        public const string alpsShapeCategoryActorExtension = "ActorExtension";
        public const string alpsShapeCategoryActorPlaceHolder = "ActorPlaceHolder";
        public const string alpsShapeCategorySIDMessage = "alpsMessage";
        public const string alpsShapeCategorySIDMessageConnector = "alpsSIDMessageConnector";
        public const string alpsShapeCategorySIDMessageConnectorBox = "messageConnectorBox";
        public const string alpsShapeCategoryStandardMessageConnector = "standardMessageConnector";
        public const string alpsShapeCategoryAbstractMessageConnector = "AbstractMessageConnector";
        public const string alpsShapeCategoryExclusiveMessageConnector = "ExclusiveMessageConnector";
        public const string alpsShapeCategoryCommunicationRestriction = "CommunicationRestriction";


        //Shape Categorys/Types on SBDs
        public const string alpsShapeCategoryFunctionState = "functionState";
        public const string alpsShapeCategoryAbstractFunctionState = "abstractFunctionState";
        public const string alpsShapeCategorySendState = "sendState";
        public const string alpsShapeCategoryAbstractSendState = "abstractSendState";
        public const string alpsShapeCategoryReceiveState = "ReceiveState";
        public const string alpsShapeCategoryAbstractReceiveState = "abstractReceiveState";
        public const string alpsShapeCategoryGuardReceiveState = "GuardReceiveState";
        public const string alpsShapeCategoryGeneralAbstractState = "GeneralAbstractState";
        public const string alpsShapeCategoryStatePlaceHolder = "statePlaceHolder";
        public const string alpsShapeCategoryStateExtension = "StateExtension";
        public const string alpsShapeCategoryStandardTransition = "standardTransition";
        public const string alpsShapeCategoryTriggerTransition = "triggerTransition";
        public const string alpsShapeCategorySucessionTransition = "successionTransition";
        public const string alpsShapeCategoryFinalTransition = "finalTransition";
        public const string alpsShapeCategoryReceiveTransition = "ReceiveTransition";
        public const string alpsShapeCategoryTriggerReceiveTransition = "triggerReceiveTransition";
        public const string alpsShapeCategorySuccessionReceiveTransition = "successionReceiveTransition";
        public const string alpsShapeCategoryFinalReceiveTransition = "finalReceiveTransition";
        public const string alpsShapeCategorySendTransition = "SendTransition";
        public const string alpsShapeCategoryTriggerSendTransition = "triggerSendTransition";
        public const string alpsShapeCategorySuccessionSendTransition = "successionSendTransition";
        public const string alpsShapeCategoryFinalSendTransition = "finalSendTransition";
        public const string alpsShapeCategorySBDStateGroup = "stateGroup";
        public const string alpsShapeCategoryUserCancelTransition = "UserCancelTransition";
        public const string alpsShapeCategoryTimeOutTransition = "timeOutTransition";
        public const string alpsShapeCategoryChecklist = "Checklist";
        public const string alspShapeCategoryCheckListPath = "CheckListPath";
        public const string alpsShapeCategoryGuardReceive = "GuardReceive";
        public const string alpsShapeCategoryCheckboxPathInitialTransition = "checkboxPathInitialTransition";


        ////////////////////////// Shape Masters

        //SID Shape Masters Names in the SID Stencils
        public const string alpsSIDMasterStandardActor = "standardActor";
        public const string alpsSIDMasterStandardMessageConnector = "standardMessageConnector";
        public const string alpsSIDMasterMessageBox = "messageBox";
        public const string alpsSIDMasterMessage = "message";
        public const string alpsSIDMasterInterfaceActor = "interfaceActor";
        //public const string alpsSIDMasterAbstractActor = "abstractActor";
        public const string alpsSIDMasterActorPlaceHolder = "actorPlaceHolder";
        public const string alpsSIDMasterActorExtension = "actorExtension";
        //public const string alpsSIDMasterAbstractMessageConnector = "abstractMessageConnector";
        //public const string alpsSIDMasterExclusiveMessageConnector = ";ExclusiveMessageConnector";
        public const string alpsSIDMasterCommunicationRestriction = "CommunicationRestriction";
        public const string alpsSIDMasterAbstractCommunicationChannel = "AbstractCommunicationChannel";

        //SBD Shape.Masters Names in the SBD Stencils
        public const string alpsSBDMasterDoState = "FunctionState";
        public const string alpsSBDMasterSendState = "SendState";
        public const string alpsSBDMasterReceiveState = "ReceiveState";
        public const string alpsSBDMasterGeneralAbstractState = "GeneralAbstractState";
        public const string alpsSBDMasterPlaceHolder = "StatePlaceHolder";
        public const string alpsSBDMasterStateExtension = "StateExtension";
        public const string alpsSBDMasterStandardTransition = "StandardTransition";
        public const string alpsSBDMasterReceiveTransition = "ReceiveTransition";
        public const string alpsSBDMasterSendTransition = "SendTransition";
        public const string alpsSBDMasterSendingFailedTransition = "SendingFailedTransition";
        public const string alpsSBDMasterSBDStateGroup = "GroupState";
        public const string alpsSBDMasterGenericReturnToOriginReference = "GenericReturnToOriginReference";
        public const string alpsSBDMasterUserCancel = "UserCancelTransition";
        public const string alpsSBDMasterTimeTransition = "TimeTransition";
        public const string alpsSBDMasterTimeOut = "TimeOutTransition";
        public const string alpsSBDMasterChecklist = "Checklist";
        public const string alpsSBDMasterGuardReceive = "GuardReceive";
        public const string alpsSBDMasterInitialTransition = "InitialTransition";
        public const string alpsSBDMasterCheckListPath = "CheckListPath";
        public const string alpsSBDMasterFlowRestrictor = "FlowRestrictor";
        public const string ancPropertieTypeDDPageLinkedDDShape = "dataDefintionShapeID";



        //Simple Sim
        public const string simpleSimDurationMeanValue = "simpleSimDurationMeanValue";
        public const string simpleSimDurationStandardDeviation = "simpleSimDurationStandardDeviation";
        public const string simpleSimDurationDistributionType = "simpleSimDurationDistributionType";
        public const string simpleSimDurationMinValue = "simpleSimDurationMinValue";
        public const string simpleSimDurationMaxValue = "simpleSimDurationMaxValue";
        public const string simpleSimTranstionChoiceChance = "simpleSimTranstionChoiceChance"; //the chance of a (function) transition to be choosen
        public const string simpleSimStayChance = "simpleSimStayChance"; //the chance to remain in an End State and not continue- only for end states
        public const string simpleSimInterfaceSubjectResponseXML = "interfaceSubjectResponseXML";
        public const string simpleSimInterfaceSubjectResponseDefinitionAction = "responseDefintionAction";
        public const string simpleSimWaitingTimeFromLastRun = "simpleSimWaitingTimeFromLastRun";
    }
}
