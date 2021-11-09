using System.Collections.Generic;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.Chemistry.ReagentEntityReactions
{
    [UsedImplicitly]
    public class AddToSolutionReaction : ReagentEntityReaction
    {
        [DataField("solution")]
        private string _solution = "reagents";

        [DataField("reagents", true, customTypeSerializer: typeof(PrototypeIdHashSetSerializer<ReagentPrototype>))]
        // ReSharper disable once CollectionNeverUpdated.Local
        private readonly HashSet<string> _reagents = new();

        protected override void React(EntityUid uid, ReagentPrototype reagent, FixedPoint2 volume, Solution? source, IEntityManager entityManager)
        {
            // TODO see if this is correct
            if (!EntitySystem.Get<SolutionContainerSystem>()
                    .TryGetSolution(uid, _solution, out var solutionContainer)
                || (_reagents.Count > 0 && !_reagents.Contains(reagent.ID))) return;

            if (EntitySystem.Get<SolutionContainerSystem>()
                .TryAddReagent(uid, solutionContainer, reagent.ID, volume, out var accepted))
                source?.RemoveReagent(reagent.ID, accepted);
        }
    }
}
