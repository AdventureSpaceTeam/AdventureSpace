﻿using System;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Arcade
{
    [RegisterComponent]
    public class RandomArcadeGameComponent : Component, IMapInit
    {
        public override string Name => "RandomArcade";

        public void MapInit()
        {
            var arcades = new[]
            {
                "BlockGameArcade",
                "SpaceVillainArcade"
            };

            var entityManager = IoCManager.Resolve<IEntityManager>();

            entityManager.SpawnEntity(
                IoCManager.Resolve<IRobustRandom>().Pick(arcades),
                Owner.Transform.Coordinates);

            Owner.Delete();
        }
    }
}
