using Content.Server.GameObjects;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Weapon.Melee;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Operators.Combat
{
    public class SwingMeleeWeaponOperator : AiOperator
    {
        private float _burstTime;
        private float _elapsedTime;

        private readonly IEntity _owner;
        private readonly IEntity _target;

        public SwingMeleeWeaponOperator(IEntity owner, IEntity target, float burstTime = 1.0f)
        {
            _owner = owner;
            _target = target;
            _burstTime = burstTime;
        }

        public override bool TryStartup()
        {
            if (!base.TryStartup())
            {
                return true;
            }
            
            if (!_owner.TryGetComponent(out CombatModeComponent combatModeComponent))
            {
                return false;
            }

            if (!combatModeComponent.IsInCombatMode)
            {
                combatModeComponent.IsInCombatMode = true;
            }

            return true;
        }

        public override Outcome Execute(float frameTime)
        {
            if (_burstTime <= _elapsedTime)
            {
                return Outcome.Success;
            }

            if (!_owner.TryGetComponent(out HandsComponent hands) || hands.GetActiveHand == null)
            {
                return Outcome.Failed;
            }

            var meleeWeapon = hands.GetActiveHand.Owner;
            meleeWeapon.TryGetComponent(out MeleeWeaponComponent meleeWeaponComponent);

            if ((_target.Transform.GridPosition.Position - _owner.Transform.GridPosition.Position).Length >
                meleeWeaponComponent.Range)
            {
                return Outcome.Failed;
            }

            var interactionSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<InteractionSystem>();

            interactionSystem.UseItemInHand(_owner, _target.Transform.GridPosition, _target.Uid);
            _elapsedTime += frameTime;
            return Outcome.Continuing;
        }
    }

}
