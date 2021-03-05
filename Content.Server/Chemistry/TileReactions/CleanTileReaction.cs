﻿using System.Linq;
using Content.Server.GameObjects.Components;
using Content.Server.Utility;
using Content.Shared.Chemistry;
using Content.Shared.Interfaces.Chemistry;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.TileReactions
{
    [DataDefinition]
    public class CleanTileReaction : ITileReaction
    {
        ReagentUnit ITileReaction.TileReact(TileRef tile, ReagentPrototype reagent, ReagentUnit reactVolume)
        {
            var entities = tile.GetEntitiesInTileFast().ToArray();
            var amount = ReagentUnit.Zero;
            foreach (var entity in entities)
            {
                if (entity.TryGetComponent(out CleanableComponent cleanable))
                {
                    var next = amount + cleanable.CleanAmount;
                    // Nothing left?
                    if (reactVolume < next)
                        break;

                    amount = next;
                    entity.Delete();
                }
            }

            return amount;
        }
    }
}
