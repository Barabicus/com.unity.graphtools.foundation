using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.Model", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class EnumConstantNodeModel : ConstantNodeModel<EnumValueReference>
    {
        public override string Title => value.Value.ToString();

        public override Type Type => EnumType.Resolve(Stencil);

        protected override EnumValueReference DefaultValue => new EnumValueReference(EnumType);

        public override void PredefineSetup(TypeHandle constantTypeHandle)
        {
            value = new EnumValueReference(constantTypeHandle);
        }

        protected override void OnDefineNode()
        {
            if (!value.IsValid(Stencil))
                value = new EnumValueReference(typeof(KeyCode).GenerateTypeHandle(Stencil));
            base.OnDefineNode();
        }

        protected override void SetFromOther(object o)
        {
            value.Value = Convert.ToInt32(o);
        }

        public Enum EnumValue => value.ValueAsEnum(Stencil);

        public TypeHandle EnumType => value.EnumType;
    }
}
