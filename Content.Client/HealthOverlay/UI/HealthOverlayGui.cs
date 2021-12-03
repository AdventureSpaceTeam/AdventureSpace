using Content.Client.IoC;
using Content.Client.Resources;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.MobState.Components;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.HealthOverlay.UI
{
    public class HealthOverlayGui : BoxContainer
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;

        public HealthOverlayGui(IEntity entity)
        {
            IoCManager.InjectDependencies(this);
            IoCManager.Resolve<IUserInterfaceManager>().StateRoot.AddChild(this);
            SeparationOverride = 0;
            Orientation = LayoutOrientation.Vertical;

            CritBar = new HealthOverlayBar
            {
                Visible = false,
                VerticalAlignment = VAlignment.Center,
                Color = Color.Red
            };

            HealthBar = new HealthOverlayBar
            {
                Visible = false,
                VerticalAlignment = VAlignment.Center,
                Color = Color.LimeGreen
            };

            AddChild(Panel = new PanelContainer
            {
                Children =
                {
                    new TextureRect
                    {
                        Texture = StaticIoC.ResC.GetTexture("/Textures/Interface/Misc/health_bar.rsi/icon.png"),
                        TextureScale = Vector2.One * HealthOverlayBar.HealthBarScale,
                        VerticalAlignment = VAlignment.Center,
                    },
                    CritBar,
                    HealthBar
                }
            });

            Entity = entity;
        }

        public PanelContainer Panel { get; }

        public HealthOverlayBar HealthBar { get; }

        public HealthOverlayBar CritBar { get; }

        public IEntity Entity { get; }

        public void SetVisibility(bool val)
        {
            Visible = val;
            Panel.Visible = val;
        }

        private void MoreFrameUpdate(FrameEventArgs args)
        {
            if ((!IoCManager.Resolve<IEntityManager>().EntityExists(Entity.Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(Entity.Uid).EntityLifeStage) >= EntityLifeStage.Deleted)
            {
                return;
            }

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(Entity.Uid, out MobStateComponent? mobState) ||
                !IoCManager.Resolve<IEntityManager>().TryGetComponent(Entity.Uid, out DamageableComponent? damageable))
            {
                CritBar.Visible = false;
                HealthBar.Visible = false;
                return;
            }

            FixedPoint2 threshold;

            if (mobState.IsAlive())
            {
                if (!mobState.TryGetEarliestCriticalState(damageable.TotalDamage, out _, out threshold))
                {
                    CritBar.Visible = false;
                    HealthBar.Visible = false;
                    return;
                }

                CritBar.Ratio = 1;
                CritBar.Visible = true;
                HealthBar.Ratio = 1 - (damageable.TotalDamage / threshold).Float();
                HealthBar.Visible = true;
            }
            else if (mobState.IsCritical())
            {
                HealthBar.Ratio = 0;
                HealthBar.Visible = false;

                if (!mobState.TryGetPreviousCriticalState(damageable.TotalDamage, out _, out var critThreshold) ||
                    !mobState.TryGetEarliestDeadState(damageable.TotalDamage, out _, out var deadThreshold))
                {
                    CritBar.Visible = false;
                    return;
                }

                CritBar.Visible = true;
                CritBar.Ratio = 1 -
                    ((damageable.TotalDamage - critThreshold) /
                    (deadThreshold - critThreshold)).Float();
            }
            else if (mobState.IsDead())
            {
                CritBar.Ratio = 0;
                CritBar.Visible = false;
                HealthBar.Ratio = 0;
                HealthBar.Visible = true;
            }
            else
            {
                CritBar.Visible = false;
                HealthBar.Visible = false;
            }
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            MoreFrameUpdate(args);

            if ((!IoCManager.Resolve<IEntityManager>().EntityExists(Entity.Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(Entity.Uid).EntityLifeStage) >= EntityLifeStage.Deleted ||
                _eyeManager.CurrentMap != IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Entity.Uid).MapID)
            {
                Visible = false;
                return;
            }

            Visible = true;

            var screenCoordinates = _eyeManager.CoordinatesToScreen(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Entity.Uid).Coordinates);
            var playerPosition = UserInterfaceManager.ScreenToUIPosition(screenCoordinates);
            LayoutContainer.SetPosition(this, new Vector2(playerPosition.X - Width / 2, playerPosition.Y - Height - 30.0f));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            HealthBar.Dispose();
        }
    }
}
