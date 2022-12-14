using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using VisioAddIn.OwlShapes.util;

namespace TestProject
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void test()
        {
            IGraphNode<string> graphNodea = new DirectedGraphNode<string>("a", null);
            IGraphNode<string> graphNodeb = new DirectedGraphNode<string>("b", new List<IGraphNode<string>> { graphNodea });
            IGraphNode<string> graphNodec = new DirectedGraphNode<string>("c", new List<IGraphNode<string>> { graphNodeb });
            IGraphNode<string> graphNoded = new DirectedGraphNode<string>("d", new List<IGraphNode<string>> { graphNodea });
            IGraphNode<string> graphNodee = new DirectedGraphNode<string>("e", new List<IGraphNode<string>> { graphNoded });
            IGraphNode<string> graphNodef = new DirectedGraphNode<string>("f", new List<IGraphNode<string>> { graphNodee });
            graphNodea.setInputNodes(new List<IGraphNode<string>> { graphNodec, graphNodef });
            Assert.IsTrue(graphNodea.getHeigthToLastLeaf() == 3);
        }
    }
}