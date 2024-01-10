
using alps.net.api.parsing;

using alps.net.api.StandardPASS;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
//using System.Windows;
using System.Windows.Forms;
using VisioAddIn.OwlShapes;
using Visio = Microsoft.Office.Interop.Visio;
using alps.net.api;
using System.Reflection;
using alps.net.api.ALPS;
using System.Diagnostics;
using System.Text;

namespace VisioAddIn
{
    /// <summary>
    /// Importer which calls the alps.net.api library to create a visio model out of a given owl file. 
    /// To use the importer you have to at least give the standard pass ont and the owl file with the model at the beginning.
    /// </summary>
    public class OWLImporter
    {


        private string fileName;
        private IList<IPASSProcessModel> passProcessModels = new List<IPASSProcessModel>();

        private enum ModelLayerType { BaseLayer, ExtensionLayer, AbstractLayer, GuardLayer, MacroLayer };

        protected enum LayoutType { Auto, Visio, Addin };

        private double defaultX = -1.25;
        private double defaultY = 5.5;
        IPASSReaderWriter owlGraph;
        protected Boolean parsingWithSimple2DVisualisation = false;


        public OWLImporter(string fileName)
        {
            ReflectiveEnumerator.addAssemblyToCheckForTypes(Assembly.GetExecutingAssembly());

            owlGraph = PASSReaderWriter.getInstance();
            owlGraph.setModelElementFactory(new VisioClassFactory());
            //owlGraph.setModelElementFactory(new BasicPASSProcessModelElementFactory());

            //tempList:

            // Load parsing structure initially (needed by the library)
            owlGraph.loadOWLParsingStructure(new List<String>
                {   "../../Resources/standard_PASS_ont_v_1.1.0.owl",
                    "../../Resources/ALPS_ont_v_0.8.0.owl" 
                });


            this.fileName = fileName;
        }

        private string getShortModelComponentID(IPASSProcessModelElement element)
        {
            // ID consists (usually) of <baseUri>#<id>, so extract only the id part
            string[] splittedComponentID = element.getModelComponentID().Split('#');
            return splittedComponentID[splittedComponentID.Length - 1];
        }

        /// <summary>
        /// Parses the owl file and creates all the pages in Visio.
        /// Further this method will drop all of the elements on the SID Page and creates all the SBD pages which belong to a 
        /// fully specified subject. Then the method placeSBDShapes() will be called and places all of the shapes which belong to it.
        /// </summary>
        public void parse(Visio.Page sIDPage, Visio.Document activeDoc)
        {
            Debug.WriteLine("start parsing file: " + fileName);
            List<String> myList = new List<String> {fileName};
            passProcessModels = owlGraph.loadModels(myList);

            //necessary so the Visio VBA Listerners do not delete message on transitions before
            // the complete model has been imported
            VisioHelper.switchVBAListenersOFF();
            // New Code
            
            //TODO: let user choose if there are multiple models
            if (passProcessModels[0] is IVisioExportable exportable)
            {
                exportable.exportToVisio(sIDPage);
            }
            VisioHelper.switchVBAListenersON();
                

            

            //this.LayoutPages(Page.LayoutType.Visio);

        }

        //private void LayoutPages(Page.LayoutType type)
        //{
        //    foreach (SBDPage page in this.SBDPages.Values)
        //    {
        //        page.Layout(type);
        //        //page.adjustSize();
        //    }
        //    foreach (SIDPage page in this.pages.Values)
        //    {
        //        if (type == Page.LayoutType.Addin)
        //            type = Page.LayoutType.Visio;
        //        page.Layout(type);

        //        //page.adjustSize();
        //    }
        //}

        //private Visio.Shape placeShape(Visio.Page page, string masterType, IList<ISimple2DVisualizationPoint> points)
        //{
        //    double defaultX = 4.25;
        //    double defaultY = 5.5;
        //    double simpleXPos = defaultX;
        //    double simpleYPos = defaultY;

        //    if (!(points is null))
        //        foreach (ISimple2DVisualizationPoint point in points)
        //        {
        //            simpleXPos = point.getRelative2DPosX();
        //            simpleYPos = point.getRelative2DPosX();
        //        }


        //    Visio.Document sidShapes = VisioHelper.OpenSidStencils();
        //    Visio.Master sidMaster = sidShapes.Masters.get_ItemU(masterType);

        //    return page.Drop(sidMaster, simpleXPos, simpleYPos);

        //}

        /// <summary>
        /// Method that places all the SBD shapes on the given SBD pages. There are two possible ways to place the shapes. 
        /// Either there is a start state given or there is no start state given. Without a start state, all of the shapes will just be randomly placed on the page underneath each other.
        /// If a start state is given, each of the following states will be placed based on the transitions going out of each state. 
        /// The methodology behind the placement is that all of the elements are usually just placed in a line underneath each other. 
        /// </summary>
        /// <param name="sBDPage"></param>
        /// <param name="subjectBehavior"></param>
        //private void placeSBDShapes(Visio.Page sBDPage, ISubjectBehavior subjectBehavior)
        //{
        //    double xPos = 1;
        //    double yPos = 10;
        //    double increment = 2;
        //    string type = "";
        //    int counter = 0;
        //    Visio.Shape savedShape;

        //    IDictionary<string, Visio.Shape> placedTranisitions = new Dictionary<string, Visio.Shape>();
        //    IDictionary<string, Visio.Shape> placedStates = new Dictionary<string, Visio.Shape>();
        //    IDictionary<string, double> yposition = new Dictionary<string, double>();
        //    IDictionary<string, double> xposition = new Dictionary<string, double>();

        //    //if (subjectBehavior.getInitialStateOfBehavior() != null)
        //    if (false)
        //    {
        //        IState test = (State)subjectBehavior.getBehaviorDescribingComponents()[subjectBehavior.getInitialStateOfBehavior().getModelComponentID()];

        //        if (test is ISendState)
        //        {
        //            type = ALPSConstants.alpsSBDMasterSendState;
        //        }
        //        else
        //        {
        //            if (test is IReceiveState)
        //            {
        //                type = ALPSConstants.alpsSBDMasterReceiveState;
        //            }
        //            else
        //            {
        //                if (test is IDoState)
        //                {
        //                    type = ALPSConstants.alpsSBDMasterDoState;
        //                }
        //            }
        //        }

        //        Visio.Document sidShapes = VisioHelper.OpenSbdStencils();
        //        Visio.Master sidMaster = sidShapes.Masters.get_ItemU(type);
        //        Visio.Shape shape = sBDPage.Drop(sidMaster, xPos, yPos);

        //        shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentID].Formula = "\"" + test.getModelComponentID().Split('#')[1] + "\"";
        //        shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeLabel].Formula = "\"" + test.getModelComponentLabelsAsStrings()[0].Split('@')[0] + "\"";
        //        shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentType].Formula = "\"" + test.GetType() + "\"";

        //        yPos = yPos - increment;

        //        IList<ITransition> transitions = new List<ITransition>();

        //        foreach (KeyValuePair<string, IBehaviorDescribingComponent> i in subjectBehavior.getBehaviorDescribingComponents())
        //        {
        //            if (i.Value is ITransition transition)
        //            {
        //                transitions.Add(transition);
        //            }
        //        }
        //        savedShape = shape;

        //        placedStates.Add(test.getModelComponentID(), shape);
        //        yposition.Add(test.getModelComponentID(), yPos);
        //        xposition.Add(test.getModelComponentID(), xPos);

        //        while (test.getOutgoingTransitions() != null && counter < subjectBehavior.getBehaviorDescribingComponents().Count)
        //        {
        //            foreach (KeyValuePair<string, ITransition> i in test.getOutgoingTransitions())
        //            {
        //                test = i.Value.getTargetState();

        //                if (test == null)
        //                {
        //                    MessageBox.Show(i.Value.getModelComponentID() + "  In this element exists a Problem");

        //                }
        //                else
        //                {
        //                    if (test is ISendState)
        //                    {
        //                        type = ALPSConstants.alpsSBDMasterSendState;
        //                    }
        //                    else
        //                    {
        //                        if (test is IReceiveState)
        //                        {
        //                            type = ALPSConstants.alpsSBDMasterReceiveState;
        //                        }
        //                        else
        //                        {
        //                            if (test is IDoState)
        //                            {
        //                                type = ALPSConstants.alpsSBDMasterDoState;
        //                            }
        //                        }
        //                    }
        //                    if (!placedStates.ContainsKey(test.getModelComponentID()))
        //                    {

        //                        sidShapes = VisioHelper.OpenSbdStencils();
        //                        sidMaster = sidShapes.Masters.get_ItemU(type);
        //                        shape = sBDPage.Drop(sidMaster, xPos, yPos);

        //                        shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentID].Formula = "\"" + test.getModelComponentID().Split('#')[1] + "\"";
        //                        shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeLabel].Formula = "\"" + test.getModelComponentLabelsAsStrings()[0].Split('@')[0] + "\"";
        //                        shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentType].Formula = "\"" + test.GetType() + "\"";

        //                        xPos = xPos + increment;

        //                        placedStates.Add(test.getModelComponentID(), shape);
        //                        yposition.Add(test.getModelComponentID(), yPos);
        //                        xposition.Add(test.getModelComponentID(), xPos);
        //                    }
        //                    else
        //                    {
        //                        //MessageBox.Show(test.getModelComponentLabelsAsStrings()[0]);
        //                        //break;
        //                    }

        //                }
        //                savedShape = shape;
        //                xPos = 1;
        //                yPos = yPos - increment;

        //                //Get the next Shape
        //                test = test.getOutgoingTransitions().ElementAt(0).Value.getTargetState();
        //            }
        //            counter++;
        //        }

        //        foreach (IBehaviorDescribingComponent behaviorDescComp in subjectBehavior.getBehaviorDescribingComponents().Select(x => x.Value))
        //        {
        //            string currentShortModelComponentID = getShortModelComponentID(behaviorDescComp);
        //            string currentModelComponentID = behaviorDescComp.getModelComponentID();
        //            string currentModelFirstLabel = behaviorDescComp.getModelComponentLabelsAsStrings()[0];

        //            if (behaviorDescComp is ISendState && !placedStates.ContainsKey(currentModelComponentID))
        //            {
        //                type = ALPSConstants.alpsSBDMasterSendState;

        //                sidShapes = VisioHelper.OpenSbdStencils();
        //                sidMaster = sidShapes.Masters.get_ItemU(type);
        //                shape = sBDPage.Drop(sidMaster, xPos, yPos);

        //                yposition.Add(currentModelComponentID, yPos);
        //                xposition.Add(currentModelComponentID, xPos);

        //                yPos = yPos - increment;

        //                shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentID].Formula = "\"" + currentShortModelComponentID + "\"";
        //                shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeLabel].Formula = "\"" + currentModelFirstLabel + "\"";
        //                shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentType].Formula = "\"" + behaviorDescComp.GetType() + "\"";

        //                placedStates.Add(currentModelComponentID, shape);

        //            }
        //            else
        //            {
        //                if (behaviorDescComp is IReceiveState && !placedStates.ContainsKey(currentModelComponentID))
        //                {
        //                    type = ALPSConstants.alpsSBDMasterReceiveState;

        //                    sidShapes = VisioHelper.OpenSbdStencils();
        //                    sidMaster = sidShapes.Masters.get_ItemU(type);
        //                    shape = sBDPage.Drop(sidMaster, xPos, yPos);

        //                    yposition.Add(currentModelComponentID, yPos);
        //                    xposition.Add(currentModelComponentID, xPos);

        //                    yPos = yPos - increment;

        //                    shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentID].Formula = "\"" + currentShortModelComponentID + "\"";
        //                    shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeLabel].Formula = "\"" + currentModelFirstLabel + "\"";
        //                    shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentType].Formula = "\"" + behaviorDescComp.GetType() + "\"";

        //                    placedStates.Add(currentModelComponentID, shape);
        //                }
        //                else
        //                {
        //                    if (behaviorDescComp is IDoState && !placedStates.ContainsKey(currentModelComponentID))
        //                    {
        //                        type = ALPSConstants.alpsSBDMasterDoState;

        //                        sidShapes = VisioHelper.OpenSbdStencils();
        //                        sidMaster = sidShapes.Masters.get_ItemU(type);
        //                        shape = sBDPage.Drop(sidMaster, xPos, yPos);

        //                        yposition.Add(currentModelComponentID, yPos);
        //                        xposition.Add(currentModelComponentID, xPos);

        //                        yPos = yPos - increment;

        //                        shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentID].Formula = "\"" + currentShortModelComponentID + "\"";
        //                        shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeLabel].Formula = "\"" + currentModelFirstLabel + "\"";
        //                        shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentType].Formula = "\"" + behaviorDescComp.GetType() + "\"";

        //                        placedStates.Add(currentModelComponentID, shape);
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    else
        //    {
        //        //This should only be used when there is no start state given. Right now this is the default way to do it
        //        foreach (IBehaviorDescribingComponent behaviorDescComp in subjectBehavior.getBehaviorDescribingComponents().Select(x => x.Value))
        //        {
        //            string currentShortModelComponentID = getShortModelComponentID(behaviorDescComp);
        //            string currentModelComponentID = behaviorDescComp.getModelComponentID();
        //            string currentModelFirstLabel = behaviorDescComp.getModelComponentLabelsAsStrings()[0];

        //            if (behaviorDescComp is ISendState)
        //            {
        //                type = ALPSConstants.alpsSBDMasterSendState;

        //                Visio.Document sidShapes = VisioHelper.OpenSbdStencils();
        //                Visio.Master sidMaster = sidShapes.Masters.get_ItemU(type);
        //                Visio.Shape shape = sBDPage.Drop(sidMaster, xPos, yPos);

        //                yposition.Add(currentModelComponentID, yPos);
        //                xposition.Add(currentModelComponentID, xPos);

        //                yPos = yPos - increment;

        //                shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentID].Formula = "\"" + currentShortModelComponentID + "\"";
        //                shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeLabel].Formula = "\"" + currentModelFirstLabel + "\"";
        //                shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentType].Formula = "\"" + behaviorDescComp.GetType() + "\"";

        //                placedStates.Add(behaviorDescComp.getModelComponentID(), shape);

        //            }
        //            else
        //            {
        //                if (behaviorDescComp is IReceiveState)
        //                {
        //                    type = ALPSConstants.alpsSBDMasterReceiveState;

        //                    Visio.Document sidShapes = VisioHelper.OpenSbdStencils();
        //                    Visio.Master sidMaster = sidShapes.Masters.get_ItemU(type);
        //                    Visio.Shape shape = sBDPage.Drop(sidMaster, xPos, yPos);

        //                    yposition.Add(behaviorDescComp.getModelComponentID(), yPos);
        //                    xposition.Add(behaviorDescComp.getModelComponentID(), xPos);

        //                    yPos = yPos - increment;

        //                    shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentID].Formula = "\"" + currentShortModelComponentID + "\"";
        //                    shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeLabel].Formula = "\"" + currentModelFirstLabel + "\"";
        //                    shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentType].Formula = "\"" + behaviorDescComp.GetType() + "\"";

        //                    placedStates.Add(behaviorDescComp.getModelComponentID(), shape);
        //                }
        //                else
        //                {
        //                    if (behaviorDescComp is IDoState)
        //                    {
        //                        type = ALPSConstants.alpsSBDMasterDoState;

        //                        Visio.Document sidShapes = VisioHelper.OpenSbdStencils();
        //                        Visio.Master sidMaster = sidShapes.Masters.get_ItemU(type);
        //                        Visio.Shape shape = sBDPage.Drop(sidMaster, xPos, yPos);

        //                        yposition.Add(currentModelComponentID, yPos);
        //                        xposition.Add(currentModelComponentID, xPos);

        //                        yPos = yPos - increment;

        //                        shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentID].Formula = "\"" + currentShortModelComponentID + "\"";
        //                        shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeLabel].Formula = "\"" + currentModelFirstLabel + "\"";
        //                        shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentType].Formula = "\"" + behaviorDescComp.GetType() + "\"";

        //                        placedStates.Add(currentModelComponentID, shape);
        //                    }

        //                }
        //            }

        //        }

        //    }

        //    xPos = xPos + 2 * increment;

        //    //Nur zum testen kann eigentlich auch weg.
        //    if (true)
        //    {
        //        foreach (KeyValuePair<string, IBehaviorDescribingComponent> b in subjectBehavior.getBehaviorDescribingComponents())
        //        {

        //            if (b.Value is ISendTransition sendTransition)
        //            {
        //                type = ALPSConstants.alpsSBDMasterSendTransition;

        //                double pinYSource = yposition[sendTransition.getSourceState().getModelComponentID()];
        //                double pinYTarget = yposition[sendTransition.getTargetState().getModelComponentID()];

        //                Visio.Document sidShapes = VisioHelper.OpenSbdStencils();
        //                Visio.Master sidMaster = sidShapes.Masters.get_ItemU(type);
        //                Visio.Shape shape = sBDPage.Drop(sidMaster, xPos, (pinYSource - pinYTarget) / 2);

        //                xPos = xPos + 5;

        //                shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentID].Formula = "\"" + b.Value.getModelComponentID().Split('#')[1] + "\"";
        //                shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeLabel].Formula = "\"" + b.Value.getModelComponentLabelsAsStrings()[0].Split('@')[0] + "\"";
        //                shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentType].Formula = "\"" + b.Value.GetType() + "\"";
        //                //shape.CellsU["Prop.originState"].Formula = "\"" + ((alps.net_api.Transition)b.Value).getSourceState().getModelComponentID() + "\"";
        //                //shape.CellsU["Prop.targetState"].Formula = "\"" + ((alps.net_api.Transition)b.Value).getTargetState().getModelComponentID() + "\"";

        //                placedTranisitions.Add(b.Value.getModelComponentID(), shape);

        //                xPos = xPos + increment;

        //                shape.CellsU["BeginX"].GlueToPos(placedStates[sendTransition.getSourceState().getModelComponentID()], 1.0, 0.5);
        //                shape.CellsU["EndX"].GlueToPos(placedStates[sendTransition.getTargetState().getModelComponentID()], 1.0, 0.5);

        //                //MessageBox.Show(shape.CellsU["User.receiverSenderListForSubject"].Formula);
        //                //shape.CellsU["User.possibleMessageList"].Formula;

        //                //shape.CellsU["User.receiverSenderListForSubject"].Formula = "\"" + ((SendTransitionCondition)((alps.net_api.SendTransition)b.Value).getTransitionCondition()).getMessageSentTo().getModelComponentLabelsAsStrings()[0] + "\"";
        //                //shape.CellsU["User.possibleMessageList"].Formula = "\"" + ((SendTransitionCondition)((alps.net_api.SendTransition)b.Value).getTransitionCondition()).getSenderOfMessage().getModelComponentLabelsAsStrings()[0] + "\"";

        //            }
        //            else
        //            {
        //                if (b.Value is IReceiveTransition receiveTrans)
        //                {
        //                    type = ALPSConstants.alpsSBDMasterReceiveTransition;

        //                    double pinYSource = yposition[receiveTrans.getSourceState().getModelComponentID()];
        //                    double pinYTarget = yposition[receiveTrans.getTargetState().getModelComponentID()];

        //                    Visio.Document sidShapes = VisioHelper.OpenSbdStencils();
        //                    Visio.Master sidMaster = sidShapes.Masters.get_ItemU(type);
        //                    Visio.Shape shape = sBDPage.Drop(sidMaster, xPos, (pinYSource - pinYTarget) / 2);

        //                    shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentID].Formula = "\"" + b.Value.getModelComponentID().Split('#')[1] + "\"";
        //                    shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeLabel].Formula = "\"" + b.Value.getModelComponentLabelsAsStrings()[0].Split('@')[0] + "\"";
        //                    shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentType].Formula = "\"" + b.Value.GetType() + "\"";
        //                    //shape.CellsU["Prop.originState"].Formula = "\"" + ((alps.net_api.Transition)b.Value).getSourceState().getModelComponentID() + "\"";
        //                    //shape.CellsU["Prop.targetState"].Formula = "\"" + ((alps.net_api.Transition)b.Value).getTargetState().getModelComponentID() + "\"";

        //                    placedTranisitions.Add(b.Value.getModelComponentID(), shape);
        //                    xPos = xPos + 5;

        //                    shape.CellsU["BeginX"].GlueToPos(placedStates[receiveTrans.getSourceState().getModelComponentID()], 1, 0.5);
        //                    shape.CellsU["EndX"].GlueToPos(placedStates[receiveTrans.getTargetState().getModelComponentID()], 1, 0.5);

        //                    //shape.CellsU["User.receiverSenderListForSubject"].Formula = "\" \"";
        //                    //shape.CellsU["User.possibleMessageList"].Formula = "\" \"";

        //                    //shape.CellsU["User.receiverSenderListForSubject"].Formula = "\"" + ((ReceiveTransitionCondition)((alps.net_api.ReceiveTransition)b.Value).getTransitionCondition()).getMessageSentFrom().getModelComponentLabelsAsStrings()[0] + "\"";
        //                    //shape.CellsU["User.possibleMessageList"].Formula = "\"" + ((ReceiveTransitionCondition)((alps.net_api.ReceiveTransition)b.Value).getTransitionCondition()).getReceptionOfMessage().getModelComponentLabelsAsStrings()[0] + "\"";

        //                }
        //                else
        //                {
        //                    if (b.Value is IDoTransition doTransition)
        //                    {
        //                        type = ALPSConstants.alpsSBDMasterStandardTransition;
        //                        string doTransSendID = doTransition.getSourceState().getModelComponentID();
        //                        string doTransTargetID = doTransition.getTargetState().getModelComponentID();

        //                        double pinYSource = yposition[doTransSendID];
        //                        double pinYTarget = yposition[doTransTargetID];

        //                        Visio.Document sidShapes = VisioHelper.OpenSbdStencils();
        //                        Visio.Master sidMaster = sidShapes.Masters.get_ItemU(type);
        //                        Visio.Shape shape = sBDPage.Drop(sidMaster, xPos, (pinYSource - pinYTarget) / 2);

        //                        shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentID].Formula = "\"" + b.Value.getModelComponentID().Split('#')[1] + "\"";
        //                        shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeLabel].Formula = "\"" + b.Value.getModelComponentLabelsAsStrings()[0].Split('@')[0] + "\"";
        //                        shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentType].Formula = "\"" + b.Value.GetType() + "\"";
        //                        shape.CellsU["Prop.originState"].Formula = "\"" + doTransSendID + "\"";
        //                        shape.CellsU["Prop.targetState"].Formula = "\"" + doTransTargetID + "\"";

        //                        xPos = xPos + 5;

        //                        shape.CellsU["BeginX"].GlueToPos(placedStates[doTransSendID], 1.0, 0.5);
        //                        shape.CellsU["EndX"].GlueToPos(placedStates[doTransTargetID], 1.0, 0.5);

        //                    }

        //                }
        //            }

        //        }
        //    }
        //}




        /// <summary>
        /// Method that connects two 2D shapes with a given connector(1D Shape) 
        /// </summary>
        //private void connectShapes(Visio.Shape firstShape, Visio.Shape secondShape, Visio.Shape connector)
        //{
        //    //Testing where, in relation to each other, the two subjects are positiond 

        //    //Gut das ich das angefangen habe aber nie beendet habe. Vielleicht kann das ja jemand anders mal versuchen
        //    if (Double.Parse(firstShape.CellsU["PinX"].Formula) > Double.Parse(secondShape.CellsU["PinXP"].Formula))
        //    {
        //        if (Double.Parse(firstShape.CellsU["PinY"].Formula) > Double.Parse(secondShape.CellsU["PinY"].Formula))
        //        {
        //            //Annahme das nun first Shape rechts und unter der anderen Shape liegt. Verbinden auf der rechten Seit der secondShape mit der unter oberen Seite der firstShape
        //            //WIP: Methode welche fragt ob das der erste oder zweite connector an das subject ist um zu wissen wie wo genau die Konnectoren hin sollen
        //            connector.CellsU["BeginX"].GlueToPos(secondShape, 1, 0.33);
        //            connector.CellsU["EndY"].GlueToPos(firstShape, 0.33, 1);

        //        }
        //        else
        //        {
        //            if (Double.Parse(firstShape.CellsU["PinY"].Formula) == Double.Parse(secondShape.CellsU["PinY"].Formula))
        //            {
        //                //Annahme das nun beide Shapes auf der gleichen Höhe (y) liegen. Verbinden der rechten Seite von secondShape mit der linken Seite von firstShape
        //                connector.CellsU["BeginX"].GlueToPos(secondShape, 1, 0.33);
        //                connector.CellsU["EndY"].GlueToPos(firstShape, 0, 0.33);
        //            }
        //            else
        //            {
        //                //Nun bleibt nur noch das firstShape rechts und über der secondShape liegt. Verbinden der rechten Seite der secondShape mit der unteren Seite der firstShape
        //                connector.CellsU["BeginX"].GlueToPos(secondShape, 1, 0.33);
        //                connector.CellsU["EndY"].GlueToPos(firstShape, 0.33, 0);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        if (Double.Parse(firstShape.CellsU["PinX"].Formula) < Double.Parse(secondShape.CellsU["PinX"].Formula))
        //        {
        //            if (Double.Parse(firstShape.CellsU["PinY"].Formula) > Double.Parse(secondShape.CellsU["PinY"].Formula))
        //            {
        //                //Annahme das nun first Shape links und unter der anderen Shape liegt. Verbinden auf der rechten Seit der firstShape mit der unter oberen Seite der secondShape
        //                connector.CellsU["BeginX"].GlueToPos(firstShape, 1, 0.33);
        //                connector.CellsU["EndY"].GlueToPos(secondShape, 0.33, 1);
        //            }
        //            else
        //            {
        //                if (Double.Parse(firstShape.CellsU["PinY"].Formula) == Double.Parse(secondShape.CellsU["PinY"].Formula))
        //                {
        //                    //Annahme das nun beide Shapes auf der gleichen Höhe (y) liegen. Verbinden der rechten Seite von firstShape mit der linken Seite von secondShape
        //                    connector.CellsU["BeginX"].GlueToPos(firstShape, 1, 0.33);
        //                    connector.CellsU["EndY"].GlueToPos(secondShape, 0, 0.33);
        //                }
        //                else
        //                {
        //                    //Nun bleibt nur noch das firstShape rechts und über der secondShape liegt. Verbinden der rechten Seite der firstShape mit der unteren Seite der secondShape
        //                    connector.CellsU["BeginX"].GlueToPos(firstShape, 1, 0.33);
        //                    connector.CellsU["EndY"].GlueToPos(secondShape, 0.33, 0);
        //                }
        //            }
        //        }
        //        else
        //        {
        //            if (Double.Parse(firstShape.CellsU["PinY"].Formula) > Double.Parse(secondShape.CellsU["PinY"].Formula))
        //            {
        //                //Annahme das nun first Shape links und unter der anderen Shape liegt. Verbinden auf der rechten Seit der firstShape mit der unter oberen Seite der secondShape
        //                connector.CellsU["BeginX"].GlueToPos(firstShape, 1, 0.33);
        //                connector.CellsU["EndY"].GlueToPos(secondShape, 0.33, 1);
        //            }
        //            else
        //            {
        //                if (Double.Parse(firstShape.CellsU["PinY"].Formula) == Double.Parse(secondShape.CellsU["PinY"].Formula))
        //                {
        //                    //Annahme das nun beide Shapes auf der gleichen Höhe (y) liegen. Verbinden der rechten Seite von firstShape mit der linken Seite von secondShape
        //                    connector.CellsU["BeginX"].GlueToPos(firstShape, 1, 0.33);
        //                    connector.CellsU["EndY"].GlueToPos(secondShape, 0, 0.33);
        //                }
        //                else
        //                {
        //                    //Nun bleibt nur noch das firstShape rechts und über der secondShape liegt. Verbinden der rechten Seite der firstShape mit der unteren Seite der secondShape
        //                    connector.CellsU["BeginX"].GlueToPos(firstShape, 1, 0.33);
        //                    connector.CellsU["EndY"].GlueToPos(secondShape, 0.33, 0);
        //                }
        //            }
        //        }
        //    }
        //}

        //Das benutzte ich nur zwei mal. Ich könnte das mal ausbauen das es für alle funktioniert ?
        //private Visio.Shape place(Visio.Page page, string masterType, IList<ISimple2DVisualizationPoint> points)
        //{

        //    Visio.Document sidShapes = VisioHelper.OpenSidStencils();
        //    Visio.Master sidMaster = sidShapes.Masters.get_ItemU(masterType);

        //    this.defaultX = defaultX + 4;
        //    this.defaultY = defaultY - 2;

        //    return page.Drop(sidMaster, defaultX, defaultY);


        //}


        /*private void addShape(Dictionary<string, Element> shapes, String id, Element shape)
        {
            if (!shapes.ContainsKey(id))
            {
                shapes.Add(id, shape);
            }
        }*/

        //Kann auch mal weg
        /*private void ConnectObjects()
        {
            // load object connections from OWL
            foreach (OwlShapes.Shape shape in this.myShapes.Values)
            {
                shape.connectOWL(this.AllShapes);
            }
            // load object connections from OWL - 2nd Pass
            foreach (OwlShapes.Shape shape in this.myShapes.Values)
            {
                shape.connectOWLPass2(this.AllShapes);
            }

            // Connect OWL for SID and SBD Pages
            foreach (OwlShapes.SIDPage page in this.pages.Values)
            {
                page.ConnectOWL(this.AllShapes);
            }
            foreach (OwlShapes.SBDPage page in this.SBDPages.Values)
            {
                page.ConnectOWL(this.AllShapes);
            }
            // Connect OWL for SID and SBD Pages - 2nd pass
            foreach (OwlShapes.SIDPage page in this.pages.Values)
            {
                page.ConnectOWLPass2(this.AllShapes);
            }
            *//*
            foreach (OwlShapes.SBDPage page in this.SBDPages.Values)
            {
                page.ConnectOWLPass2(this.AllShapes);
            }
            *//*
        }

        //Warum ist das noch hier ??
        private void PlacePagesAndShapes()
        {
            // place Shapes

            //To Create an SBD-Page there has to be an Subject connect to it (Actor)

            foreach (OwlShapes.SIDPage page in this.pages.Values)
            {
                page.createPage(modelUri);
                page.placeShape();
            }
            foreach (OwlShapes.SIDPage page in this.pages.Values)
            {
                List<OwlShapes.Page> createdPages = new List<OwlShapes.Page>();
                foreach (Element shape in page.shapes.Values)
                {
                    if (shape is OwlShapes.SBDPage && !createdPages.Contains(shape))
                    {
                        OwlShapes.SBDPage sbd = (OwlShapes.SBDPage)shape;
                        sbd.createPage(page);
                        sbd.placeShape();
                        createdPages.Add(sbd);
                    }
                }
            }


        }

        //Irrelevant
        public void DumpObjectsAsJSON()
        {
            System.Diagnostics.Debug.WriteLine("[");
            int objCount = 0;
            foreach (OwlShapes.Element shape in this.AllShapes.Values)
            {
                objCount++;
                string line = shape.ToString();
                if (objCount < this.AllShapes.Count)
                {
                    line += ", ";
                }
                System.Diagnostics.Debug.WriteLine(line);
            }
            System.Diagnostics.Debug.WriteLine("]");
        }*/



        /*//Hm das kann weg glaube ich
        private void HandleProcessModel(OntologyClass c)
        {
            if (c.Instances.Count() == 1)
            {
                OntologyResource ressource = c.Instances.ElementAt(0);

                foreach (Triple t in ressource.TriplesWithSubject)
                {
                    Uri IDUri = new Uri(OWLGlobalVariables.StandardPassOntNamespace + "hasModelComponentID");
                    if (t.Predicate.NodeType == NodeType.Uri && t.Object.NodeType == NodeType.Literal)
                    {
                        Uri pred = ((UriNode)t.Predicate).Uri;
                        if (pred.AbsoluteUri == IDUri.AbsoluteUri)
                        {
                            modelUri = ((LiteralNode)t.Object).Value.ToString();
                        }
                    }
                }

                foreach (Triple t in ressource.TriplesWithSubject)
                {
                    Uri lableUri = new Uri(OWLGlobalVariables.StandardPassOntNamespace + "hasModelComponentLabel");
                    if (t.Predicate.NodeType == NodeType.Uri && t.Object.NodeType == NodeType.Literal)
                    {
                        Uri pred = ((UriNode)t.Predicate).Uri;
                        if (pred.AbsoluteUri == lableUri.AbsoluteUri)
                        {
                            modelLable = ((LiteralNode)t.Object).Value.ToString();
                        }
                    }

                }
            }
        }*/
        /*            
        private void HandleModelLayer(OntologyClass c)
        {
            if(c.Instances.Count() == 0)
            {
                //TODO
                //create SID-Page and get all SID-Shapes on it
                OwlShapes.SIDPage p = new OwlShapes.SIDPage();
                //...??? So much To Do
            }
            foreach (OntologyResource i in c.Instances)
            {
                //TODO: Detect if ExtensionLayer
                OwlShapes.SIDPage p;
                ModelLayerType type = detectModelLayerType(i);
                switch (type)
                {
                    default:
                    case ModelLayerType.BaseLayer: p = new OwlShapes.SIDPage(); break;
                    case ModelLayerType.ExtensionLayer: p = new OwlShapes.ExtendingSIDPage(); break;
                }
                p.fromOWL(this.graph, i);
                p.modelUri = i.Triples.ElementAt(0).Subject.GraphUri.AbsoluteUri.ToString();
                this.addShape(this.AllShapes, p.getModelComponentID(), p);
                if (!this.pages.ContainsKey(p.getModelComponentID()))
                {
                    this.pages.Add(p.getModelComponentID(), p);
                }
            }
        }
        
        private void HandleSubjectBehavior(OntologyClass c)
        {
            foreach (OntologyResource i in c.Instances)
            {
                OwlShapes.SBDPage p = new OwlShapes.SBDPage();
                p.fromOWL(this.graph, i);
                this.addShape(this.AllShapes, p.getModelComponentID(), p);
                if (!this.SBDPages.ContainsKey(p.getModelComponentID()))
                {
                    this.SBDPages.Add(p.getModelComponentID(), p);
                }
            }
        }

        private void HandleDoState(OntologyClass c)
        {
            foreach (OntologyResource i in c.Instances)
            {
                FunctionState s = new FunctionState();
                s.fromOWL(this.graph, i);
                this.addShape(this.myShapes, s.getModelComponentID(), s);
                this.addShape(this.AllShapes, s.getModelComponentID(), s);
            }
        }

        private void HandleSendState(OntologyClass c)
        {
            foreach (OntologyResource i in c.Instances)
            {
                SendState s = new SendState();
                s.fromOWL(this.graph, i);
                this.addShape(this.myShapes, s.getModelComponentID(), s);
                this.addShape(this.AllShapes, s.getModelComponentID(), s);
            }
        }
        private void HandleReceiveState(OntologyClass c)
        {
            foreach (OntologyResource i in c.Instances)
            {
                ReceiveState s = new ReceiveState();
                s.fromOWL(this.graph, i);
                this.addShape(this.myShapes, s.getModelComponentID(), s);
                this.addShape(this.AllShapes, s.getModelComponentID(), s);
            }
        }

        private void HandleStateExtension(OntologyClass c)
        {

            foreach (OntologyResource i in c.Instances)
            {
                StateExtension s = new StateExtension();
                s.fromOWL(this.graph, i);
                this.addShape(this.myShapes, s.getModelComponentID(), s);
                this.addShape(this.AllShapes, s.getModelComponentID(), s);
            }
        }

        private void HandleTimeBasedReminderTransition(OntologyClass c)
        {
            foreach (OntologyResource i in c.Instances)
            {
                TimeBasedTransition s = new TimeBasedTransition();
                s.fromOWL(this.graph, i);
                this.addShape(this.myShapes, s.getModelComponentID(), s);
                this.addShape(this.AllShapes, s.getModelComponentID(), s);
            }
        }

        private void HandleYearMonthTimerTransition(OntologyClass c)
        {
            foreach (OntologyResource i in c.Instances)
            {
                YearMonthTransition s = new YearMonthTransition();
                s.fromOWL(this.graph, i);
                this.addShape(this.myShapes, s.getModelComponentID(), s);
                this.addShape(this.AllShapes, s.getModelComponentID(), s);
            }
        }

        private void HandleCalendarBasedReminderTransition(OntologyClass c)
        {
            foreach (OntologyResource i in c.Instances)
            {
                CalendarBasedTransition s = new CalendarBasedTransition();
                s.fromOWL(this.graph, i);
                this.addShape(this.myShapes, s.getModelComponentID(), s);
                this.addShape(this.AllShapes, s.getModelComponentID(), s);
            }
        }

        private void HandleBusinessDayTimerTransition(OntologyClass c)
        {
            foreach (OntologyResource i in c.Instances)
            {
                BusinessDayTransition s = new BusinessDayTransition();
                s.fromOWL(this.graph, i);
                this.addShape(this.myShapes, s.getModelComponentID(), s);
                this.addShape(this.AllShapes, s.getModelComponentID(), s);
            }
        }

        private void HandleDayTimeTimerTransition(OntologyClass c)
        {
            foreach (OntologyResource i in c.Instances)
            {
                DayTimeTransition s = new DayTimeTransition();
                s.fromOWL(this.graph, i);
                this.addShape(this.myShapes, s.getModelComponentID(), s);
                this.addShape(this.AllShapes, s.getModelComponentID(), s);
            }
        }


        private void HandleDoTransition(OntologyClass c)
        {
            foreach (OntologyResource i in c.Instances)
            {
                DoTransition s = new DoTransition();
                s.fromOWL(this.graph, i);
                this.addShape(this.myShapes, s.getModelComponentID(), s);
                this.addShape(this.AllShapes, s.getModelComponentID(), s);
            }
        }

        private void HandleSendTransition(OntologyClass c)
        {
            foreach (OntologyResource i in c.Instances)
            {
                SendTransition s = new SendTransition();
                s.fromOWL(this.graph, i);
                this.addShape(this.myShapes, s.getModelComponentID(), s);
                this.addShape(this.AllShapes, s.getModelComponentID(), s);
            }
        }

        private void HandleReceiveTransition(OntologyClass c)
        {
            foreach (OntologyResource i in c.Instances)
            {
                ReceiveTransition s = new ReceiveTransition();
                s.fromOWL(this.graph, i);
                this.addShape(this.myShapes, s.getModelComponentID(), s);
                this.addShape(this.AllShapes, s.getModelComponentID(), s);
            }
        }

        private void HandleFullySpecifiedSingleSubject(OntologyClass c)
        {
            foreach (OntologyResource i in c.Instances)
            {
                StandardActor a = new StandardActor();
                a.fromOWL(this.graph, i);
                this.addShape(this.myShapes, a.getModelComponentID(), a);
                this.addShape(this.AllShapes, a.getModelComponentID(), a);
            }
        }

        private void HandleInterfaceSubject(OntologyClass c)
        {
            foreach (OntologyResource i in c.Instances)
            {
                InterfaceActor a = new InterfaceActor();
                a.fromOWL(this.graph, i);
                this.addShape(this.myShapes, a.getModelComponentID(), a);
                this.addShape(this.AllShapes, a.getModelComponentID(), a);
            }
        }

        private void HandleSubjectExtension(OntologyClass c)
        {
            foreach (OntologyResource i in c.Instances)
            {
                ActorExtension a = new ActorExtension();
                a.fromOWL(this.graph, i);
                this.addShape(this.myShapes, a.getModelComponentID(), a);
                this.addShape(this.AllShapes, a.getModelComponentID(), a);
            }
        }
        private void HandleStandardMessageExchange(OntologyClass c)
        {
            foreach (OntologyResource i in c.Instances)
            {
                StandardMessageExchange a = new StandardMessageExchange();
                a.fromOWL(this.graph, i);
                this.addShape(this.myShapes, a.getModelComponentID(), a);
                this.addShape(this.AllShapes, a.getModelComponentID(), a);
            }
        }

        private void HandleMessageExchangeList(OntologyClass c)
        {
            foreach (OntologyResource i in c.Instances)
            {
                SIDMessageConnector a = new SIDMessageConnector();
                a.fromOWL(this.graph, i);
                this.addShape(this.myShapes, a.getModelComponentID(), a);
                this.addShape(this.AllShapes, a.getModelComponentID(), a);
            }
        }

        private void HandleMessageSpecification(OntologyClass c)
        {
            foreach (OntologyResource i in c.Instances)
            {
                Message a = new Message();
                a.fromOWL(this.graph, i);
                this.addShape(this.myShapes, a.getModelComponentID(), a);
                this.addShape(this.AllShapes, a.getModelComponentID(), a);
            }
        }

        private void HandleGroupState(OntologyClass c)
        {
            foreach (OntologyResource i in c.Instances)
            {
                GroupState a = new GroupState();
                a.fromOWL(this.graph, i);
                this.addShape(this.myShapes, a.getModelComponentID(), a);
                this.addShape(this.AllShapes, a.getModelComponentID(), a);
            }
        }



        /// <summary>
        /// places a shape on the given visio page
        /// </summary>
        /// <param name="page">visio page</param>
        /// <param name="shapeName">master name of the shape</param>
        private Visio.Shape PlaceShape(Visio.Page page, string shapeName)
        {
            Visio.Documents visioDocs = Globals.ThisAddIn.Application.Documents;
            Visio.Document sidShapes = visioDocs.OpenEx(GlobalVariables.getSIDName(),
                (short)Microsoft.Office.Interop.Visio.VisOpenSaveArgs.visOpenDocked);
            Visio.Document sbdShapes = visioDocs.OpenEx(GlobalVariables.getSBDName(),
                (short)Microsoft.Office.Interop.Visio.VisOpenSaveArgs.visOpenDocked);

            Random rnd = new Random();
            Visio.Master sidMaster = sidShapes.Masters.get_ItemU(shapeName);
            Visio.Shape shape = page.Drop(sidMaster, 5, 5);
            //Anordnung der Shapes
            page.LayoutIncremental(VisLayoutIncrementalType.visLayoutIncrSpace, VisLayoutHorzAlignType.visLayoutHorzAlignDefault,VisLayoutVertAlignType.visLayoutVertAlignDefault, 1, 1, VisUnitCodes.visCentimeters);
            return shape;
        }

        /// <summary>
        /// places a standard actor and defines all the specified parameters
        /// </summary>
        /// <param name="page">page where it should be placed</param>
        /// <param name="shapeName">name of shape type in the stencil set</param>
        /// <param name="modelComponentID">ID of component</param>
        /// <param name="lable">lable that should be displayed</param>
        /// <param name="instantiationMax">max no of instantiations possible</param>
        /// <returns>the placed and customized shape</returns>
        public Visio.Shape PlaceStandardActor(Visio.Page page, string shapeName, string modelComponentID, string lable, string instantiationMax)
        {
            Visio.Shape shape = PlaceShape(page, shapeName);
            shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentID].Formula = "\"" + modelComponentID + "\"";
            shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeLable].Formula = "\"" + lable + "\"";
            shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeMaximumNumberOfInstantiation].Formula = instantiationMax;

            return shape;
        }
 
        public void CreateDummies()
        {
            Visio.Page sid = VisioHelper.CreateSIDPage("SID_1", "SID_1", "", "", "", "1");

            //Test Standard Actor
            Visio.Shape act1 = this.PlaceStandardActor(sid, "standardActor", "SID_1_FullySpecifiedSubject_1", "test actor 1", "2");
            Visio.Page sbd1 = VisioHelper.CreateSBDPage(sid, "SBD_1", "test_sbd_u1", act1);
            
            Visio.Shape act2 = this.PlaceStandardActor(sid, "standardActor", "SID_1_FullySpecifiedSubject_2", "test actor 2", "2");
            Visio.Page sbd2 = VisioHelper.CreateSBDPage(sid, "SBD_2", "test_sbd_u2", act2);
            

            /*
             * Additional Actors to test 
            Visio.Shape act3 = this.PlaceStandardActor(sid, "standardActor", "SID_1_FullySpecifiedSubject_3", "test actor 3", "2");
            Visio.Page sbd3 = this.CreateSBDPage(sid, "SBD_3", "test_sbd_u3", act3);

            Visio.Shape act4 = this.PlaceStandardActor(sid, "standardActor", "SID_1_FullySpecifiedSubject_4", "test actor 4", "2");
            Visio.Page sbd4 = this.CreateSBDPage(sid, "SBD_4", "test_sbd_u4", act4);
            */
        /*
        FunctionState fs = new FunctionState();
        fs.isEndState = false;
        fs.modelComponentID = "SBD_4_DoState_71";
        fs.lable = "Hello! I'm a Do-State";
        fs.PlaceShape(sbd1);
        */
        /*
         * Different Transitions to test them in the CreateDummy()-Method
        SendState ss2 = new SendState();
        ss2.isEndState = false;
        ss2.isStartState = false;
        ss2.lable = "Ich bin ein SendState";
        ss2.PlaceShape(sbd1);

        DoTransition dt1 = new DoTransition();
        dt1.lable = "Message Inhalt DoTransition";
        dt1.PlaceShape(sbd1);
        dt1.connectState(ss2.vshape, fs.vshape);

        SendTransition st1 = new SendTransition();
        st1.lable = "To: Lable xx: Msg: Message Inhalt";
        st1.PlaceShape(sbd1);
        st1.connectState(ss2.vshape, fs.vshape);

        ReceiveTransition rt1 = new ReceiveTransition();
        rt1.lable = "From: Lable xy: Msg: Message Inhalt";
        rt1.PlaceShape(sbd1);
        rt1.connectState(ss2.vshape, fs.vshape);

        TimeOutTransition timeOutT1 = new TimeOutTransition();
        timeOutT1.lable = "Time Out Transition Message";
        timeOutT1.PlaceShape(sbd1);
        timeOutT1.connectState(ss2.vshape, fs.vshape);

        UserCancel uc1 = new UserCancel();
        uc1.lable = "User Cancel Transition Message";
        uc1.PlaceShape(sbd1);
        uc1.connectState(ss2.vshape, fs.vshape);

        SendState ss = new SendState();
        ss.isEndState = false;
        ss.isStartState = false;
        ss.lable= "Ich bin ein SendState";
        ss.PlaceShape(sbd2);
        */

        /*Further SIDMessages
        SIDMessage message12 = new SIDMessage();
        message12.lable = "Nachricht 1";
        message12.modelComponentID = "";
        message12.PlaceShape(sid);

        SIDMessage message13 = new SIDMessage();
        message13.lable = "Nachricht 2";
        message13.modelComponentID = "";
        message13.PlaceShape(sid);
        */
        /*
        StandardActor actor1 = new StandardActor();
        actor1.modelComponentID = "SID_1_FullySpecifiedSubject_1";
        actor1.lable = "Kunde";
        actor1.PlaceShape(sid);

        StandardActor actor2 = new StandardActor();
        actor2.modelComponentID = "SID_2_FullySpecifiedSubject_2";
        actor2.lable = "Lager";
        actor2.PlaceShape(sid);


        SIDMessageConnector mconnector = new SIDMessageConnector();
        mconnector.lable = "Connection";
        mconnector.modelComponentID = "testID";
        mconnector.originSubject = actor1.modelComponentID;
        mconnector.targetSubject = actor2.modelComponentID;
        //mconnector.Messages.Add(message12);
        //mconnector.Messages.Add(message13);
        mconnector.PlaceShape(sid);

        mconnector.connectShape(actor2.vshape, actor1.vshape);      
        */


        /*
        private ModelLayerType detectModelLayerType(OntologyResource res)
        {

            Uri typeUri = new Uri(OWLGlobalVariables.typeUri);

            Uri extensionLayerUri = new Uri(OWL.AbstractPassOnt.nameSpace + OWL.AbstractPassOnt.uriFragmentExtensionLayer);
            Uri baseLayerUri = new Uri(OWL.AbstractPassOnt.nameSpace + OWL.AbstractPassOnt.uriFragmentBaseLayer);
            Uri guardLayerUri = new Uri(OWL.AbstractPassOnt.nameSpace + OWL.AbstractPassOnt.uriFragmentGuardLayer);
            Uri abstractLayerUri = new Uri(OWL.AbstractPassOnt.nameSpace + OWL.AbstractPassOnt.uriFragmentAbstractLayer);
            Uri macroLayerUri = new Uri(OWL.AbstractPassOnt.nameSpace + OWL.AbstractPassOnt.uriFragmentMacroLayer);

            IEnumerable<INode> properties = res.GetResourceProperty(OWLGlobalVariables.typeUri);

            ModelLayerType result = ModelLayerType.BaseLayer;

            foreach (UriNode uriNode in properties)
            {
                Uri node = uriNode.Uri;
                if (node.AbsoluteUri.Equals(macroLayerUri.AbsoluteUri)) {
                    result = ModelLayerType.MacroLayer;
                }
                else if (node.AbsoluteUri.Equals(abstractLayerUri.AbsoluteUri)) {
                    result = ModelLayerType.AbstractLayer;
                }
                else if (node.AbsoluteUri.Equals(guardLayerUri.AbsoluteUri)) {
                    result = ModelLayerType.GuardLayer;
                }
                else if (node.AbsoluteUri.Equals(baseLayerUri.AbsoluteUri)) {
                    result = ModelLayerType.BaseLayer;
                }
                else if(node.AbsoluteUri.Equals(extensionLayerUri.AbsoluteUri)){
                    result = ModelLayerType.ExtensionLayer; 
                }
            }
            return result;
        }
         */

    }
}








/*
 foreach (KeyValuePair<string, BehaviorDescriptionComponent> f in fullySpecifiedSubject.getSubjectBehavior().getBehaviorDescribingComponents())
                        {
                            if (new alps.net_api.SendState().GetType().Equals(f.Value.GetType()))
                            {
                                Visio.Document sidShapes = VisioHelper.OpenSbdStencils();
                                Visio.Master sidMaster = sidShapes.Masters.get_ItemU(ALPSConstants.alpsSBDMasterSendState);
                                shape = SBDPages[counting].Drop(sidMaster, 4.25, 5.5);

                                shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentID].Formula = "\"" + f.Value.getModelComponentID().Split('#')[1] + "\"";
                                shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeLable].Formula = "\"" + f.Value.getModelComponentLabelsAsStrings()[0] + "\"";
                                shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentType].Formula = "\"" + f.Value.GetType() + "\"";

                            }

                            if (new alps.net_api.ReceiveState().GetType().Equals(f.Value.GetType()))
                            {
                                Visio.Document sidShapes = VisioHelper.OpenSbdStencils();
                                Visio.Master sidMaster = sidShapes.Masters.get_ItemU(ALPSConstants.alpsSBDMasterReceiveState);
                                shape = SBDPages[counting].Drop(sidMaster, 4.25, 5.5);

                                shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentID].Formula = "\"" + f.Value.getModelComponentID().Split('#')[1] + "\"";
                                shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeLable].Formula = "\"" + f.Value.getModelComponentLabelsAsStrings()[0] + "\"";
                                shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentType].Formula = "\"" + f.Value.GetType() + "\"";

                            }

                            if (new alps.net_api.DoState().GetType().Equals(f.Value.GetType()))
                            {
                                Visio.Document sidShapes = VisioHelper.OpenSbdStencils();
                                Visio.Master sidMaster = sidShapes.Masters.get_ItemU(ALPSConstants.alpsSBDMasterFunctionState);
                                shape = SBDPages[counting].Drop(sidMaster, 4.25, 5.5);

                                shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentID].Formula = "\"" + f.Value.getModelComponentID().Split('#')[1] + "\"";
                                shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeLable].Formula = "\"" + f.Value.getModelComponentLabelsAsStrings()[0] + "\"";
                                shape.CellsU["Prop." + ALPSConstants.alpsPropertieTypeModelComponentType].Formula = "\"" + f.Value.GetType() + "\"";

                            }
                        }
     */

