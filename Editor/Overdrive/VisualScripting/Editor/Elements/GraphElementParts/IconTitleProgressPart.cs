using System;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.UIElements;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class IconTitleProgressPart : EditableTitlePart
    {
        public ProgressBar CoroutineProgressBar;

        static readonly string k_UssClassName = "ge-icon-title-progress";

        public static readonly string k_CollapseButtonPartName = "collapse-button";

        public static IconTitleProgressPart Create(string name, IGTFGraphElementModel model, IGraphElement graphElement, string parentClassName)
        {
            if (model is NodeModel)
            {
                return new IconTitleProgressPart(name, model, graphElement, parentClassName);
            }

            return null;
        }

        protected IconTitleProgressPart(string name, IGTFGraphElementModel model, IGraphElement ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName, multiline: false)
        {
            if (model is ICollapsible)
            {
                var collapseButtonPart = NodeCollapseButtonPart.Create(k_CollapseButtonPartName, model, ownerElement, k_UssClassName);
                PartList.AppendPart(collapseButtonPart);
            }
        }

        VisualElement m_Root;
        public override VisualElement Root => m_Root;

        protected override void BuildPartUI(VisualElement container)
        {
            var nodeModel = m_Model as NodeModel;

            m_Root = new VisualElement { name = PartName };
            m_Root.AddToClassList(k_UssClassName);
            m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            TitleContainer = new VisualElement();
            TitleContainer.AddToClassList(k_UssClassName.WithUssElement("title-container"));
            TitleContainer.AddToClassList(m_ParentClassName.WithUssElement("title-container"));
            m_Root.Add(TitleContainer);

            var icon = new VisualElement();
            icon.AddToClassList(k_UssClassName.WithUssElement("icon"));
            icon.AddToClassList(m_ParentClassName.WithUssElement("icon"));
            icon.AddToClassList(nodeModel.IconTypeString);
            TitleContainer.Add(icon);

            bool isRenamable = m_Model is UnityEditor.GraphToolsFoundation.Overdrive.Model.IRenamable renamable && renamable.IsRenamable;

            if (isRenamable)
            {
                TitleLabel = new EditableLabel { name = k_TitleLabelName };
                TitleLabel.RegisterCallback<ChangeEvent<string>>(OnRename);
            }
            else
            {
                TitleLabel = new Label { name = k_TitleLabelName };
            }

            TitleLabel.AddToClassList(k_UssClassName.WithUssElement("title"));
            TitleLabel.AddToClassList(m_ParentClassName.WithUssElement("title"));
            TitleContainer.Add(TitleLabel);

            if (nodeModel.HasProgress)
            {
                CoroutineProgressBar = new ProgressBar();
                CoroutineProgressBar.AddToClassList(k_UssClassName.WithUssElement("progress-bar"));
                CoroutineProgressBar.AddToClassList(m_ParentClassName.WithUssElement("progress-bar"));
                TitleContainer.Add(CoroutineProgressBar);
            }

            container.Add(m_Root);
        }

        protected override void PostBuildPartUI()
        {
            base.PostBuildPartUI();
            m_Root.AddStylesheet("IconTitleProgressPart.uss");
        }

        protected override void UpdatePartFromModel()
        {
            base.UpdatePartFromModel();

            var nodeModel = m_Model as NodeModel;
            if (nodeModel == null)
                return;

            CoroutineProgressBar?.EnableInClassList("hidden", !nodeModel.HasProgress);

            if (nodeModel.HasUserColor)
            {
                m_Root.style.backgroundColor = nodeModel.Color;
            }
            else
            {
                m_Root.style.backgroundColor = StyleKeyword.Null;
            }
        }
    }
}
