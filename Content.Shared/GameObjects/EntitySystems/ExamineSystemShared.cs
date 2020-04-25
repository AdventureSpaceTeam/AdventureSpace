using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Mobs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Shared.GameObjects.EntitySystems
{
    public abstract class ExamineSystemShared : EntitySystem
    {
        public const float ExamineRange = 16f;
        public const float ExamineRangeSquared = ExamineRange * ExamineRange;

        [Pure]
        protected static bool CanExamine(IEntity examiner, IEntity examined)
        {
            if (!examiner.TryGetComponent(out ExaminerComponent examinerComponent))
            {
                return false;
            }

            if (!examinerComponent.DoRangeCheck)
            {
                return true;
            }

            if (examiner.Transform.MapID != examined.Transform.MapID)
            {
                return false;
            }

            return IoCManager.Resolve<IEntitySystemManager>()
                .GetEntitySystem<SharedInteractionSystem>()
                .InRangeUnobstructed(examiner.Transform.MapPosition, examined.Transform.MapPosition.Position,
                    ExamineRange, predicate: entity => entity == examiner || entity == examined, insideBlockerValid:true);
        }
    }
}
