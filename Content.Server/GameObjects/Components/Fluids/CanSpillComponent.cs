using System;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Fluids
{
    [RegisterComponent]
    public class CanSpillComponent : Component
    {
        public override string Name => "CanSpill";
        // TODO: If the Owner doesn't have a SolutionComponent straight up just have this remove itself?

        /// <summary>
        ///     Transfers solution from the held container to the target container.
        /// </summary>
        [Verb]
        private sealed class FillTargetVerb : Verb<CanSpillComponent>
        {
            protected override void GetData(IEntity user, CanSpillComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user) ||
                    !component.Owner.TryGetComponent(out SolutionComponent solutionComponent))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = "Spill liquid";
                data.Visibility = solutionComponent.CurrentVolume > ReagentUnit.Zero
                    ? VerbVisibility.Visible
                    : VerbVisibility.Disabled;
            }

            protected override void Activate(IEntity user, CanSpillComponent component)
            {
                var solutionComponent = component.Owner.GetComponent<SolutionComponent>();
                // Need this as when we split the component's owner may be deleted
                var entityLocation = component.Owner.Transform.GridPosition;
                var solution = solutionComponent.SplitSolution(solutionComponent.CurrentVolume);
                SpillHelper.SpillAt(entityLocation, solution, "PuddleSmear");
            }
        }
    }
}
