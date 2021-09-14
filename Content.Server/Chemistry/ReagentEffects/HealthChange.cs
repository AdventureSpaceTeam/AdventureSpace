using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Content.Shared.Damage;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// Default metabolism for medicine reagents.
    /// </summary>
    public class HealthChange : ReagentEffect
    {
        /// <summary>
        /// Damage to apply every metabolism cycle. Damage Ignores resistances.
        /// </summary>
        [DataField("damage", required: true)]
        public DamageSpecifier Damage = default!;

        public override void Metabolize(IEntity solutionEntity, Solution.ReagentQuantity amount)
        {
            EntitySystem.Get<DamageableSystem>().TryChangeDamage(solutionEntity.Uid, Damage, true);
        }
    }
}
