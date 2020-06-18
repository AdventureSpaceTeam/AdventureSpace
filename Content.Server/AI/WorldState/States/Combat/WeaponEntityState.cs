using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.WorldState.States.Combat
{
    [UsedImplicitly]
    public sealed class WeaponEntityState : PlanningStateData<IEntity>
    {
        // Similar to TargetEntity
        public override string Name => "WeaponEntity";
        public override void Reset()
        {
            Value = null;
        }
    }
}
