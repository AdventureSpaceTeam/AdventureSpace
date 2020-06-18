using System.Collections.Generic;
using Content.Server.AI.Operators;
using Content.Server.AI.Operators.Inventory;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.Utility.Considerations.Hands;
using Content.Server.AI.Utility.Considerations.Nutrition;
using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.Utility.Actions.Nutrition.Food
{
    public sealed class UseFoodInInventory : UtilityAction
    {
        private IEntity _entity;

        public UseFoodInInventory(IEntity owner, IEntity entity, float weight) : base(owner)
        {
            _entity = entity;
            Bonus = weight;
        }

        public override void SetupOperators(Blackboard context)
        {
            ActionOperators = new Queue<AiOperator>(new AiOperator[]
            {
                new EquipEntityOperator(Owner, _entity),
                new UseItemInHandsOperator(Owner, _entity),
            });
        }

        protected override Consideration[] Considerations => new Consideration[]
        {
            new TargetInOurHandsCon(
                new BoolCurve()),
            new HungerCon(
                new LogisticCurve(1000f, 1.3f, -0.3f, 0.5f)),
            new FoodValueCon(
                new QuadraticCurve(1.0f, 0.4f, 0.0f, 0.0f))
        };

        protected override void UpdateBlackboard(Blackboard context)
        {
            base.UpdateBlackboard(context);
            context.GetState<TargetEntityState>().SetValue(_entity);
        }
    }
}
