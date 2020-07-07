using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Combat;
using Content.Server.GameObjects.Components.Weapon.Melee;

namespace Content.Server.AI.Utility.Considerations.Combat.Melee
{
    public sealed class MeleeWeaponDamageCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            var target = context.GetState<WeaponEntityState>().GetValue();

            if (target == null || !target.TryGetComponent(out MeleeWeaponComponent meleeWeaponComponent))
            {
                return 0.0f;
            }

            // Just went with max health
            return meleeWeaponComponent.Damage / 300.0f;
        }
    }
}
