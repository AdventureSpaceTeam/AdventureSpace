using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
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

        public void ReactionEntity(EntityUid uid, ReactionMethod method, Solution solution)
        {
            foreach (var (id, quantity) in solution)
            {
                ReactionEntity(uid, method, id, quantity, solution);
            }
        }

        public void ReactionEntity(EntityUid uid, ReactionMethod method, string reagentId, FixedPoint2 reactVolume, Solution? source)
        {
            // We throw if the reagent specified doesn't exist.
            ReactionEntity(uid, method, _prototypeManager.Index<ReagentPrototype>(reagentId), reactVolume, source);
        }

        public void ReactionEntity(EntityUid uid, ReactionMethod method, ReagentPrototype reagent,
            FixedPoint2 reactVolume, Solution? source)
        {
            if (!EntityManager.TryGetComponent(uid, out ReactiveComponent? reactive))
                return;

            foreach (var reaction in reactive.Reactions)
            {
                // If we have a source solution, use the reagent quantity we have left. Otherwise, use the reaction volume specified.
                reaction.React(method, uid, reagent, source?.GetReagentQuantity(reagent.ID) ?? reactVolume, source, EntityManager);

                // Make sure we still have enough reagent to go...
                if (source != null && !source.ContainsReagent(reagent.ID))
                    break;
            }
        }
    }
}
