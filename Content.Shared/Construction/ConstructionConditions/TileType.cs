﻿#nullable enable
using System.Collections.Generic;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Construction.ConstructionConditions
{
    [UsedImplicitly]
    [DataDefinition]
    public class TileType : IConstructionCondition
    {
        [DataField("targets")] public List<string> TargetTiles { get; private set; } = new();

        public bool Condition(IEntity user, EntityCoordinates location, Direction direction)
        {
            if (TargetTiles == null) return true;

            var tileFound = location.GetTileRef();

            if (tileFound == null)
                return false;

            var tile = tileFound.Value.Tile.GetContentTileDefinition();
            foreach (var targetTile in TargetTiles)
            {
                if (tile.Name == targetTile) {
                    return true;
                }
            }
            return false;
        }
    }
}
