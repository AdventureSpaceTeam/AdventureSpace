using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry
{
    [UsedImplicitly]
    public partial class ChemistrySystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public void ReactionEntity(IEntity? entity, ReactionMethod method, string reagentId, ReagentUnit reactVolume,
            Components.Solution? source)
        {
            // We throw if the reagent specified doesn't exist.
            ReactionEntity(entity, method, _prototypeManager.Index<ReagentPrototype>(reagentId), reactVolume, source);
        }

        public void ReactionEntity(IEntity? entity, ReactionMethod method, ReagentPrototype reagent,
            ReagentUnit reactVolume, Components.Solution? source)
        {
            if (entity == null || entity.Deleted || !entity.TryGetComponent(out ReactiveComponent? reactive))
                return;

            foreach (var reaction in reactive.Reactions)
            {
                // If we have a source solution, use the reagent quantity we have left. Otherwise, use the reaction volume specified.
                reaction.React(method, entity, reagent, source?.GetReagentQuantity(reagent.ID) ?? reactVolume, source);

                // Make sure we still have enough reagent to go...
                if (source != null && !source.ContainsReagent(reagent.ID))
                    break;
            }
        }
    }
}
