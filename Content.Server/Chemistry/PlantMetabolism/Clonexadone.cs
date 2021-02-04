﻿#nullable enable
using System;
using Content.Server.GameObjects.Components.Botany;
using Content.Shared.Interfaces.Chemistry;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;

namespace Content.Server.Chemistry.PlantMetabolism
{
    [UsedImplicitly]
    public class Clonexadone : IPlantMetabolizable
    {
        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
        }

        public void Metabolize(IEntity plantHolder, float customPlantMetabolism = 1)
        {
            if (plantHolder.Deleted || !plantHolder.TryGetComponent(out PlantHolderComponent? plantHolderComp)
            || plantHolderComp.Seed == null || plantHolderComp.Dead)
                return;

            var deviation = 0;
            var seed = plantHolderComp.Seed;
            var random = IoCManager.Resolve<IRobustRandom>();
            if (plantHolderComp.Age > seed.Maturation)
                deviation = (int) Math.Max(seed.Maturation - 1, plantHolderComp.Age - random.Next(7, 10));
            else
                deviation = (int) (seed.Maturation / seed.GrowthStages);
            plantHolderComp.Age -= deviation;
            plantHolderComp.SkipAging++;
            plantHolderComp.ForceUpdate = true;
        }
    }
}
