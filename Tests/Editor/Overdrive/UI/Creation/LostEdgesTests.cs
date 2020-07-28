using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScriptingTests.UI
{
    class LostEdgesTests : BaseUIFixture
    {
        protected override bool CreateGraphOnStartup => true;

        [UnityTest]
        public IEnumerator LostEdgesAreDrawn()
        {
            var operatorModel = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-100, -100));
            IGTFConstantNodeModel intModel = GraphModel.CreateConstantNode("int", typeof(int).GenerateTypeHandle(), new Vector2(-150, -100));
            var edge = (EdgeModel)GraphModel.CreateEdge(operatorModel.Input0, intModel.OutputPort);

            // simulate a renamed port by changing the edge's port id

            var field = typeof(EdgeModel).GetField("m_ToPortReference", BindingFlags.Instance | BindingFlags.NonPublic);
            var inputPortReference = (PortReference)field.GetValue(edge);
            inputPortReference.UniqueId = "asd";
            field.SetValue(edge, inputPortReference);

            edge.ResetPorts(); // get rid of cached port models

            Store.ForceRefreshUI(UpdateFlags.All);
            yield return null;

            var lostPortsAdded = GraphView.Query(className: "ge-port--data-type-missing-port").Build().ToList().Count;
            Assert.AreEqual(1, lostPortsAdded);
        }
    }
}