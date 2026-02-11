# Data in ShapeSheet

## SID Page
[Shape Data]
- Prop.pageType ="SubjectInteraction"
- Prop.modelURI =URI":"Page.NameU
- Prop.pageLayer =Page.NameU # do we need?

### Both
[Shape Data]
- Prop.pageType ="SubjectInteraction"
### Visio
[Shape Data]
- Prop.author =""
- Prop.summary =""
- Prop.risksAndProblems =""
- Prop.modelURI ="http://subjective-me.jimdo.com/s-bpm/processmodels/2025-09-30/Zeichenblatt-1"
- Prop.modelVersion ="YYYY-MM-DD"
- Prop.pageLayer ="SID_X"
- Prop.extends =""
- Prop.implements =""
- Prop.priorityOrder ="1"
- Prop.documentCurrencySymbol =SETATREF(TheDoc!Prop.documentCurrencySymbol)
### AddIn
[User-defined Cells]
- User.OWLIMPORTINFORATIOMOD =0
[Shape Data]
- Prop.modelURI ="baseuri:SID_X"
- Prop.pageLayer =" "
- Prop.extends =" "
- Prop.implements =" "
- Prop.priorityOrder =" "

## Standart Subject
[User-defined Cells]
- User.containingPageID =Page.ID # do we need?
[Hyperlinks]
- Hyperlink.linkedSBD>SubAdress=SBD.NameU
[Shape Data]
- Prop.lable =Subject.Name

### Both
[User-defined Cells]
- User.containingPageID =_
[Hyperlinks]
- Hyperlink.linkedSBD SubAdress="Subject"
### Visio
[Shape Data]
- Prop.lable ="Subject "&ID()
- Prop.modelComponentID ="SID_"&(User.containingPageID+IF(User.containingPageID=0,1,0))&"_"&Prop.modelComponentType&"_"&ID()
- Prop.modelComponentType =IF(Prop.abstract,"AbstractSubject","FullySpecifiedSubject")
- Prop.maximumNumberOfInstantiation =IF(Prop.multiSubject,"*","1")
### AddIn
[Shape Data]
- Prop.lable ="NAME"
- Prop.modelComponentID ="SID_X_FullySpecifiedSubject_Y"
- Prop.modelComponentType ="ALPS_Visio_AddIn_rewrite.OWLShapes.VisioFullySpecifiedSubject"
- Prop.maximumNumberOfInstantiation ="1"

## SBD Page
[Shape Data]
- Prop.pageType ="SubjectBehavior"
- Prop.subjectShapeID =Subject.ID
- Prop.pageLayer =SIDPage.NameU # do we need?
[Hyperlinks]
- Hyperlink.linkedSIDPage>SubAdress=Pages[SIDPage.NameU]!ThePage!PAGENAME()

### Both
[Shape Data]
- Prop.pageType ="SubjectBehavior"
- Prop.subjectShapeID =X
### Visio
[Shape Data]
- Prop.pageLayer ="SID_X"
[Hyperlinks]
- Hyperlink.linkedSIDPage SubAdress=Pages[SID_X]!ThePage!PAGENAME()
### AddIn
[Shape Data]
- Prop.pageLayer =" "
[Hyperlinks]
- Hyperlink.linkedSIDPage SubAdress=" "

## Message Connector
[1-D Endpoints]
- BeginX =PAR(PNT(Subject!Connections.X_,Subject!Connections.Y_)) # do we need?
- BeginY =PAR(PNT(Subject!Connections.X_,Subject!Connections.Y_)) # do we need?
- EndX =PAR(PNT(Subject'!Connections.X_,Subject!Connections.Y_)) # do we need?
- EndY =PAR(PNT(Subject'!Connections.X_,Subject!Connections.Y_)) # do we need?
[User-defined Cells]
- User.globalX =SETATREF(MessageBox!User.connectorControlsPositionX) # do we need?
- User.globalY =SETATREF(MessageBox!User.connectorControlsPositionY) # do we need?
- User.idOfCorrespondingShape =MessageBox.ID
- User.containingPageID =Page.ID # do we need?
[Shape Data]
- Prop.lable =Name
- Prop.numberOfMessages =MessageBox!Prop.numberOfMessages

### Both
[1-D Endpoints]
- BeginX =PAR(PNT(Subject!Connections.X_,Subject!Connections.Y_))
- BeginY =PAR(PNT(Subject!Connections.X_,Subject!Connections.Y_))
- EndX =PAR(PNT(Subject!Connections.X_,Subject!Connections.Y_))
- EndY =PAR(PNT(Subject!Connections.X_,Subject!Connections.Y_))
[User-defined Cells]
- User.globalX =SETATREF(MessageBox!User.connectorControlsPositionX)
- User.globalY =SETATREF(MessageBox!User.connectorControlsPositionY)
- User.idOfCorrespondingShape =MessageBox!User.idOnPage
- User.containingPageID =_
[Shape Data]
- Prop.numberOfMessages =MessageBox!Prop.numberOfMessages
### Visio
[Shape Data]
- Prop.modelComponentType =INDEX(0,Prop.modelComponentType.Format)
- Prop.modelComponentID ="SID_"&(User.containingPageID+IF(User.containingPageID=0,1,0))&"_"&Prop.modelComponentType&"_"&ID()
- Prop.lable =Prop.modelComponentID
### AddIn
[Shape Data]
- Prop.modelComponentType ="ALPS_Visio_AddIn_rewrite.OWLShapes.VisioMessageExchange"
- Prop.modelComponentID ="MessageConnector_MessageSpecification"
- Prop.lable ="Message: Label From: Subject To: Subject"

## MessageBox
[User-defined Cells]
- User.idOfCorrespondingShape =MessageConnector.ID

### Both
[User-defined Cells]
- User.idOfCorrespondingShape =MessageConnector!User.idOnPage

## Message
[Shape Data]
- Prop.lable =Name
[ShapeLayout]
- Relationships =SUM(DEPENDSON(5,MessageBox!SheetRef())

### Both
[Shape Data]
- Prop.lable ="NAME"
### Visio
[Shape Data]
- Prop.modelComponentID =ThePage!Prop.pageLayer&"_"&Prop.modelComponentType&"_"&ID()
- Prop.modelComponentType ="MessageSpecification"
[ShapeLayout]
- Relationships =SUM(DEPENDSON(5,MessageBox!SheetRef())
### AddIn
[Shape Data]
- Prop.modelComponentID ="MessageSpecification"
- Prop.modelComponentType ="ALPS_Visio_AddIn_rewrite.OWLShapes.VisioMessageSpecification"
[ShapeLayout]
- Relationships =SUM(DEPENDSON(5,REF())