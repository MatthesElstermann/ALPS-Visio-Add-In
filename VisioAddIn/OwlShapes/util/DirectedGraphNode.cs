using System;
using System.Collections.Generic;

namespace VisioAddIn.OwlShapes.util
{
    public class DirectedGraphNode<T> : IGraphNode<T> where T : class
    {
        protected readonly IList<IGraphNode<T>> incomingNodes = new List<IGraphNode<T>>();
        protected readonly IList<IGraphNode<T>> outgoingNodes = new List<IGraphNode<T>>();
        T content;
        readonly string guid;


        public IGraphNode<T> createDASubgraph(IDictionary<string, IGraphNode<T>> allNodes = null)
        {
            if (allNodes == null || allNodes.Count == 0)
            {
                allNodes = new Dictionary<string, IGraphNode<T>>();
            }

            if (allNodes.ContainsKey(getID())) return null;

            IGraphNode<T> copyNode = copy(false);

            allNodes.Add(copyNode.getID(), copyNode);
            foreach (IGraphNode<T> child in outgoingNodes)
            {
                IGraphNode<T> childCopy = child.createDASubgraph(allNodes);
                if (childCopy != null)
                    childCopy.addInputNode(copyNode);
            }
            return copyNode;
        }


        public DirectedGraphNode(string guid)
        {
            this.guid = guid;
        }

        public DirectedGraphNode(T content = null, IList<IGraphNode<T>> incomingNodes = null, IList<IGraphNode<T>> outgoingNodes = null)
        {
            guid = Guid.NewGuid().ToString();
            setInputNodes(incomingNodes);
            setOutputNodes(outgoingNodes);
            setContent(content);
        }

        public string getID()
        {
            return guid;
        }

        public void addInputNode(IGraphNode<T> inputNode)
        {
            if (inputNode == null || incomingNodes.Contains(inputNode)) return;
            incomingNodes.Add(inputNode);
            inputNode.addOutputNode(this);
        }

        public void addOutputNode(IGraphNode<T> outputNode)
        {
            if (outputNode == null || outgoingNodes.Contains(outputNode)) return;
            outgoingNodes.Add(outputNode);
            outputNode.addInputNode(this);
        }

        public bool containsContent(T compare, out IGraphNode<T> node)
        {
            bool test = false;
            node = null;

            foreach (IGraphNode<T> t in outgoingNodes)
            {
                if (t.getContent().Equals(content))
                {
                    test = true;
                    node = this;
                    break;
                }
                else
                {
                    test = t.containsContent(content, out node);
                    if (test) break;
                }
            }

            return test;
        }

        public T getContent()
        {
            return content;
        }

        public int getHeigthToLastLeaf(bool eliminateCycles = true)
        {
            if (eliminateCycles)
            {
                IGraphNode<T> acyclicEquivalent = createDASubgraph();
                return acyclicEquivalent.getHeigthToLastLeaf(false);
            }
            else
            {
                if (getOutputNodes().Count == 0)
                    return 0;
                int height = 0;
                foreach (IGraphNode<T> child in getOutputNodes())
                {
                    height = Math.Max(height, child.getHeigthToLastLeaf(false));
                }
                return height + 1;
            }
        }

        public IList<IGraphNode<T>> getInputNodes()
        {
            return incomingNodes;
        }

        public IGraphNode<T> getOutputNode(int index)
        {
            return outgoingNodes[index];
        }

        public IList<IGraphNode<T>> getOutputNodes()
        {
            return outgoingNodes;
        }

        public IGraphNode<T> getRoot()
        {
            throw new NotImplementedException();
        }

        public void setContent(T content)
        {
            this.content = content;
        }

        public void setInputNodes(IList<IGraphNode<T>> inputNodes)
        {
            this.incomingNodes.Clear();
            if (inputNodes != null)
            {
                foreach(IGraphNode<T> node in inputNodes)
                    this.addInputNode(node);
            }
        }

        public void setOutputNodes(IList<IGraphNode<T>> outputNodes)
        {
            this.outgoingNodes.Clear();
            if (outputNodes != null)
            {
                foreach (IGraphNode<T> node in outputNodes)
                    this.addOutputNode(node);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is IGraphNode<T> node)
                return node.getID().Equals(getID());
            return false;
        }

        public override int GetHashCode()
        {
            return getID().GetHashCode();
        }

        public IGraphNode<T> copy(bool copyConnections = true)
        {
            IGraphNode<T> node = new DirectedGraphNode<T>(getID());
            node.setContent(getContent());
            if (copyConnections)
            {
                node.setInputNodes(getInputNodes());
                node.setOutputNodes(getOutputNodes());
            }
            return node;
        }
    }
}
