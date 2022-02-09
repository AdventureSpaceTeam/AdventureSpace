using System.Collections.Generic;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.NodeContainer.Nodes
{
    /// <summary>
    ///     Organizes themselves into distinct <see cref="INodeGroup"/>s with other <see cref="Node"/>s
    ///     that they can "reach" and have the same <see cref="Node.NodeGroupID"/>.
    /// </summary>
    [ImplicitDataDefinitionForInheritors]
    public abstract class Node
    {
        /// <summary>
        ///     An ID used as a criteria for combining into groups. Determines which <see cref="INodeGroup"/>
        ///     implementation is used as a group, detailed in <see cref="INodeGroupFactory"/>.
        /// </summary>
        [ViewVariables]
        [DataField("nodeGroupID")]
        public NodeGroupID NodeGroupID { get; private set; } = NodeGroupID.Default;

        /// <summary>
        ///     The node group this node is a part of.
        /// </summary>
        [ViewVariables] public INodeGroup? NodeGroup;

        /// <summary>
        ///     The entity that owns this node via its <see cref="NodeContainerComponent"/>.
        /// </summary>
        [ViewVariables] public EntityUid Owner { get; private set; } = default!;

        /// <summary>
        ///     If this node should be considered for connection by other nodes.
        /// </summary>
        public virtual bool Connectable(IEntityManager entMan, TransformComponent? xform = null)
        {
            if (Deleting)
                return false;

            if (entMan.IsQueuedForDeletion(Owner))
                return false;

            if (!NeedAnchored)
                return true;

            xform ??= entMan.GetComponent<TransformComponent>(Owner);
            return xform.Anchored;
        }

        protected bool Anchored => !NeedAnchored || IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner).Anchored;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("needAnchored")]
        public bool NeedAnchored { get; } = true;

        /// <summary>
        ///    Prevents a node from being used by other nodes while midway through removal.
        /// </summary>
        public bool Deleting;

        /// <summary>
        ///     All compatible nodes that are reachable by this node.
        ///     Effectively, active connections out of this node.
        /// </summary>
        public readonly HashSet<Node> ReachableNodes = new();

        internal int FloodGen;
        internal int UndirectGen;
        internal bool FlaggedForFlood;
        internal int NetId;

        /// <summary>
        ///     Name of this node on the owning <see cref="NodeContainerComponent"/>.
        /// </summary>
        public string Name = default!;

        /// <summary>
        ///     Invoked when the owning <see cref="NodeContainerComponent"/> is initialized.
        /// </summary>
        /// <param name="owner">The owning entity.</param>
        public virtual void Initialize(EntityUid owner, IEntityManager entMan)
        {
            Owner = owner;
        }

        /// <summary>
        ///     How this node will attempt to find other reachable <see cref="Node"/>s to group with.
        ///     Returns a set of <see cref="Node"/>s to consider grouping with. Should not return this current <see cref="Node"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The set of nodes returned can be asymmetrical
        /// (meaning that it can return other nodes whose <see cref="GetReachableNodes"/> does not return this node).
        /// If this is used, creation of a new node may not correctly merge networks unless both sides
        /// of this asymmetric relation are made to manually update with <see cref="NodeGroupSystem.QueueReflood"/>.
        /// </para>
        /// </remarks>
        public abstract IEnumerable<Node> GetReachableNodes(TransformComponent xform,
            EntityQuery<NodeContainerComponent> nodeQuery,
            EntityQuery<TransformComponent> xformQuery,
            IMapGrid? grid,
            IEntityManager entMan);
    }
}
