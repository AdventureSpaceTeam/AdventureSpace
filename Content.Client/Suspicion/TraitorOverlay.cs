using Content.Shared.Examine;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Client.Suspicion
{
    public class TraitorOverlay : Overlay
    {
        private readonly IEntityManager _entityManager;
        private readonly IEyeManager _eyeManager;
        private readonly IPlayerManager _playerManager;

        public override OverlaySpace Space => OverlaySpace.ScreenSpace;
        private readonly Font _font;

        private readonly string _traitorText = Loc.GetString("traitor-overlay-traitor-text");

        public TraitorOverlay(
            IEntityManager entityManager,
            IResourceCache resourceCache,
            IEyeManager eyeManager)
        {
            _playerManager = IoCManager.Resolve<IPlayerManager>();

            _entityManager = entityManager;
            _eyeManager = eyeManager;

            _font = new VectorFont(resourceCache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 10);
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            var viewport = _eyeManager.GetWorldViewport();

            var ent = _playerManager.LocalPlayer?.ControlledEntity;
            if (ent == null || IoCManager.Resolve<IEntityManager>().TryGetComponent(ent.Uid, out SuspicionRoleComponent? sus) != true)
            {
                return;
            }

            foreach (var (_, uid) in sus.Allies)
            {
                // Otherwise the entity can not exist yet
                if (!_entityManager.TryGetEntity(uid, out var ally))
                {
                    continue;
                }

                if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(ally.Uid, out IPhysBody? physics))
                {
                    continue;
                }

                if (!ExamineSystemShared.InRangeUnOccluded(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent.Uid).MapPosition, IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ally.Uid).MapPosition, 15,
                    entity => entity == ent || entity == ally))
                {
                    continue;
                }

                // if not on the same map, continue
                if (IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(physics.Owner.Uid).MapID != _eyeManager.CurrentMap || physics.Owner.IsInContainer())
                {
                    continue;
                }

                var worldBox = physics.GetWorldAABB();

                // if not on screen, or too small, continue
                if (!worldBox.Intersects(in viewport) || worldBox.IsEmpty())
                {
                    continue;
                }

                var screenCoordinates = args.ViewportControl!.WorldToScreen(physics.GetWorldAABB().TopLeft + (0, 0.5f));
                args.ScreenHandle.DrawString(_font, screenCoordinates, _traitorText, Color.OrangeRed);
            }
        }
    }
}
