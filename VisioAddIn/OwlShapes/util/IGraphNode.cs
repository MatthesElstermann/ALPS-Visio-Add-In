using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisioAddIn.OwlShapes.util
{
    public interface IGraphNode<T>
    {
        /// <summary>
        /// Sets the parent node
        /// </summary>
        /// <param name="parent">the parent node</param>
        void addInputNode(IGraphNode<T> inputNode);

        /// <summary>
        /// Creates a directed acyclic subgraph that starts from this vertice.
        /// Cuts out all loops.
        /// </summary>
        /// <param name="allNodes"></param>
        /// <returns></returns>
        IGraphNode<T> createDASubgraph( IDictionary<string, IGraphNode<T>> allNodes = null);

        void setInputNodes(IList<IGraphNode<T>> inputNodes);

        /// <summary>
        /// Returns the parent node
        /// </summary>
        /// <returns>the parent node</returns>
        IList<IGraphNode<T>> getInputNodes();

        /// <summary>
        /// Overrides the current child nodes with a list of new child nodes
        /// </summary>
        /// <param name="childNodes">the new child nodes</param>
        void setOutputNodes(IList<IGraphNode<T>> outputNodes);

        /// <summary>
        /// Adds a child to the list of child nodes
        /// </summary>
        /// <param name="child">the node of the child</param>
        void addOutputNode(IGraphNode<T> outputNode);

        string getID();

        /// <summary>
        /// Returns the child nodes
        /// </summary>
        /// <returns>the child nodes</returns>
        IList<IGraphNode<T>> getOutputNodes();

        IGraphNode<T> getOutputNode(int index);

        /// <summary>
        /// Sets the content of the node
        /// </summary>
        /// <param name="content">the content</param>
        void setContent(T content);

        /// <summary>
        /// Returns the content of the node
        /// </summary>
        /// <returns>the content of the node</returns>
        T getContent();

        /// <summary>
        /// Checks whether the node contains a given string as content
        /// </summary>
        /// <param name="content">the string that will be checked as reference</param>
        /// <returns>true if the string equals the content, false if not</returns>
        bool containsContent(T compare, out IGraphNode<T> node);


        /// <summary>
        /// Returns the root node of the current tree node
        /// </summary>
        /// <returns>the root node</returns>
        IGraphNode<T> getRoot();

        /// <summary>
        /// Returns the height of the longest path to a leaf starting from this node.
        /// If this node is already a leaf, it returns 0
        /// </summary>
        /// <returns></returns>
        int getHeigthToLastLeaf(bool eliminateCycles = true);

        IGraphNode<T> copy(bool copyConnections = true);

    }
}
