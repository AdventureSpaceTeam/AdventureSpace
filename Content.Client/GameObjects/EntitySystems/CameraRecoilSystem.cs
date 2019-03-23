using Content.Client.GameObjects.Components.Mobs;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Systems;

namespace Content.Client.GameObjects.EntitySystems
{
    public sealed class CameraRecoilSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            EntityQuery = new TypeEntityQuery(typeof(CameraRecoilComponent));
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            foreach (var entity in RelevantEntities)
            {
                var recoil = entity.GetComponent<CameraRecoilComponent>();
                recoil.FrameUpdate(frameTime);
            }
        }
    }
}
