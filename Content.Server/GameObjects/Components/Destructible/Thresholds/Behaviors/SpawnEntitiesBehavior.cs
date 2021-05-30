#nullable enable
using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds.Behaviors
{
    [Serializable]
    [DataDefinition]
    public class SpawnEntitiesBehavior : IThresholdBehavior
    {
        /// <summary>
        ///     Entities spawned on reaching this threshold, from a min to a max.
        /// </summary>
        [DataField("spawn")]
        public Dictionary<string, MinMax> Spawn { get; set; } = new();

        public void Execute(IEntity owner, DestructibleSystem system)
        {
            foreach (var (entityId, minMax) in Spawn)
            {
                var count = minMax.Min >= minMax.Max
                    ? minMax.Min
                    : system.Random.Next(minMax.Min, minMax.Max + 1);

                if (count == 0) continue;

                if (EntityPrototypeHelpers.HasComponent<StackComponent>(entityId))
                {
                    var spawned = owner.EntityManager.SpawnEntity(entityId, owner.Transform.MapPosition);
                    owner.EntityManager.EventBus.RaiseLocalEvent(spawned.Uid, new StackChangeCountEvent(count), false);
                    spawned.RandomOffset(0.5f);
                }
                else
                {
                    for (var i = 0; i < count; i++)
                    {
                        var spawned = owner.EntityManager.SpawnEntity(entityId, owner.Transform.MapPosition);
                        spawned.RandomOffset(0.5f);
                    }
                }
            }
        }
    }
}
