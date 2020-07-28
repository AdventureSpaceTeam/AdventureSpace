﻿using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Robust.Server.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Robust.Shared.GameObjects.Components;

namespace Content.Server.GameObjects.Components.NodeContainer.Nodes
{
    /// <summary>
    ///     Organizes themselves into distinct <see cref="INodeGroup"/>s with other <see cref="Node"/>s
    ///     that they can "reach" and have the same <see cref="Node.NodeGroupID"/>.
    /// </summary>
    public abstract class Node
    {
        /// <summary>
        ///     An ID used as a criteria for combining into groups. Determines which <see cref="INodeGroup"/>
        ///     implementation is used as a group, detailed in <see cref="INodeGroupFactory"/>.
        /// </summary>
        [ViewVariables]
        public NodeGroupID NodeGroupID { get; private set; }

        [ViewVariables]
        public INodeGroup NodeGroup { get => _nodeGroup; set => SetNodeGroup(value); }
        private INodeGroup _nodeGroup = BaseNodeGroup.NullGroup;

        [ViewVariables]
        public IEntity Owner { get; private set; }

        [ViewVariables]
        private bool _needsGroup = true;

        /// <summary>
        ///     If this node should be considered for connection by other nodes.
        /// </summary>
        private bool Connectable => !_deleting && Anchored;

        private bool Anchored => !Owner.TryGetComponent<ICollidableComponent>(out var collidable) || collidable.Anchored;

        /// <summary>
        ///    Prevents a node from being used by other nodes while midway through removal.
        /// </summary>
        private bool _deleting = false;

#pragma warning disable 649
        [Dependency] private readonly INodeGroupFactory _nodeGroupFactory;
#pragma warning restore 649

        public void Initialize(NodeGroupID nodeGroupID, IEntity owner)
        {
            NodeGroupID = nodeGroupID;
            Owner = owner;
        }

        public void OnContainerInitialize()
        {
            TryAssignGroupIfNeeded();
            CombineGroupWithReachable();
            if (Owner.TryGetComponent<ICollidableComponent>(out var collidable))
            {
                AnchorUpdate();
                collidable.AnchoredChanged += AnchorUpdate;
            }
        }

        public void OnContainerRemove()
        {
            _deleting = true;
            if (Owner.TryGetComponent<ICollidableComponent>(out var collidable))
            {
                collidable.AnchoredChanged -= AnchorUpdate;
            }
            NodeGroup.RemoveNode(this);
        }

        public bool TryAssignGroupIfNeeded()
        {
            if (!_needsGroup)
            {
                return false;
            }
            NodeGroup = GetReachableCompatibleGroups().FirstOrDefault() ?? MakeNewGroup();
            return true;
        }

        public void SpreadGroup()
        {
            Debug.Assert(!_needsGroup);
            foreach (var node in GetReachableCompatibleNodes().Where(node => node._needsGroup))
            {
                node.NodeGroup = NodeGroup;
                node.SpreadGroup();
            }
        }

        public void ClearNodeGroup()
        {
            _nodeGroup = BaseNodeGroup.NullGroup;
            _needsGroup = true;
        }

        /// <summary>
        ///     How this node will attempt to find other reachable <see cref="Node"/>s to group with.
        ///     Returns a set of <see cref="Node"/>s to consider grouping with. Should not return this current <see cref="Node"/>.
        /// </summary>
        protected abstract IEnumerable<Node> GetReachableNodes();

        private IEnumerable<Node> GetReachableCompatibleNodes()
        {
            return GetReachableNodes().Where(node => node.NodeGroupID == NodeGroupID)
                .Where(node => node.Connectable);
        }

        private IEnumerable<INodeGroup> GetReachableCompatibleGroups()
        {
            return GetReachableCompatibleNodes().Where(node => !node._needsGroup)
                .Select(node => node.NodeGroup)
                .Where(group => group != NodeGroup);
        }

        private void CombineGroupWithReachable()
        {
            Debug.Assert(!_needsGroup);
            foreach (var group in GetReachableCompatibleGroups())
            {
                NodeGroup.CombineGroup(group);
            }
        }

        private void SetNodeGroup(INodeGroup newGroup)
        {
            _nodeGroup = newGroup;
            NodeGroup.AddNode(this);
            _needsGroup = false;
        }

        private INodeGroup MakeNewGroup()
        {
            return _nodeGroupFactory.MakeNodeGroup(NodeGroupID);
        }

        private void AnchorUpdate()
        {
            if (Anchored)
            {
                if (_needsGroup)
                {
                    TryAssignGroupIfNeeded();
                    CombineGroupWithReachable();
                }
            }
            else
            {
                NodeGroup.RemoveNode(this);
                ClearNodeGroup();
            }
        }
    }
}
