using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    class SingleOutputNodeModel : IGTFNodeModel, IHasSingleOutputPort, IHasTitle, ICollapsible
    {
        public IGTFGraphModel GraphModel => null;
        public Vector2 Position { get; set; }

        public void Move(Vector2 delta)
        {
            Position += delta;
        }

        public bool IsDeletable => true;
        public bool IsDroppable => true;
        public bool IsCopiable => true;
        public string Title { get; set; }
        public string DisplayTitle => Title;

        PortModel m_Port = new PortModel { Direction = Direction.Output };
        public IGTFPortModel GTFOutputPort => m_Port;
        public IEnumerable<IGTFPortModel> Ports => new[] { GTFOutputPort };
        public bool Collapsed { get; set; }
    }
}
