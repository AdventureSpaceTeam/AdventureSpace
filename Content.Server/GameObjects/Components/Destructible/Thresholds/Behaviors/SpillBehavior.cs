#nullable enable
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.Components.Fluids;
using Content.Server.GameObjects.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds.Behaviors
{
    [UsedImplicitly]
    [DataDefinition]
    public class SpillBehavior : IThresholdBehavior
    {
        public void Execute(IEntity owner, DestructibleSystem system)
        {
            if (!owner.TryGetComponent(out SolutionContainerComponent? solutionContainer))
                return;

            solutionContainer.Solution.SpillAt(owner.Transform.Coordinates, "PuddleSmear", false);
        }
    }
}
