using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Clothing;
using Content.Shared.GameObjects.Components.Inventory;

namespace Content.Server.AI.Utility.Considerations.Clothing
{
    public class ClothingInSlotCon : Consideration
    {

        public ClothingInSlotCon Slot(EquipmentSlotDefines.Slots slot, Blackboard context)
        {
            context.GetState<ClothingSlotConState>().SetValue(slot);
            return this;
        }
        
        protected override float GetScore(Blackboard context)
        {
            var slot = context.GetState<ClothingSlotConState>().GetValue();
            var inventory = context.GetState<EquippedClothingState>().GetValue();
            return inventory.ContainsKey(slot) ? 1.0f : 0.0f;
        }
    }
}
