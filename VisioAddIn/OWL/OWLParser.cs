using Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Visio = Microsoft.Office.Interop.Visio;
using VisioAddIn1;
using Model;
using VisioAddIn1.OwlShapes;
using Microsoft.Office.Interop.Visio;
using VDS.RDF.Ontology;
using VDS.RDF.Parsing;
using VDS.RDF;

namespace VisioAddIn1
{
    class OWLParser
    {
        private string FileName;
        private OntologyGraph graph;
        private Dictionary<String, Element> myShapes;
        private Dictionary<String, Element> AllShapes;
        private Dictionary<String, OwlShapes.SIDPage> pages;

        private Dictionary<String, OwlShapes.SBDPage> SBDPages;

        private string modelUri;
        private string modelLable;

        private enum ModelLayerType {BaseLayer, ExtensionLayer, AbstractLayer, GuardLayer, MacroLayer};

        protected enum LayoutType{Auto, Visio, Addin };

        public OWLParser(string fileName)
        {
            FileName = fileName;
        }

        public void LoadFile( string fileName )
        {
            this.myShapes = new Dictionary<string, Element>();
            this.AllShapes = new Dictionary<string, Element>();
            this.pages = new Dictionary<string, OwlShapes.SIDPage>();
            this.SBDPages = new Dictionary<string, OwlShapes.SBDPage>();
            graph = new OntologyGraph();           
            graph.BaseUri = new Uri(ALPSConstants.alpsDefaultModelNameSpace);
            FileLoader.Load(graph, fileName);
            EmbeddedResourceLoader.Load(graph, "VisioAddIn1.Resources.standard-pass-ont.owl, VisioAddIn1");
            EmbeddedResourceLoader.Load(graph, "VisioAddIn1.Resources.abstract-layered-pass-ont.owl, VisioAddIn1");

        }

        /// <summary>
        /// parses the file and creates all the pages in Visio.
        /// </summary>
        public void Parse()
        {
            this.LoadFile( this.FileName );
            
            foreach (OntologyClass ontClass in this.graph.OwlClasses)
            {
                if (ontClass.Resource.NodeType == NodeType.Uri)
                {
                    // handle each OWL class
                    UriNode ressource = (UriNode)ontClass.Resource;
                    if (ressource.Uri.Fragment == "#" + OWL.StandardPassOnt.uriFragmentProcessModel)
                    {
                        this.HandleProcessModel(ontClass);
                    }
                    else if (ressource.Uri.Fragment == "#" + OWL.AbstractPassOnt.uriFragmentModelLayer)
                    {
                        this.HandleModelLayer(ontClass);
                    }
                    else if (ressource.Uri.Fragment == "#" + OWL.StandardPassOnt.uriFragmentSubjectBehavior)
                    {
                        this.HandleSubjectBehavior(ontClass);
                    }

                    else if (ressource.Uri.Fragment == "#" + OWL.StandardPassOnt.uriFragmentDoState)
                    {
                        this.HandleDoState(ontClass);
                    }

                    else if (ressource.Uri.Fragment == "#" + OWL.StandardPassOnt.uriFragmentSendState)
                    {
                        this.HandleSendState(ontClass);
                    }
                    else if (ressource.Uri.Fragment == "#" + OWL.StandardPassOnt.uriFragmentReceiveState)
                    {
                        this.HandleReceiveState(ontClass);
                    }
                    else if (ressource.Uri.Fragment == "#" + OWL.AbstractPassOnt.uriFragmentStateExtension)
                    {
                        this.HandleStateExtension(ontClass);
                    }
                    else if (ressource.Uri.Fragment == "#" + OWL.StandardPassOnt.uriFragmentDoTransition)
                    {
                        this.HandleDoTransition(ontClass);
                    }
                    else if (ressource.Uri.Fragment == "#" + OWL.StandardPassOnt.uriFragmentSendTransition)
                    {
                        this.HandleSendTransition(ontClass);
                    }
                    else if (ressource.Uri.Fragment == "#" + OWL.StandardPassOnt.uriFragmentReceiveTransition)
                    {
                        this.HandleReceiveTransition(ontClass);
                    }
                    else if (ressource.Uri.Fragment == "#" + OWL.StandardPassOnt.uriFragmentTimeBasedReminderTransition)
                    {
                        this.HandleTimeBasedReminderTransition(ontClass);
                    }
                    else if (ressource.Uri.Fragment == "#" + OWL.StandardPassOnt.uriFragmentYearMonthTimerTransition)
                    {
                        this.HandleYearMonthTimerTransition(ontClass);
                    }
                    else if (ressource.Uri.Fragment == "#" + OWL.StandardPassOnt.uriFragmentCalendarBasedReminderTransition)
                    {
                        this.HandleCalendarBasedReminderTransition(ontClass);
                    }
                    else if (ressource.Uri.Fragment == "#" + OWL.StandardPassOnt.uriFragmentBusinessDayTimerTransition)
                    {
                        this.HandleBusinessDayTimerTransition(ontClass);
                    }
                    else if (ressource.Uri.Fragment == "#" + OWL.StandardPassOnt.uriFragmentDayTimeTimerTransition)
                    {
                        this.HandleDayTimeTimerTransition(ontClass);
                    }
                    else if (ressource.Uri.Fragment == "#" + OWL.StandardPassOnt.uriFragmentFullySpecifiedSingleSubject)
                    {
                        this.HandleFullySpecifiedSingleSubject(ontClass);
                    }
                    else if (ressource.Uri.Fragment == "#" + OWL.StandardPassOnt.uriFragmentInterfaceSubject)
                    {
                        this.HandleInterfaceSubject(ontClass);
                    }
                    else if (ressource.Uri.Fragment == "#" + OWL.AbstractPassOnt.uriFragmentSubjectExtension)
                    {
                        this.HandleSubjectExtension(ontClass);
                    }
                    else if (ressource.Uri.Fragment == "#" + OWL.StandardPassOnt.uriFragmentMessageExchange)
                    {
                        this.HandleStandardMessageExchange(ontClass);
                    }
                    else if (ressource.Uri.Fragment == "#" + OWL.StandardPassOnt.uriFragmentMessageExchangeList)
                    {
                        this.HandleMessageExchangeList(ontClass);
                    }
                    else if (ressource.Uri.Fragment == "#" + OWL.StandardPassOnt.uriFragmentMessageSpecification)
                    {
                        this.HandleMessageSpecification(ontClass);
                    }
                    else if (ressource.Uri.Fragment == "#" + OWL.AbstractPassOnt.uriFragmentGroupState)
                    {
                        this.HandleGroupState(ontClass);
                    }

                }
            }

            // load OWL object/pages connections into our own objects
            this.ConnectObjects();
            // Place Pages and Shapes into Visio to create the visio objects
            this.PlacePagesAndShapes();
            // Finally Connect the visio shapes
            foreach (Element shape in this.myShapes.Values)
            {
                if (shape is Jointer)
                {
                    shape.connectShape();
                }
                
            }
            // Print all Shapes and Pages as Json for Debugging
            // Use a online JSON formatter to get the maximum of infos
            this.DumpObjectsAsJSON();

            /*
            //Save File with new Name
            string docName = this.FileName.Remove(this.FileName.Length - 4) + ".vsdx";
            Visio.Document doc = Globals.ThisAddIn.Application.ActiveDocument;
            doc.SaveAs(docName);
            */
            string[] temp = FileName.Split('\\');
            string docName = temp[temp.Length - 1];
            docName = modelLable + ".vsdx";
            Globals.ThisAddIn.openSaveFileDialog(docName);

            // auto layout visio pages
            this.LayoutPages(OwlShapes.Page.LayoutType.Visio);

            
            //set 2D Data for all shapes whom contains them
            foreach (OwlShapes.Element shape in AllShapes.Values)
            {
                if (shape is OwlShapes.Shape && ((OwlShapes.Shape)shape).hasAny2DData())
                {
                    ((OwlShapes.Shape)shape).set2DData();
                }
            }

            //snap extensions to their state/subject
            foreach (Element snapable in AllShapes.Values)
            {
                if (snapable is Snapable)
                    ((Snapable)snapable).snap(AllShapes);
            }           

            //Get the spacing between the Shapes right
            this.LayoutPages(OwlShapes.Page.LayoutType.AutoArange);


            //As last step, add the Messages to the SIDPages
            foreach (Element shape in AllShapes.Values)
            {
                if(shape is SIDMessageConnector)
                    ((SIDMessageConnector)shape).addMessages();
            }
        }

        private void addShape( Dictionary<string,Element> shapes, String id, Element shape)
        {
            if (!shapes.ContainsKey(id))
            {
                shapes.Add(id, shape);
            }
        }

        private void ConnectObjects()
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
            /*
            foreach (OwlShapes.SBDPage page in this.SBDPages.Values)
            {
                page.ConnectOWLPass2(this.AllShapes);
            }
            */
        }

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
        }

        private void LayoutPages(OwlShapes.Page.LayoutType type)
        {
            foreach (OwlShapes.SBDPage page in this.SBDPages.Values)
            {
                page.Layout(type);
                //page.adjustSize();
            }
            foreach (OwlShapes.SIDPage page in this.pages.Values)
            {
                if (type == OwlShapes.Page.LayoutType.Addin)
                    type = OwlShapes.Page.LayoutType.Visio;
                page.Layout(type);

                //page.adjustSize();
            }
        }

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
        }
                    
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
        }
        
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
         
    }
}
