#nullable enable
using System.Collections.Generic;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.NodeContainer
{
    /// <summary>
    ///     Creates and maintains a set of <see cref="Node"/>s.
    /// </summary>
    [RegisterComponent]
    public class NodeContainerComponent : Component, IExamine
    {
        public override string Name => "NodeContainer";

        [ViewVariables]
        public IReadOnlyList<Node> Nodes => _nodes;
        [DataField("nodes")]
        private List<Node> _nodes = new();
        [DataField("examinable")]
        private bool _examinable;

        public override void Initialize()
        {
            base.Initialize();
            foreach (var node in _nodes)
            {
                node.Initialize(Owner);
            }
        }

        protected override void Startup()
        {
            base.Startup();
            foreach (var node in _nodes)
            {
                node.OnContainerStartup();
            }
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case AnchoredChangedMessage:
                    AnchorUpdate();
                    break;
            }
        }

        protected override void Shutdown()
        {
            base.Shutdown();

            foreach (var node in _nodes)
            {
                node.OnContainerShutdown();
            }
        }

        private void AnchorUpdate()
        {
            foreach (var node in Nodes)
            {
                node.AnchorUpdate();
            }
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (!_examinable || !inDetailsRange) return;

            for (var i = 0; i < Nodes.Count; i++)
            {
                var node = Nodes[i];
                if (node == null) continue;
                switch (node.NodeGroupID)
                {
                    case NodeGroupID.HVPower:
                        message.AddMarkup(
                            Loc.GetString("It has a connector for [color=orange]HV cables[/color]."));
                        break;
                    case NodeGroupID.MVPower:
                        message.AddMarkup(
                            Loc.GetString("It has a connector for [color=yellow]MV cables[/color]."));
                        break;
                    case NodeGroupID.Apc:
                        message.AddMarkup(
                            Loc.GetString("It has a connector for [color=green]APC cables[/color]."));
                        break;
                }

                if(i != Nodes.Count - 1)
                    message.AddMarkup("\n");
            }
        }
    }
}
