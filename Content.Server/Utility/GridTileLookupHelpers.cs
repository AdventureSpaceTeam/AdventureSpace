﻿#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Content.Shared.Maps;
using Robust.Server.GameObjects.EntitySystems.TileLookup;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.Utility
{
    public static class GridTileLookupHelpers
    {
        /// <summary>
        ///     Helper that returns all entities in a turf very fast.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<IEntity> GetEntitiesInTileFast(this TileRef turf, GridTileLookupSystem? gridTileLookup = null)
        {
            gridTileLookup ??= EntitySystem.Get<GridTileLookupSystem>();

            return gridTileLookup.GetEntitiesIntersecting(turf.GridIndex, turf.GridIndices);
        }

        /// <summary>
        ///     Helper that returns all entities in a turf.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<IEntity> GetEntitiesInTileFast(this Vector2i indices, GridId gridId, GridTileLookupSystem? gridTileLookup = null)
        {
            gridTileLookup ??= EntitySystem.Get<GridTileLookupSystem>();
            return gridTileLookup.GetEntitiesIntersecting(gridId, indices);
        }
    }
}
