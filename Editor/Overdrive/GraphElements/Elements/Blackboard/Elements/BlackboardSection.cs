using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class BlackboardSection : VisualElement
    {
        public static readonly string ussClassName = "ge-blackboard-section";
        public static readonly string headerUssClassName = ussClassName.WithUssElement("header");
        public static readonly string titleLabelUssClassName = ussClassName.WithUssElement("title");
        public static readonly string addButtonUssClassName = ussClassName.WithUssElement("add");
        public static readonly string rowsUssClassName = ussClassName.WithUssElement("rows");
        public static readonly string dragIndicatorUssClassName = ussClassName.WithUssElement("drag-indicator");

        VisualElement m_DragIndicator;
        VisualElement m_RowsContainer;

        int m_InsertIndex;
        Blackboard m_Blackboard;
        Button m_AddButton;

        int InsertionIndex(Vector2 pos)
        {
            int index = -1;
            VisualElement owner = contentContainer ?? this;
            Vector2 localPos = this.ChangeCoordinatesTo(owner, pos);

            if (owner.ContainsPoint(localPos))
            {
                index = 0;

                foreach (VisualElement child in Children())
                {
                    Rect rect = child.layout;

                    if (localPos.y > (rect.y + rect.height / 2))
                    {
                        ++index;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return index;
        }

        public BlackboardSection(Blackboard blackboard, string name)
        {
            m_Blackboard = blackboard;
            m_InsertIndex = -1;

            RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
            RegisterCallback<DragLeaveEvent>(OnDragLeaveEvent);

            var header = new VisualElement() { name = "section-header" };
            header.AddToClassList(headerUssClassName);

            var titleLabel = new Label(name) { name = "section-title-label" };
            titleLabel.AddToClassList(titleLabelUssClassName);
            header.Add(titleLabel);

            m_AddButton = new Button(() =>
            {
                GenericMenu menu = new GenericMenu();
                (m_Blackboard.Model as IBlackboardGraphModel)?.PopulateCreateMenu(name, menu, m_Blackboard.CommandDispatcher);
                Vector2 menuPosition = new Vector2(m_AddButton.layout.xMin, m_AddButton.layout.yMax);
                menuPosition = m_AddButton.parent.LocalToWorld(menuPosition);
                menu.DropDown(new Rect(menuPosition, Vector2.zero));
            }) { text = "+" };
            m_AddButton.AddToClassList(addButtonUssClassName);
            header.Add(m_AddButton);

            hierarchy.Add(header);

            m_RowsContainer = new VisualElement() { name = "rows-container" };
            m_RowsContainer.AddToClassList(rowsUssClassName);
            hierarchy.Add(m_RowsContainer);

            m_DragIndicator = new VisualElement { name = "drag-indicator" };
            m_DragIndicator.AddToClassList(dragIndicatorUssClassName);
            hierarchy.Add(m_DragIndicator);

            AddToClassList(ussClassName);
        }

        public override VisualElement contentContainer => m_RowsContainer;

        void HideDragIndicator()
        {
            m_DragIndicator.style.visibility = Visibility.Hidden;
        }

        void ShowDragIndicator(float yPosition)
        {
            m_DragIndicator.style.visibility = Visibility.Visible;
            m_DragIndicator.style.left = 0;
            m_DragIndicator.style.top = yPosition - m_DragIndicator.resolvedStyle.height / 2;
            m_DragIndicator.style.width = layout.width;
        }

        bool CanAcceptDrop(List<ISelectableGraphElement> draggedObjects)
        {
            return draggedObjects?.OfType<BlackboardField>().Any(Contains) ?? false;
        }

        void OnDragUpdatedEvent(DragUpdatedEvent evt)
        {
            var draggedObjects = DragAndDrop.GetGenericData("DragSelection") as List<ISelectableGraphElement>;

            if (!CanAcceptDrop(draggedObjects))
            {
                HideDragIndicator();
                return;
            }

            m_InsertIndex = InsertionIndex(evt.localMousePosition);

            if (m_InsertIndex != -1)
            {
                float indicatorY;

                if (m_InsertIndex == childCount)
                {
                    VisualElement lastChild = this[m_InsertIndex - 1];

                    indicatorY = lastChild.ChangeCoordinatesTo(this,
                        new Vector2(0, lastChild.layout.height + lastChild.resolvedStyle.marginBottom)).y;
                }
                else
                {
                    VisualElement childAtInsertIndex = this[m_InsertIndex];

                    indicatorY = childAtInsertIndex.ChangeCoordinatesTo(this,
                        new Vector2(0, -childAtInsertIndex.resolvedStyle.marginTop)).y;
                }

                ShowDragIndicator(indicatorY);
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
            }
            else
            {
                HideDragIndicator();
            }

            evt.StopPropagation();
        }

        void OnDragPerformEvent(DragPerformEvent evt)
        {
            var selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectableGraphElement>;

            if (selection != null && CanAcceptDrop(selection) && m_InsertIndex != -1)
            {
                OnItemDropped(m_InsertIndex, selection);
            }

            HideDragIndicator();
            evt.StopPropagation();
        }

        void OnItemDropped(int index, IEnumerable<ISelectableGraphElement> elements)
        {
            var droppedRows = elements.OfType<BlackboardField>().Where(Contains).ToList();
            if (!droppedRows.Any())
                return;

            var models = droppedRows.Select(r => r.Model as IVariableDeclarationModel);
            IVariableDeclarationModel insertAfterModel = null;

            if (index >= childCount)
                insertAfterModel = (this[childCount - 1] as BlackboardRow)?.Model as IVariableDeclarationModel;
            else if (index > 0)
                insertAfterModel = (this[index - 1] as BlackboardRow)?.Model as IVariableDeclarationModel;

            m_Blackboard.CommandDispatcher.Dispatch(new ReorderGraphVariableDeclarationCommand(models, insertAfterModel));
        }

        void OnDragLeaveEvent(DragLeaveEvent evt)
        {
            HideDragIndicator();
        }
    }
}