#nullable enable
using System;
using JetBrains.Annotations;

namespace Content.Server.NodeContainer.NodeGroups
{
    /// <summary>
    ///     Associates a <see cref="INodeGroup"/> implementation with a <see cref="NodeGroupID"/>.
    ///     This is used to gurantee all <see cref="INode"/>s of the same <see cref="INode.NodeGroupID"/>
    ///     have the same type of <see cref="INodeGroup"/>. Used by <see cref="INodeGroupFactory"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    [MeansImplicitUse]
    public class NodeGroupAttribute : Attribute
    {
        public NodeGroupID[] NodeGroupIDs { get; }

        public NodeGroupAttribute(params NodeGroupID[] nodeGroupTypes)
        {
            NodeGroupIDs = nodeGroupTypes;
        }
    }
}
