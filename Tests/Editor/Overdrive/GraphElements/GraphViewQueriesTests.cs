using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements.Utilities;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    public class GraphViewQueriesTests : GraphViewTester
    {
        BasicNodeModel m_Node1;
        BasicNodeModel m_Node2;
        BasicNodeModel m_Node3;
        BasicNodeModel m_Node4;
        BasicEdgeModel m_Edge1;
        BasicEdgeModel m_Edge2;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_Node1 = CreateNode("Node 1", new Vector2(100, 100), 2, 2);
            m_Node2 = CreateNode("Node 2", new Vector2(200, 200), 2, 2);
            m_Node3 = CreateNode("Node 3", new Vector2(400, 400));
            m_Node4 = CreateNode("Node 4", new Vector2(500, 500));
            m_Edge1 = GraphModel.CreateEdgeGTF(m_Node1.InputPorts.First(), m_Node2.OutputPorts.First()) as BasicEdgeModel;
            m_Edge2 = GraphModel.CreateEdgeGTF(m_Node1.InputPorts.First(), m_Node2.OutputPorts.ElementAt(1)) as BasicEdgeModel;
        }

        IEnumerable<GraphElement> GetElements<T>() where T : GraphElement
        {
            GraphElement e = m_Node1.GetUI<T>(graphView);
            if (e != null) yield return e;

            e = m_Node2.GetUI<T>(graphView);
            if (e != null) yield return e;

            e = m_Node3.GetUI<T>(graphView);
            if (e != null) yield return e;

            e = m_Node4.GetUI<T>(graphView);
            if (e != null) yield return e;

            e = m_Edge1.GetUI<T>(graphView);
            if (e != null) yield return e;

            e = m_Edge2.GetUI<T>(graphView);
            if (e != null) yield return e;
        }

        [Test]
        public void QueryAllElements()
        {
            graphView.RebuildUI(GraphModel, Store);
            List<GraphElement> allElements = graphView.graphElements.ToList();

            Assert.AreEqual(6, allElements.Count);
            Assert.IsFalse(allElements.OfType<Port>().Any());

            foreach (var e in GetElements<GraphElement>())
            {
                Assert.IsTrue(allElements.Contains(e));
            }
        }

        [Test]
        public void QueryAllNodes()
        {
            graphView.RebuildUI(GraphModel, Store);
            List<Node> allNodes = graphView.nodes.ToList();

            Assert.AreEqual(4, allNodes.Count);

            foreach (var e in GetElements<Node>())
            {
                Assert.IsTrue(allNodes.Contains(e));
            }
        }

        [Test]
        public void QueryAllEdges()
        {
            graphView.RebuildUI(GraphModel, Store);
            List<Edge> allEdges = graphView.edges.ToList();

            Assert.AreEqual(2, allEdges.Count);

            foreach (var e in GetElements<Edge>())
            {
                Assert.IsTrue(allEdges.Contains(e));
            }
        }

        [Test]
        public void QueryAllPorts()
        {
            graphView.RebuildUI(GraphModel, Store);
            Assert.AreEqual(8, graphView.ports.ToList().Count);
        }
    }
}
