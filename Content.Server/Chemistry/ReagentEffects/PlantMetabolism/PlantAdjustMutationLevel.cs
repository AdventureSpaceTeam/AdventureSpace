﻿using Content.Shared.Chemistry.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Chemistry.ReagentEffects.PlantMetabolism
{
    public class PlantAdjustMutationLevel : PlantAdjustAttribute
    {
        public override void Metabolize(EntityUid plantHolder, EntityUid organEntity, Solution.ReagentQuantity reagent, IEntityManager entityManager)
        {
            if (!CanMetabolize(plantHolder, out var plantHolderComp, entityManager))
                return;

            plantHolderComp.MutationLevel += Amount * plantHolderComp.MutationMod;
        }
    }
}
