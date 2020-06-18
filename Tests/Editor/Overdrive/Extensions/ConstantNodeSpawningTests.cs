using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Extensions
{
    internal class ConstantNodeSpawningTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;

        [Test(Description = "make sure basic types have a custom editor on the Graphtools side")]
        public void TestConstantEditorExtensionMethodsExistForBasicTypes()
        {
            Assert.That(GraphModel.NodeModels.Count, NUnit.Framework.Is.Zero);
            var expectedTypes = new[] { typeof(string), typeof(Boolean), typeof(Int32), typeof(Double), typeof(Single), typeof(Vector2), typeof(Vector3), typeof(Vector4), typeof(Quaternion), typeof(Color) };
            for (var i = 0; i < expectedTypes.Length; i++)
            {
                var type = expectedTypes[i];
                Type constantNodeType = GraphModel.Stencil.GetConstantNodeModelType(type);

                var constantExtMethod = ExtensionMethodCache<IConstantEditorBuilder>.GetExtensionMethod(constantNodeType, ConstantEditorBuilder.FilterMethods, ConstantEditorBuilder.KeySelector);
                Assert.That(constantExtMethod, NUnit.Framework.Is.Not.Null, $"No constant editor for {type.Name} / {constantNodeType.Name}");

                GraphModel.CreateConstantNode(constantNodeType.Name, type.GenerateTypeHandle(Stencil), 100f * i * Vector2.right);
            }
            Assert.That(GraphModel.NodeModels.OfType<ConstantNodeModel>().Count(), NUnit.Framework.Is.EqualTo(expectedTypes.Length));
        }
    }
}
