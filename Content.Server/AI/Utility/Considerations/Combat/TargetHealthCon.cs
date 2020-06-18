using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.GameObjects;
using Content.Shared.GameObjects;

namespace Content.Server.AI.Utility.Considerations.Combat
{
    public sealed class TargetHealthCon : Consideration
    {
        public TargetHealthCon(IResponseCurve curve) : base(curve) {}

        public override float GetScore(Blackboard context)
        {
            var target = context.GetState<TargetEntityState>().GetValue();

            if (target == null || !target.TryGetComponent(out DamageableComponent damageableComponent))
            {
                return 0.0f;
            }

            // Just went with max health
            return damageableComponent.CurrentDamage[DamageType.Total] / 300.0f;
        }
    }
}
