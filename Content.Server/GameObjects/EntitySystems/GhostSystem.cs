using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Observer;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class GhostSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GhostComponent, MindRemovedMessage>(OnMindRemovedMessage);
            SubscribeLocalEvent<GhostComponent, MindUnvisitedMessage>(OnMindUnvisitedMessage);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeLocalEvent<GhostComponent, MindRemovedMessage>(OnMindRemovedMessage);
            UnsubscribeLocalEvent<GhostComponent, MindUnvisitedMessage>(OnMindUnvisitedMessage);
        }

        private void OnMindRemovedMessage(EntityUid uid, GhostComponent component, MindRemovedMessage args)
        {
            if (!EntityManager.TryGetEntity(uid, out var entity))
                return;
            DeleteEntity(entity);
        }

        private void OnMindUnvisitedMessage(EntityUid uid, GhostComponent component, MindUnvisitedMessage args)
        {
            if (!EntityManager.TryGetEntity(uid, out var entity))
                return;
            DeleteEntity(entity);
        }

        private void DeleteEntity(IEntity? entity)
        {
            if (entity?.Deleted == true)
                return;

            entity?.Delete();
        }
    }
}
