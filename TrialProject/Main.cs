using alps.net.api;
using alps.net.api.parsing;
using System.Collections.Generic;
using alps.net.api.StandardPASS;
using System.Linq;
using System;
using alps.net.api.ALPS;
using System.Diagnostics;
using System.Reflection;
using VisioAddIn.OwlShapes;
using VisioAddIn;

namespace LibraryExample.DynamicImporterExample
{
    public class MainClass
    {
        public static void Main(string[] args)
        {

            // Needs to be called once
            // Now the reflective enumerator searches for classes in the library assembly as well as in the current.
            //ReflectiveEnumerator.addAssemblyToCheckForTypes(System.Reflection.Assembly.GetExecutingAssembly());
            ReflectiveEnumerator.addAssemblyToCheckForTypes(Assembly.GetExecutingAssembly());


            IPASSReaderWriter owlGraph = PASSReaderWriter.getInstance();

            // Set own factory as parsing factory to parse ontology classes to the right instances
            //owlGraph.setModelElementFactory(new BasicPASSProcessModelElementFactory());
            owlGraph.setModelElementFactory(new VisioAddIn.OwlShapes.VisioClassFactory());

            IList<string> paths = new List<string>
            {
               "C:\\Users\\qs0196\\source\\repos\\alps.net.api\\src\\standard_PASS_ont_v_1.1.0.owl",
               "C:\\Users\\qs0196\\source\\repos\\alps.net.api\\src\\ALPS_ont_v_0.8.0.owl",
            };

            // Load these files once (no future calls needed)
            // This call creates both parsing trees and the parsing dictionary
            owlGraph.loadOWLParsingStructure(paths);

            // This loads models from the specified owl.
            // Every owl instance of a FullySpecifiedSubject is parsed to an AdditionalFunctionalityFullySpecifiedSubject
            IList<IPASSProcessModel> models = owlGraph.loadModels(new List<string> { "C:\\Data\\ExportImportTest1.owl" });

            // IDictionary of all elements
            IDictionary<string, IPASSProcessModelElement> allElements = models[0].getAllElements();
            // Drop the keys, keep values
            ICollection<IPASSProcessModelElement> onlyElements = models[0].getAllElements().Values;
            // Filter for a specific interface (Enumerable, not so easy to use -> convert to list)
            IList<BasicPASSProcessModelElementFactory> onlyAdditionalFunctionalityElements = models[0].getAllElements().Values.OfType<BasicPASSProcessModelElementFactory>().ToList();


            //some output examples for a parsed model
            Console.WriteLine("Number ob Models loaded: " + models.Count);
            Console.WriteLine("Found " + onlyAdditionalFunctionalityElements.Count +
                              " AdditionalFunctionalityElements in First model!");

            IDictionary<string, IModelLayer> layers = models[0].getModelLayers();
            Console.WriteLine("Layers in first model: " + layers.Count);

            IModelLayer firstLayer = layers.ElementAt(0).Value;

            //IFullySpecifiedSubject mySubject = firstLayer.getFullySpecifiedSubject(0);
            //IDictionary<string, ISubjectBehavior> mySubjectBehaviors = mySubject.getBehaviors();
            //Console.WriteLine("Numbers of behaviors: " + mySubjectBehaviors.Count);

            //ISubjectBehavior firstBehavior = mySubjectBehaviors.ElementAt(0).Value;
            //Console.WriteLine("Numbers of Elements in Behavior: " + firstBehavior.getBehaviorDescribingComponents().Count);
            //Console.WriteLine("First Element: " + firstBehavior.getBehaviorDescribingComponents().ElementAt(0).Value.getModelComponentID());
            //IState firstState = firstBehavior.getInitialStateOfBehavior();


            //iterateStates(firstBehavior);

            IStandaloneMacroSubject mySMS = getASMSFromLayer(firstLayer);
            if (mySMS != null)
            {
                IMacroBehavior myMB = mySMS.getBehavior();
                Console.WriteLine("found behavior: " + (myMB != null));

                iterateStates(myMB);
            }

           
      

        }

        private static IStandaloneMacroSubject getASMSFromLayer(IModelLayer layer)
        {
            IStandaloneMacroSubject result = null;
            Console.WriteLine("Subjecs:");
            foreach (KeyValuePair<string, IPASSProcessModelElement> kvp in layer.getElements())
            {
                IPASSProcessModelElement myComponent = kvp.Value;

                if (myComponent is ISubject)
                {
                    Console.WriteLine("Subject: " + myComponent.getModelComponentID());
                    if (myComponent is IStandaloneMacroSubject)
                    {
                        result = (IStandaloneMacroSubject)myComponent;
                    }
                }
            }
            return result;
        }

        private static void iterateStates(ISubjectBehavior someBehavior)
        {
            Console.WriteLine("State Stats");

            foreach (KeyValuePair<string, IBehaviorDescribingComponent> kvp in someBehavior.getBehaviorDescribingComponents())
            {
                IPASSProcessModelElement myComponent = kvp.Value;
                if (myComponent is IState)
                {
                    Console.Write("state: " + myComponent.getModelComponentID());

                    IState myIstate = (IState)myComponent;

                    Console.Write(" - start: " + myIstate.isStateType(IState.StateType.InitialStateOfBehavior));
                    Console.WriteLine(" - end: " + myIstate.isStateType(IState.StateType.EndState));
                }
            }
        }
    }


}