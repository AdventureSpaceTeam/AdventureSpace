using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.WorldState.States
{
    /// <summary>
    /// Could be target item to equip, target to attack, etc.
    /// </summary>
    [UsedImplicitly]
    public sealed class TargetEntityState : PlanningStateData<IEntity>
    {
        public override string Name => "TargetEntity";

        public override void Reset()
        {
            Value = null;
        }
    }
}
