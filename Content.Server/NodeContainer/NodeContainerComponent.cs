#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Examine;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.NodeContainer
{
    /// <summary>
    ///     Creates and maintains a set of <see cref="Node"/>s.
    /// </summary>
    [RegisterComponent]
    public class NodeContainerComponent : Component, IExamine
    {
        public override string Name => "NodeContainer";

        [DataField("nodes")] [ViewVariables] public Dictionary<string, Node> Nodes { get; } = new();

        [DataField("examinable")] private bool _examinable = false;

        public T GetNode<T>(string identifier) where T : Node
        {
            return (T) Nodes[identifier];
        }

        public bool TryGetNode<T>(string identifier, [NotNullWhen(true)] out T? node) where T : Node
        {
            if (Nodes.TryGetValue(identifier, out var n) && n is T t)
            {
                node = t;
                return true;
            }

            node = null;
            return false;
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (!_examinable || !inDetailsRange) return;

            foreach (var node in Nodes.Values)
            {
                if (node == null) continue;
                switch (node.NodeGroupID)
                {
                    case NodeGroupID.HVPower:
                        message.AddMarkup(
                            Loc.GetString("node-container-component-on-examine-details-hvpower") + "\n");
                        break;
                    case NodeGroupID.MVPower:
                        message.AddMarkup(
                            Loc.GetString("node-container-component-on-examine-details-mvpower") + "\n");
                        break;
                    case NodeGroupID.Apc:
                        message.AddMarkup(
                            Loc.GetString("node-container-component-on-examine-details-apc") + "\n");
                        break;
                }
            }
        }
    }
}
