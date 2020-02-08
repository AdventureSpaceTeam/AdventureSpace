﻿using System;
using System.Linq;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Maps;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.Server.Explosions
{
    public static class ExplosionHelper
    {
        /// <summary>
        /// Distance used for camera shake when distance from explosion is (0.0, 0.0).
        /// Avoids getting NaN values down the line from doing math on (0.0, 0.0).
        /// </summary>
        private static Vector2 _epicenterDistance = (0.1f, 0.1f);

        public static void SpawnExplosion(GridCoordinates coords, int devastationRange, int heavyImpactRange, int lightImpactRange, int flashRange)
        {
            var tileDefinitionManager = IoCManager.Resolve<ITileDefinitionManager>();
            var serverEntityManager = IoCManager.Resolve<IServerEntityManager>();
            var entitySystemManager = IoCManager.Resolve<IEntitySystemManager>();
            var mapManager = IoCManager.Resolve<IMapManager>();
            var robustRandom = IoCManager.Resolve<IRobustRandom>();

            var maxRange = MathHelper.Max(devastationRange, heavyImpactRange, lightImpactRange, 0f);
            //Entity damage calculation
            var entitiesAll = serverEntityManager.GetEntitiesInRange(coords, maxRange).ToList();

            foreach (var entity in entitiesAll)
            {
                if (entity.Deleted)
                    continue;
                if (!entity.Transform.IsMapTransform)
                    continue;

                var distanceFromEntity = (int)entity.Transform.GridPosition.Distance(mapManager, coords);
                var exAct = entitySystemManager.GetEntitySystem<ActSystem>();
                var severity = ExplosionSeverity.Destruction;
                if (distanceFromEntity < devastationRange)
                {
                    severity = ExplosionSeverity.Destruction;
                }
                else if (distanceFromEntity < heavyImpactRange)
                {
                    severity = ExplosionSeverity.Heavy;
                }
                else if (distanceFromEntity < lightImpactRange)
                {
                    severity = ExplosionSeverity.Light;
                }
                else
                {
                    continue;
                }
                //exAct.HandleExplosion(Owner, entity, severity);
                exAct.HandleExplosion(null, entity, severity);
            }

            //Tile damage calculation mockup
            //TODO: make it into some sort of actual damage component or whatever the boys think is appropriate
            var mapGrid = mapManager.GetGrid(coords.GridID);
            var circle = new Circle(coords.Position, maxRange);
            var tiles = mapGrid.GetTilesIntersecting(circle);
            foreach (var tile in tiles)
            {
                var tileLoc = mapGrid.GridTileToLocal(tile.GridIndices);
                var tileDef = (ContentTileDefinition)tileDefinitionManager[tile.Tile.TypeId];
                var distanceFromTile = (int)tileLoc.Distance(mapManager, coords);
                if (!string.IsNullOrWhiteSpace(tileDef.SubFloor))
                {
                    if (distanceFromTile < devastationRange)
                        mapGrid.SetTile(tileLoc, new Tile(tileDefinitionManager["space"].TileId));
                    if (distanceFromTile < heavyImpactRange)
                    {
                        if (robustRandom.Prob(80))
                        {
                            mapGrid.SetTile(tileLoc, new Tile(tileDefinitionManager[tileDef.SubFloor].TileId));
                        }
                        else
                        {
                            mapGrid.SetTile(tileLoc, new Tile(tileDefinitionManager["space"].TileId));
                        }
                    }
                    if (distanceFromTile < lightImpactRange)
                    {
                        if (robustRandom.Prob(50))
                        {
                            mapGrid.SetTile(tileLoc, new Tile(tileDefinitionManager[tileDef.SubFloor].TileId));
                        }
                    }
                }
            }

            //Effects and sounds
            var time = IoCManager.Resolve<IGameTiming>().CurTime;
            var message = new EffectSystemMessage
            {
                EffectSprite = "Effects/explosion.rsi",
                RsiState = "explosionfast",
                Born = time,
                DeathTime = time + TimeSpan.FromSeconds(5),
                Size = new Vector2(flashRange / 2, flashRange / 2),
                Coordinates = coords,
                //Rotated from east facing
                Rotation = 0f,
                ColorDelta = new Vector4(0, 0, 0, -1500f),
                Color = Vector4.Multiply(new Vector4(255, 255, 255, 750), 0.5f),
                Shaded = false
            };
            entitySystemManager.GetEntitySystem<EffectSystem>().CreateParticle(message);
            entitySystemManager.GetEntitySystem<AudioSystem>().Play("/Audio/effects/explosion.ogg", coords);

            // Knock back cameras of all players in the area.

            var playerManager = IoCManager.Resolve<IPlayerManager>();
            foreach (var player in playerManager.GetAllPlayers())
            {
                if (player.AttachedEntity == null
                    || player.AttachedEntity.Transform.MapID != mapGrid.ParentMapId
                    || !player.AttachedEntity.TryGetComponent(out CameraRecoilComponent recoil))
                {
                    continue;
                }

                var playerPos = player.AttachedEntity.Transform.WorldPosition;
                var delta = coords.ToMapPos(mapManager) - playerPos;
                //Change if zero. Will result in a NaN later breaking camera shake if not changed
                if (delta.EqualsApprox((0.0f, 0.0f)))
                    delta = _epicenterDistance;

                var distance = delta.LengthSquared;
                var effect = 1 / (1 + 0.2f * distance);
                if (effect > 0.01f)
                {
                    var kick = -delta.Normalized * effect;
                    recoil.Kick(kick);
                }
            }
        }
    }
}
