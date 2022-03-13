using Content.Server.Botany.Components;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Server.Chemistry.ReagentEffects.PlantMetabolism
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class RobustHarvest : ReagentEffect
    {
        public override void Effect(ReagentEffectArgs args)
        {
            if (!args.EntityManager.TryGetComponent(args.SolutionEntity, out PlantHolderComponent? plantHolderComp)
                                    || plantHolderComp.Seed == null || plantHolderComp.Dead ||
                                    plantHolderComp.Seed.Immutable)
                return;

            var random = IoCManager.Resolve<IRobustRandom>();

            if (plantHolderComp.Seed.Potency < 100 && random.Prob(0.1f))
            {
                plantHolderComp.CheckForDivergence(true);
                plantHolderComp.Seed.Potency++;
            }

            if (plantHolderComp.Seed.Yield > 1 && random.Prob(0.1f))
            {
                plantHolderComp.CheckForDivergence(true);
                plantHolderComp.Seed.Yield--;
            }
        }
    }
}
