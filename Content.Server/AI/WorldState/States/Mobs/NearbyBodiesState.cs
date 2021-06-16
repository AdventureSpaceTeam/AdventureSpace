﻿using System.Collections.Generic;
using Content.Server.AI.Components;
using Content.Server.AI.Utils;
using Content.Shared.Body.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.AI.WorldState.States.Mobs
{
    [UsedImplicitly]
    public sealed class NearbyBodiesState : CachedStateData<List<IEntity>>
    {
        public override string Name => "NearbyBodies";

        protected override List<IEntity> GetTrueValue()
        {
            var result = new List<IEntity>();

            if (!Owner.TryGetComponent(out AiControllerComponent? controller))
            {
                return result;
            }

            foreach (var entity in Visibility.GetEntitiesInRange(Owner.Transform.Coordinates, typeof(SharedBodyComponent), controller.VisionRadius))
            {
                if (entity == Owner) continue;
                result.Add(entity);
            }

            return result;
        }
    }
}
