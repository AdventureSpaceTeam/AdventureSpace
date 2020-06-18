using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems.AI.Pathfinding.Pathfinders;
using Content.Shared.AI;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.EntitySystems.AI.Pathfinding
{
#if DEBUG
    [UsedImplicitly]
    public class ServerPathfindingDebugSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            AStarPathfindingJob.DebugRoute += DispatchAStarDebug;
            JpsPathfindingJob.DebugRoute += DispatchJpsDebug;
            SubscribeNetworkEvent<SharedAiDebug.RequestPathfindingGraphMessage>(DispatchGraph);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            AStarPathfindingJob.DebugRoute -= DispatchAStarDebug;
            JpsPathfindingJob.DebugRoute -= DispatchJpsDebug;
        }

        private void DispatchAStarDebug(SharedAiDebug.AStarRouteDebug routeDebug)
        {
            var mapManager = IoCManager.Resolve<IMapManager>();
            var route = new List<Vector2>();
            foreach (var tile in routeDebug.Route)
            {
                var tileGrid = mapManager.GetGrid(tile.GridIndex).GridTileToLocal(tile.GridIndices);
                route.Add(mapManager.GetGrid(tile.GridIndex).LocalToWorld(tileGrid).Position);
            }

            var cameFrom = new Dictionary<Vector2, Vector2>();
            foreach (var (from, to) in routeDebug.CameFrom)
            {
                var tileOneGrid = mapManager.GetGrid(from.GridIndex).GridTileToLocal(from.GridIndices);
                var tileOneWorld = mapManager.GetGrid(from.GridIndex).LocalToWorld(tileOneGrid).Position;
                var tileTwoGrid = mapManager.GetGrid(to.GridIndex).GridTileToLocal(to.GridIndices);
                var tileTwoWorld = mapManager.GetGrid(to.GridIndex).LocalToWorld(tileTwoGrid).Position;
                cameFrom.Add(tileOneWorld, tileTwoWorld);
            }

            var gScores = new Dictionary<Vector2, float>();
            foreach (var (tile, score) in routeDebug.GScores)
            {
                var tileGrid = mapManager.GetGrid(tile.GridIndex).GridTileToLocal(tile.GridIndices);
                gScores.Add(mapManager.GetGrid(tile.GridIndex).LocalToWorld(tileGrid).Position, score);
            }

            var closedTiles = new List<Vector2>();
            foreach (var tile in routeDebug.ClosedTiles)
            {
                var tileGrid = mapManager.GetGrid(tile.GridIndex).GridTileToLocal(tile.GridIndices);
                closedTiles.Add(mapManager.GetGrid(tile.GridIndex).LocalToWorld(tileGrid).Position);
            }

            var systemMessage = new SharedAiDebug.AStarRouteMessage(
                routeDebug.EntityUid,
                route,
                cameFrom,
                gScores,
                closedTiles,
                routeDebug.TimeTaken
                );

            EntityManager.EntityNetManager.SendSystemNetworkMessage(systemMessage);
        }

        private void DispatchJpsDebug(SharedAiDebug.JpsRouteDebug routeDebug)
        {
            var mapManager = IoCManager.Resolve<IMapManager>();
            var route = new List<Vector2>();
            foreach (var tile in routeDebug.Route)
            {
                var tileGrid = mapManager.GetGrid(tile.GridIndex).GridTileToLocal(tile.GridIndices);
                route.Add(mapManager.GetGrid(tile.GridIndex).LocalToWorld(tileGrid).Position);
            }

            var jumpNodes = new List<Vector2>();
            foreach (var tile in routeDebug.JumpNodes)
            {
                var tileGrid = mapManager.GetGrid(tile.GridIndex).GridTileToLocal(tile.GridIndices);
                jumpNodes.Add(mapManager.GetGrid(tile.GridIndex).LocalToWorld(tileGrid).Position);
            }

            var systemMessage = new SharedAiDebug.JpsRouteMessage(
                routeDebug.EntityUid,
                route,
                jumpNodes,
                routeDebug.TimeTaken
            );

            EntityManager.EntityNetManager.SendSystemNetworkMessage(systemMessage);
        }

        private void DispatchGraph(SharedAiDebug.RequestPathfindingGraphMessage message)
        {
            var pathfindingSystem = EntitySystemManager.GetEntitySystem<PathfindingSystem>();
            var mapManager = IoCManager.Resolve<IMapManager>();
            var result = new Dictionary<int, List<Vector2>>();

            var idx = 0;

            foreach (var (gridId, chunks) in pathfindingSystem.Graph)
            {
                var gridManager = mapManager.GetGrid(gridId);

                foreach (var chunk in chunks.Values)
                {
                    var nodes = new List<Vector2>();
                    foreach (var node in chunk.Nodes)
                    {
                        var worldTile = gridManager.GridTileToWorldPos(node.TileRef.GridIndices);

                        nodes.Add(worldTile);
                    }

                    result.Add(idx, nodes);
                    idx++;
                }
            }

            var systemMessage = new SharedAiDebug.PathfindingGraphMessage(result);
            EntityManager.EntityNetManager.SendSystemNetworkMessage(systemMessage);
        }
    }
#endif
}
