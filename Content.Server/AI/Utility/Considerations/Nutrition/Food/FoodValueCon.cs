using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Nutrition.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Utility.Considerations.Nutrition.Food
{
    public sealed class FoodValueCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            var target = context.GetState<TargetEntityState>().GetValue();

            if (target == null || (!IoCManager.Resolve<IEntityManager>().EntityExists(target.Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(target.Uid).EntityLifeStage) >= EntityLifeStage.Deleted || !target.TryGetComponent<FoodComponent>(out var foodComp) ||
                !EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(target.Uid, foodComp.SolutionName, out var food))
            {
                return 0.0f;
            }

            var nutritionValue = 0;

            foreach (var reagent in food.Contents)
            {
                // TODO
                nutritionValue += (reagent.Quantity * 30).Int();
            }

            return nutritionValue / 1000.0f;
        }
    }
}
