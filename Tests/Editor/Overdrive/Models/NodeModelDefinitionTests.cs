using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;
using UnityEngine;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Models
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    class NodeModelDefinitionTests
    {
        NodeModel m_Node;
        public void M1(int i) {}
        public int M3(int i, bool b) => 0;

        [Test]
        public void CallingDefineTwiceCreatesPortsOnce()
        {
            var asset = ScriptableObject.CreateInstance<TestGraphAssetModel>();
            asset.CreateGraph("asd", typeof(ClassStencil));
            var g = asset.GraphModel as TestGraphModel;

            m_Node = g.CreateNode<TestNodeModel>("test", Vector2.zero);
            Assert.That(m_Node.InputsById.Count, Is.EqualTo(1));

            m_Node.DefineNode();
            Assert.That(m_Node.InputsById.Count, Is.EqualTo(1));
        }

        [Test]
        public void CallingDefineTwiceCreatesOneEmbeddedConstant()
        {
            var asset = ScriptableObject.CreateInstance<TestGraphAssetModel>();
            asset.CreateGraph("asd", typeof(ClassStencil));
            var g = asset.GraphModel as TestGraphModel;

            m_Node = g.CreateNode<TestNodeModel>("test", Vector2.zero);
            Assert.That(m_Node.InputConstantsById.Count, Is.EqualTo(1));

            m_Node.DefineNode();
            Assert.That(m_Node.InputConstantsById.Count, Is.EqualTo(1));
        }

        [Test]
        public void NonAbstractNodeModels_AllHaveSerializableAttributes()
        {
            //Prepare
            var allNodeModelTypes = TypeCache.GetTypesDerivedFrom(typeof(NodeModel))
                .Where(t => !t.IsAbstract && !t.IsGenericType);
            var serializableNodeTypes = allNodeModelTypes.Where(t => t.GetCustomAttributes(typeof(SerializableAttribute)).Any());
            var serializableTypesLookup = new HashSet<Type>(serializableNodeTypes);

            //Act
            var invalidNodeModelTypes = new List<Type>();
            foreach (var nodeModelType in allNodeModelTypes)
            {
                if (!serializableTypesLookup.Contains(nodeModelType))
                    invalidNodeModelTypes.Add(nodeModelType);
            }

            //Validate
            if (invalidNodeModelTypes.Count > 0)
            {
                string errorMessage = "The following types don't have the required \"Serializable\" attribute:\n\n";
                StringBuilder builder = new StringBuilder(errorMessage);
                foreach (var invalidNodeType in invalidNodeModelTypes)
                    builder.AppendLine(invalidNodeType.ToString());
                Debug.LogError(builder.ToString());
            }

            Assert.That(invalidNodeModelTypes, Is.Empty);
        }

        [Serializable]
        public class TestNodeModelWithCustomPorts : NodeModel
        {
            public Func<IGTFPortModel> CreatePortFunc { get; set; }

            public Func<IGTFPortModel> CreatePort<T>(T value = default)
            {
                return () => AddDataInputPort(typeof(T).Name, defaultValue: value);
            }

            protected override void OnDefineNode()
            {
                CreatePortFunc?.Invoke();
            }
        }

        static IEnumerable<object[]> GetPortModelDefaultValueTestCases()
        {
            yield return PortValueTestCase<int>();
            yield return PortValueTestCase(42);
            yield return PortValueTestCase<KeyCode>();
            yield return PortValueTestCase(KeyCode.Escape);
        }

        [Test, TestCaseSource(nameof(GetPortModelDefaultValueTestCases))]
        public void PortModelsCanHaveDefaultValues(object expectedValue, Func<TestNodeModelWithCustomPorts, Func<IGTFPortModel>> createPort, Func<IConstant, object> getValue)
        {
            var asset = ScriptableObject.CreateInstance<TestGraphAssetModel>();
            asset.CreateGraph("asd", typeof(ClassStencil));
            var g = asset.GraphModel as TestGraphModel;

            m_Node = g.CreateNode<TestNodeModelWithCustomPorts>("test", Vector2.zero, preDefineSetup: ports => ports.CreatePortFunc = createPort(ports));
            Assert.That(getValue(m_Node.InputsByDisplayOrder.Single().EmbeddedValue), Is.EqualTo(expectedValue));
        }

        static object[] PortValueTestCase<T>(T value = default)
        {
            Func<IConstant, object> getCompareValue = c => c.ObjectValue;
            if (typeof(T).IsSubclassOf(typeof(Enum)))
            {
                getCompareValue = c => ((EnumConstant)c).EnumValue;
            }
            return new object[] { value, new Func<TestNodeModelWithCustomPorts, Func<IGTFPortModel>>(m => m.CreatePort(value)), getCompareValue };
        }
    }
}