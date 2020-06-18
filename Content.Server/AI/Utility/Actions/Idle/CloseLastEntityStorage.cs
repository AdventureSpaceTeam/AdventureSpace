using System.Collections.Generic;
using Content.Server.AI.Operators;
using Content.Server.AI.Operators.Inventory;
using Content.Server.AI.Operators.Movement;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.Utility.Considerations.Movement;
using Content.Server.AI.Utility.Considerations.State;
using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Inventory;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.Utility.Actions.Idle
{
    /// <summary>
    /// If we just picked up a bunch of stuff and have time then close it
    /// </summary>
    public sealed class CloseLastEntityStorage : UtilityAction
    {
        public override float Bonus => 1.5f;
        
        public CloseLastEntityStorage(IEntity owner) : base(owner) {}

        protected override Consideration[] Considerations => new Consideration[]
        {
            new StoredStateIsNullCon<LastOpenedStorageState, IEntity>(
                new InverseBoolCurve()),
            new DistanceCon(
                new QuadraticCurve(1.0f, 1.0f, 0.02f, 0.0f)),
        };
        public override void SetupOperators(Blackboard context)
        {
            var lastStorage = context.GetState<LastOpenedStorageState>().GetValue();
            
            ActionOperators = new Queue<AiOperator>(new AiOperator[]
            {
                new MoveToEntityOperator(Owner, lastStorage),
                new CloseLastStorageOperator(Owner), 
            });
        }
    }
}