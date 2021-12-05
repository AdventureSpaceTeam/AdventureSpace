using Content.Server.CombatMode;
using Content.Server.Interaction;
using Content.Shared.Interaction.Helpers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Operators.Inventory
{
    /// <summary>
    /// A Generic interacter; if you need to check stuff then make your own
    /// </summary>
    public class InteractWithEntityOperator : AiOperator
    {
        private readonly EntityUid _owner;
        private readonly EntityUid _useTarget;

        public InteractWithEntityOperator(EntityUid owner, EntityUid useTarget)
        {
            _owner = owner;
            _useTarget = useTarget;

        }

        public override Outcome Execute(float frameTime)
        {
            if (IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(_useTarget).GridID != IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(_owner).GridID)
            {
                return Outcome.Failed;
            }

            if (!_owner.InRangeUnobstructed(_useTarget, popup: true))
            {
                return Outcome.Failed;
            }

            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(_owner, out CombatModeComponent? combatModeComponent))
            {
                combatModeComponent.IsInCombatMode = false;
            }

            // Click on da thing
            var interactionSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<InteractionSystem>();
            interactionSystem.AiUseInteraction(_owner, IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(_useTarget).Coordinates, _useTarget);

            return Outcome.Success;
        }
    }
}
