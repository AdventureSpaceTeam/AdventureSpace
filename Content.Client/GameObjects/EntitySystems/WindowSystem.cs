using System.Collections.Generic;
using Content.Client.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class WindowSystem : EntitySystem
    {
        private readonly Queue<IEntity> _dirtyEntities = new Queue<IEntity>();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeEvent<WindowSmoothDirtyEvent>(HandleDirtyEvent);
        }

        private void HandleDirtyEvent(object sender, WindowSmoothDirtyEvent ev)
        {
            if (sender is IEntity senderEnt && senderEnt.HasComponent<WindowComponent>())
            {
                _dirtyEntities.Enqueue(senderEnt);
            }
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            // Performance: This could be spread over multiple updates, or made parallel.
            while (_dirtyEntities.Count > 0)
            {
                var entity = _dirtyEntities.Dequeue();
                if (entity.Deleted)
                {
                    continue;
                }

                entity.GetComponent<WindowComponent>().UpdateSprite();
            }
        }
    }

    /// <summary>
    ///     Event raised by a <see cref="WindowComponent"/> when it needs to be recalculated.
    /// </summary>
    public sealed class WindowSmoothDirtyEvent : EntitySystemMessage
    {
    }
}
