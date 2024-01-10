using System;
using System.Collections.Generic;
using System.Linq;
using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Microsoft.Office.Interop.Visio;
using VisioAddIn.OwlShapes.util;
using System.Diagnostics;

namespace VisioAddIn.OwlShapes
{
    public class VisioMacroBehavor : MacroBehavior, IVisioExportableWithShape
    {
        private const string className = "VisioMacroBehavior";
        private Simple2DPosParser parser;

        protected VisioMacroBehavor()
        {
        }

        public VisioMacroBehavor(IModelLayer layer, string labelForID = null, ISubject subject = null,
            ISet<IBehaviorDescribingComponent> behaviorDescribingComponents = null, ISet<IStateReference> stateReferences = null,
            IState initialStateOfBehavior = null,
            int priorityNumber = 0, string comment = null, string additionalLabel = null,
            IList<IIncompleteTriple> additionalAttribute = null)
            : base(layer, labelForID, subject, behaviorDescribingComponents, stateReferences, initialStateOfBehavior, priorityNumber,
                comment,
                additionalLabel, additionalAttribute)
        {
        }

        public void exportToVisio(Page currentPage, ISimple2DVisualizationBounds bounds = null)
        {
            foreach (var state in behaviorDescriptionComponents.Values.OfType<IState>())
            {
                Debug.WriteLine(" State in MB: " + state.getModelComponentID() + " - is exportable: " + (state is IVisioExportable));
                if (state is IVisioExportable exportable)
                    exportable.exportToVisio(currentPage);
            }

            foreach (var transition in behaviorDescriptionComponents.Values.OfType<ITransition>())
                if (transition is IVisioExportable exportable)
                    exportable.exportToVisio(currentPage);

            IList<IState> possibleFirstStates = new List<IState>();
            IDictionary<string, IGraphNode<IPASSProcessModelElement>> allCreatedNodes =
                new Dictionary<string, IGraphNode<IPASSProcessModelElement>>();

            // Search all the first states
            foreach (var state in getBehaviorDescribingComponents().Values.OfType<IState>())
                if (state.getIncomingTransitions().Count == 0)
                {
                    possibleFirstStates.Add(state);

                    // Create a tree node wrapping the state, add it to known states (used for building the tree)
                    IGraphNode<IPASSProcessModelElement> createdNode =
                        new DirectedGraphNode<IPASSProcessModelElement>(state);
                    allCreatedNodes.Add(state.getModelComponentID(), createdNode);
                }

            foreach (var state in possibleFirstStates) buildTree(state, allCreatedNodes);
            if (possibleFirstStates.Count > 0)
                exportTree(allCreatedNodes[possibleFirstStates[0].getModelComponentID()], currentPage);
        }

        public Shape getShape()
        {
            throw new NotImplementedException();
        }

        public void setShape(Shape shape)
        {
            throw new NotImplementedException();
        }

        private void exportTree(IGraphNode<IPASSProcessModelElement> rootNode, Page currentPage)
        {
            double pageWidth = currentPage.PageSheet.CellsU["PageWidth"].Result[""];
            double pageHeight = currentPage.PageSheet.CellsU["PageHeight"].Result[""];
            __exportTree(rootNode, currentPage, 25, pageHeight - 25);
        }

        private double __exportTree(IGraphNode<IPASSProcessModelElement> rootNode, Page currentPage, double xpos, double ypos)
        {
            if (rootNode.getContent() is IVisioExportableWithShape ex)
            {
                var exportedShape = ex.getShape();
                exportedShape.CellsU["PinX"].Result[""] = xpos;
                exportedShape.CellsU["PinY"].Result[""] = ypos;

                var newX = xpos + 70;
                var newY = ypos;

                foreach (var childNode in rootNode.getOutputNodes())
                    newY = __exportTree(childNode, currentPage, newX, newY) - 40;

                if (rootNode.getOutputNodes().Count > 0)
                    newY += 40;
                return newY;
            }

            return ypos;
        }

        private void buildTree(IState state, IDictionary<string, IGraphNode<IPASSProcessModelElement>> allCreatedNodes)
        {
            IGraphNode<IPASSProcessModelElement> originNode = allCreatedNodes[state.getModelComponentID()];
            foreach (var outgoing in state.getOutgoingTransitions().Values)
            {
                var targetState = outgoing.getTargetState();
                if (targetState != null)
                {
                    // Node is already somewhere in the tree
                    if (allCreatedNodes.ContainsKey(targetState.getModelComponentID()))
                    {
                        // originNode.addOutputNode(allCreatedNodes[targetState.getModelComponentID()]);
                    }

                    // Node is not in the tree --> create it, build tree recusively
                    else
                    {
                        IGraphNode<IPASSProcessModelElement> outputNode =
                            new DirectedGraphNode<IPASSProcessModelElement>(targetState);
                        originNode.addOutputNode(outputNode);
                        allCreatedNodes.Add(targetState.getModelComponentID(), outputNode);
                        buildTree(targetState, allCreatedNodes);
                    }
                }
            }
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioMacroBehavor();
        }


        protected override bool parseAttribute(string predicate, string objectContent, string lang, string dataType,
            IParseablePASSProcessModelElement element)
        {
            if (parser is null) parser = new Simple2DPosParser(this);
            if (!parser.parseAttribute(predicate, objectContent, lang, dataType, element))
                return base.parseAttribute(predicate, objectContent, lang, dataType, element);
            return true;
        }

    }
}