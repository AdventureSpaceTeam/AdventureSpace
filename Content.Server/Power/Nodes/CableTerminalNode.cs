﻿using System.Collections.Generic;
using Content.Server.NodeContainer.Nodes;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Power.Nodes
{
    [DataDefinition]
    public class CableTerminalNode : CableDeviceNode
    {
        public override IEnumerable<Node> GetReachableNodes()
        {
            var compMgr = IoCManager.Resolve<IComponentManager>();
            var grid = IoCManager.Resolve<IMapManager>().GetGrid(Owner.Transform.GridID);
            var gridIndex = grid.TileIndicesFor(Owner.Transform.Coordinates);

            var dir = Owner.Transform.LocalRotation.GetDir();
            var targetIdx = gridIndex + NodeHelpers.TileOffsetForDir(dir);

            foreach (var node in NodeHelpers.GetNodesInTile(compMgr, grid, targetIdx))
            {
                if (node is CableTerminalPortNode)
                    yield return node;
            }

            foreach (var node in base.GetReachableNodes())
            {
                yield return node;
            }
        }
    }
}
