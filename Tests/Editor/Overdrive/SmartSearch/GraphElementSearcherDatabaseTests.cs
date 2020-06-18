using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive.VisualScripting;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.SmartSearch
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    static class GraphElementSearcherDatabaseTestsExtensions
    {
        public static bool DoNothing1(this GraphElementSearcherDatabaseTests o) { return true; }
        internal static int DoNothing2(this GraphElementSearcherDatabaseTests o) { return 0; }
        public static void DoNothing3(this GraphElementSearcherDatabaseTests o) {}
    }

    class GraphElementSearcherDatabaseTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;

#pragma warning disable CS0414
#pragma warning disable CS0649
        [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
        sealed class FakeObject
        {
            public const string Value = "";
            public static readonly string Blah = "";
            public readonly string Details = "";
            public float Num;

            [Obsolete]
            public string ObjName => "FakeObject";

            public int Hash => 42;

            public static bool IsActive { get; set; }
            public static int Zero => 0;

            public int this[int index] => index + 1;

            public FakeObject() {}
            public FakeObject(int i) {}

            public string Name => "FakeObject";
            public float GetFloat() { return 1f; }
            public void Foo() {}
        }
#pragma warning restore CS0649
#pragma warning restore CS0414

        void CreateNodesAndValidateGraphModel(GraphNodeModelSearcherItem item, SpawnFlags mode,
            Action<List<INodeModel>> assertNodesCreation)
        {
            var initialNodes = GraphModel.NodeModels.ToList();
            var initialEdges = GraphModel.EdgeModels.ToList();

            item.CreateElements.Invoke(new GraphNodeCreationData(GraphModel, Vector2.zero, mode));

            // If nodes are created as Orphan, graphModel should not be modified
            if (mode.IsOrphan())
            {
                CollectionAssert.AreEqual(initialNodes, GraphModel.NodeModels);
                CollectionAssert.AreEqual(initialEdges, GraphModel.EdgeModels);
                return;
            }

            assertNodesCreation.Invoke(initialNodes);
        }

        [TestCase(SpawnFlags.Default)]
        [TestCase(SpawnFlags.Orphan)]
        public void TestGraphVariables(SpawnFlags mode)
        {
            const string name = "int";
            var var1 = GraphModel.CreateGraphVariableDeclaration(name,
                typeof(int).GenerateTypeHandle(Stencil), false);

            var db = new GraphElementSearcherDatabase(Stencil)
                .AddGraphVariables(GraphModel)
                .Build();

            var results = db.Search("i", out _);
            Assert.AreEqual(1, results.Count);

            var item = (GraphNodeModelSearcherItem)results[0];
            var data = (TypeSearcherItemData)item.Data;
            Assert.AreEqual(var1.DataType, data.Type);

            CreateNodesAndValidateGraphModel(item, mode, initialNodes =>
            {
                var node = GraphModel.NodeModels.OfType<VariableNodeModel>().FirstOrDefault();
                Assert.IsNotNull(node);
                Assert.AreEqual(initialNodes.Count + 1, GraphModel.NodeModels.Count);
                Assert.AreEqual(name.Nicify(), node.Title);
                Assert.AreEqual(typeof(int), node.DataType.Resolve(Stencil));
            });
        }

        [TestCase(SearcherContext.Graph, "sti", CommonSearcherTags.StickyNote)]
        public void TestSingleItem(SearcherContext context, string query, CommonSearcherTags expectedTag)
        {
            var db = new GraphElementSearcherDatabase(Stencil)
                .AddStickyNote()
                .Build();

            var results = db.Search(query, out _);

            if (context == SearcherContext.Graph)
            {
                Assert.That(results[0], Is.TypeOf<GraphNodeModelSearcherItem>());
                var item = (GraphNodeModelSearcherItem)results[0];
                Assert.That(item.Data, Is.TypeOf<TagSearcherItemData>());
                var tag = (TagSearcherItemData)item.Data;
                Assert.That(tag.Tag, Is.EqualTo(expectedTag));
            }
        }

        [Test]
        public void TestConstants()
        {
            var db = new GraphElementSearcherDatabase(Stencil)
                .AddConstants(new[] { typeof(string) })
                .Build();

            var results = db.Search("st", out _);
            Assert.AreEqual(1, results.Count);

            var item = results[0] as GraphNodeModelSearcherItem;
            Assert.IsNotNull(item);

            var data = (TypeSearcherItemData)item.Data;
            Assert.AreEqual(typeof(string).GenerateTypeHandle(Stencil), data.Type);
        }
    }
}
