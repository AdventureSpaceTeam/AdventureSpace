using System.Collections.Generic;
using Content.Server.AI.Operators;
using Content.Server.AI.Operators.Movement;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.AI.Utility.Actions.Test
{
    /// <summary>
    /// Used for pathfinding debugging
    /// </summary>
    public class MoveRightAndLeftTen : UtilityAction
    {
        public override bool CanOverride => false;

        public MoveRightAndLeftTen(IEntity owner) : base(owner) {}

        protected override Consideration[] Considerations { get; } = {
            new DummyCon(
                new BoolCurve())
        };

        public override void SetupOperators(Blackboard context)
        {
            var currentPosition = Owner.Transform.GridPosition;
            var nextPosition = Owner.Transform.GridPosition.Offset(new Vector2(10.0f, 0.0f));
            var originalPosOp = new MoveToGridOperator(Owner, currentPosition, 0.25f);
            var newPosOp = new MoveToGridOperator(Owner, nextPosition, 0.25f);

            ActionOperators = new Queue<AiOperator>(new AiOperator[]
            {
                newPosOp,
                originalPosOp
            });
        }
    }
}
