using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Hands;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Server.GameObjects.Components.Items.Storage;

namespace Content.Server.AI.Utility.Considerations.Inventory
{
    public class CanPutTargetInInventoryCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            // First check if target in inventory already
            // If not then check if we have a free hand
            var target = context.GetState<TargetEntityState>().GetValue();

            if (target == null || !target.HasComponent<ItemComponent>())
            {
                return 0.0f;
            }

            foreach (var item in context.GetState<EnumerableInventoryState>().GetValue())
            {
                if (item == target)
                {
                    return 1.0f;
                }
            }

            return context.GetState<AnyFreeHandState>().GetValue() ? 1.0f : 0.0f;
        }
    }
}
