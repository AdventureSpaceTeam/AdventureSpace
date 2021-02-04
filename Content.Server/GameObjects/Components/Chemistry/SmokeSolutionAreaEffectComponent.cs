﻿#nullable enable
using System.Linq;
using Content.Server.GameObjects.Components.Body.Circulatory;
using Content.Server.GameObjects.Components.Body.Respiratory;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Chemistry;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Chemistry
{
    [RegisterComponent]
    [ComponentReference(typeof(SolutionAreaEffectComponent))]
    public class SmokeSolutionAreaEffectComponent : SolutionAreaEffectComponent
    {
        public override string Name => "SmokeSolutionAreaEffect";

        protected override void UpdateVisuals()
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance) &&
                SolutionContainerComponent != null)
            {
                appearance.SetData(SmokeVisuals.Color, SolutionContainerComponent.Color);
            }
        }

        protected override void ReactWithEntity(IEntity entity, double solutionFraction)
        {
            if (SolutionContainerComponent == null)
                return;

            if (!entity.TryGetComponent(out BloodstreamComponent? bloodstream))
                return;

            if (entity.TryGetComponent(out InternalsComponent? internals) &&
                internals.AreInternalsWorking())
                return;

            var cloneSolution = SolutionContainerComponent.Solution.Clone();
            var transferAmount = ReagentUnit.Min(cloneSolution.TotalVolume * solutionFraction, bloodstream.EmptyVolume);
            var transferSolution = cloneSolution.SplitSolution(transferAmount);

            foreach (var reagentQuantity in transferSolution.Contents.ToArray())
            {
                if (reagentQuantity.Quantity == ReagentUnit.Zero) continue;
                var reagent = PrototypeManager.Index<ReagentPrototype>(reagentQuantity.ReagentId);
                transferSolution.RemoveReagent(reagentQuantity.ReagentId,reagent.ReactionEntity(entity, ReactionMethod.Ingestion, reagentQuantity.Quantity));
            }

            bloodstream.TryTransferSolution(transferSolution);
        }


        protected override void OnKill()
        {
            if (Owner.Deleted)
                return;
            Owner.Delete();
        }
    }
}
