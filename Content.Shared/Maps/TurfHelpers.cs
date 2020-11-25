﻿#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Physics;
using Content.Shared.Utility;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Shared.Maps
{
    public static class TurfHelpers
    {
        /// <summary>
        ///     Attempts to get the turf at map indices with grid id or null if no such turf is found.
        /// </summary>
        public static TileRef GetTileRef(this Vector2i Vector2i, GridId gridId, IMapManager? mapManager = null)
        {
            if (!gridId.IsValid())
                return default;

            mapManager ??= IoCManager.Resolve<IMapManager>();

            if (!mapManager.TryGetGrid(gridId, out var grid))
                return default;

            if (!grid.TryGetTileRef(Vector2i, out var tile))
                return default;

            return tile;
        }

        /// <summary>
        ///     Attempts to get the turf at a certain coordinates or null if no such turf is found.
        /// </summary>
        public static TileRef? GetTileRef(this EntityCoordinates coordinates, IEntityManager? entityManager = null, IMapManager? mapManager = null)
        {
            entityManager ??= IoCManager.Resolve<IEntityManager>();

            if (!coordinates.IsValid(entityManager))
                return null;


            mapManager ??= IoCManager.Resolve<IMapManager>();

            if (!mapManager.TryGetGrid(coordinates.GetGridId(entityManager), out var grid))
                return null;


            if (!grid.TryGetTileRef(coordinates, out var tile))
                return null;

            return tile;
        }

        public static bool TryGetTileRef(this EntityCoordinates coordinates, [NotNullWhen(true)] out TileRef? turf)
        {
            return (turf = coordinates.GetTileRef()) != null;
        }

        /// <summary>
        ///     Returns the content tile definition for a tile.
        /// </summary>
        public static ContentTileDefinition GetContentTileDefinition(this Tile tile, ITileDefinitionManager? tileDefinitionManager = null)
        {
            tileDefinitionManager ??= IoCManager.Resolve<ITileDefinitionManager>();
            return (ContentTileDefinition)tileDefinitionManager[tile.TypeId];
        }

        /// <summary>
        ///     Returns whether a tile is considered space.
        /// </summary>
        public static bool IsSpace(this Tile tile, ITileDefinitionManager? tileDefinitionManager = null)
        {
            return tile.GetContentTileDefinition(tileDefinitionManager).IsSpace;
        }

        /// <summary>
        ///     Returns the content tile definition for a tile ref.
        /// </summary>
        public static ContentTileDefinition GetContentTileDefinition(this TileRef tile, ITileDefinitionManager? tileDefinitionManager = null)
        {
            return tile.Tile.GetContentTileDefinition(tileDefinitionManager);
        }

        /// <summary>
        ///     Returns whether a tile ref is considered space.
        /// </summary>
        public static bool IsSpace(this TileRef tile, ITileDefinitionManager? tileDefinitionManager = null)
        {
            return tile.Tile.IsSpace(tileDefinitionManager);
        }

        public static bool PryTile(this EntityCoordinates coordinates, IEntityManager? entityManager = null,
            IMapManager? mapManager = null)
        {
            entityManager ??= IoCManager.Resolve<IEntityManager>();
            mapManager ??= IoCManager.Resolve<IMapManager>();

            return coordinates.ToVector2i(entityManager, mapManager).PryTile(coordinates.GetGridId(entityManager));
        }

        public static bool PryTile(this Vector2i indices, GridId gridId,
            IMapManager? mapManager = null, ITileDefinitionManager? tileDefinitionManager = null, IEntityManager? entityManager = null)
        {
            mapManager ??= IoCManager.Resolve<IMapManager>();
            var grid = mapManager.GetGrid(gridId);
            var tileRef = grid.GetTileRef(indices);
            return tileRef.PryTile(mapManager, tileDefinitionManager, entityManager);
        }

        public static bool PryTile(this TileRef tileRef,
            IMapManager? mapManager = null, ITileDefinitionManager? tileDefinitionManager = null, IEntityManager? entityManager = null)
        {
            var tile = tileRef.Tile;
            var indices = tileRef.GridIndices;

            // If the arguments are null, resolve the needed dependencies.
            mapManager ??= IoCManager.Resolve<IMapManager>();
            tileDefinitionManager ??= IoCManager.Resolve<ITileDefinitionManager>();
            entityManager ??= IoCManager.Resolve<IEntityManager>();

            if (tile.IsEmpty) return false;

            var tileDef = (ContentTileDefinition) tileDefinitionManager[tile.TypeId];

            if (!tileDef.CanCrowbar) return false;

            var mapGrid = mapManager.GetGrid(tileRef.GridIndex);

            var plating = tileDefinitionManager[tileDef.BaseTurfs[^1]];

             mapGrid.SetTile(tileRef.GridIndices, new Tile(plating.TileId));

             var half = mapGrid.TileSize / 2f;

            //Actually spawn the relevant tile item at the right position and give it some random offset.
            var tileItem = entityManager.SpawnEntity(tileDef.ItemDropPrototypeName, indices.ToEntityCoordinates(tileRef.GridIndex, mapManager).Offset(new Vector2(half, half)));
            tileItem.RandomOffset(0.25f);
            return true;
        }

        /// <summary>
        ///     Helper that returns all entities in a turf.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<IEntity> GetEntitiesInTile(this TileRef turf, bool approximate = false, IEntityManager? entityManager = null)
        {
            entityManager ??= IoCManager.Resolve<IEntityManager>();

            return entityManager.GetEntitiesIntersecting(turf.MapIndex, GetWorldTileBox(turf), approximate);
        }

        /// <summary>
        ///     Helper that returns all entities in a turf.
        /// </summary>
        public static IEnumerable<IEntity> GetEntitiesInTile(this EntityCoordinates coordinates, bool approximate = false, IEntityManager? entityManager = null)
        {
            var turf = coordinates.GetTileRef();

            if (turf == null)
                return Enumerable.Empty<IEntity>();

            return GetEntitiesInTile(turf.Value, approximate, entityManager);
        }

        /// <summary>
        ///     Helper that returns all entities in a turf.
        /// </summary>
        public static IEnumerable<IEntity> GetEntitiesInTile(this Vector2i indices, GridId gridId, bool approximate = false, IEntityManager? entityManager = null)
        {
            return GetEntitiesInTile(indices.GetTileRef(gridId), approximate, entityManager);
        }

        /// <summary>
        /// Checks if a turf has something dense on it.
        /// </summary>
        public static bool IsBlockedTurf(this TileRef turf, bool filterMobs)
        {
            var physics = IoCManager.Resolve<IPhysicsManager>();

            var worldBox = GetWorldTileBox(turf);

            var query = physics.GetCollidingEntities(turf.MapIndex, in worldBox);

            foreach (var body in query)
            {
                if (body.CanCollide && body.Hard && (body.CollisionLayer & (int) CollisionGroup.Impassable) != 0)
                    return true;

                if (filterMobs && (body.CollisionLayer & (int) CollisionGroup.MobMask) != 0)
                    return true;
            }

            return false;
        }

        public static EntityCoordinates GridPosition(this TileRef turf, IMapManager? mapManager = null)
        {
            mapManager ??= IoCManager.Resolve<IMapManager>();

            return turf.GridIndices.ToEntityCoordinates(turf.GridIndex, mapManager);
        }

        /// <summary>
        /// Creates a box the size of a tile, at the same position in the world as the tile.
        /// </summary>
        private static Box2 GetWorldTileBox(TileRef turf)
        {
            var map = IoCManager.Resolve<IMapManager>();

            // This is scaled to 90 % so it doesn't encompass walls on other tiles.
            var tileBox = Box2.UnitCentered.Scale(0.9f);

            if (map.TryGetGrid(turf.GridIndex, out var tileGrid))
            {
                tileBox = tileBox.Scale(tileGrid.TileSize);
                return tileBox.Translated(tileGrid.GridTileToWorldPos(turf.GridIndices));
            }

            return tileBox;
        }
    }
}
