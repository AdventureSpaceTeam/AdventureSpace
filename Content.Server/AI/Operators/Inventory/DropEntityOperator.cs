using Content.Server.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Log;

namespace Content.Server.AI.Operators.Inventory
{
    public class DropEntityOperator : AiOperator
    {
        private readonly IEntity _owner;
        private readonly IEntity _entity;
        public DropEntityOperator(IEntity owner, IEntity entity)
        {
            _owner = owner;
            _entity = entity;
        }

        /// <summary>
        /// Requires EquipEntityOperator to put it in the active hand first
        /// </summary>
        /// <param name="frameTime"></param>
        /// <returns></returns>
        public override Outcome Execute(float frameTime)
        {
            if (!_owner.TryGetComponent(out HandsComponent handsComponent) || handsComponent.FindHand(_entity) == null)
            {
                return Outcome.Failed;
            }

            return handsComponent.Drop(_entity) ? Outcome.Success : Outcome.Failed;
        }
    }
}
