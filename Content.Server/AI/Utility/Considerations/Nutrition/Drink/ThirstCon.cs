using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.GameObjects.Components.Nutrition;
using Content.Shared.GameObjects.Components.Nutrition;

namespace Content.Server.AI.Utility.Considerations.Nutrition.Drink
{
    public class ThirstCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();

            if (!owner.TryGetComponent(out ThirstComponent thirst))
            {
                return 0.0f;
            }

            return 1 - (thirst.CurrentThirst / thirst.ThirstThresholds[ThirstThreshold.OverHydrated]);
        }
    }
}
