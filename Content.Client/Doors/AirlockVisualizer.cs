using System;
using Content.Client.Wires.Visualizers;
using Content.Shared.Doors;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Doors
{
    [UsedImplicitly]
    public class AirlockVisualizer : AppearanceVisualizer, ISerializationHooks
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        private const string AnimationKey = "airlock_animation";

        [DataField("animationTime")]
        private float _delay = 0.8f;

        [DataField("denyAnimationTime")]
        private float _denyDelay = 0.3f;

        /// <summary>
        ///     Whether the maintenance panel is animated or stays static.
        ///     False for windoors.
        /// </summary>
        [DataField("animatedPanel")]
        private bool _animatedPanel = true;

        /// <summary>
        /// Means the door is simply open / closed / opening / closing. No wires or access.
        /// </summary>
        [DataField("simpleVisuals")]
        private bool _simpleVisuals = false;

        /// <summary>
        ///     Whether the BaseUnlit layer should still be visible when the airlock
        ///     is opened.
        /// </summary>
        [DataField("openUnlitVisible")]
        private bool _openUnlitVisible = false;

        private Animation CloseAnimation = default!;
        private Animation OpenAnimation = default!;
        private Animation DenyAnimation = default!;

        void ISerializationHooks.AfterDeserialization()
        {
            IoCManager.InjectDependencies(this);

            CloseAnimation = new Animation {Length = TimeSpan.FromSeconds(_delay)};
            {
                var flick = new AnimationTrackSpriteFlick();
                CloseAnimation.AnimationTracks.Add(flick);
                flick.LayerKey = DoorVisualLayers.Base;
                flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("closing", 0f));

                if (!_simpleVisuals)
                {
                    var flickUnlit = new AnimationTrackSpriteFlick();
                    CloseAnimation.AnimationTracks.Add(flickUnlit);
                    flickUnlit.LayerKey = DoorVisualLayers.BaseUnlit;
                    flickUnlit.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("closing_unlit", 0f));

                    if (_animatedPanel)
                    {
                        var flickMaintenancePanel = new AnimationTrackSpriteFlick();
                        CloseAnimation.AnimationTracks.Add(flickMaintenancePanel);
                        flickMaintenancePanel.LayerKey = WiresVisualizer.WiresVisualLayers.MaintenancePanel;
                        flickMaintenancePanel.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("panel_closing", 0f));
                    }
                }
            }

            OpenAnimation = new Animation {Length = TimeSpan.FromSeconds(_delay)};
            {
                var flick = new AnimationTrackSpriteFlick();
                OpenAnimation.AnimationTracks.Add(flick);
                flick.LayerKey = DoorVisualLayers.Base;
                flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("opening", 0f));

                if (!_simpleVisuals)
                {
                    var flickUnlit = new AnimationTrackSpriteFlick();
                    OpenAnimation.AnimationTracks.Add(flickUnlit);
                    flickUnlit.LayerKey = DoorVisualLayers.BaseUnlit;
                    flickUnlit.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("opening_unlit", 0f));

                    if (_animatedPanel)
                    {
                        var flickMaintenancePanel = new AnimationTrackSpriteFlick();
                        OpenAnimation.AnimationTracks.Add(flickMaintenancePanel);
                        flickMaintenancePanel.LayerKey = WiresVisualizer.WiresVisualLayers.MaintenancePanel;
                        flickMaintenancePanel.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("panel_opening", 0f));
                    }
                }
            }

            if (!_simpleVisuals)
            {
                DenyAnimation = new Animation {Length = TimeSpan.FromSeconds(_denyDelay)};
                {
                    var flick = new AnimationTrackSpriteFlick();
                    DenyAnimation.AnimationTracks.Add(flick);
                    flick.LayerKey = DoorVisualLayers.BaseUnlit;
                    flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("deny_unlit", 0f));
                }
            }
        }

        public override void InitializeEntity(EntityUid entity)
        {
            if (!_entMan.HasComponent<AnimationPlayerComponent>(entity))
            {
                _entMan.AddComponent<AnimationPlayerComponent>(entity);
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = _entMan.GetComponent<ISpriteComponent>(component.Owner);
            var animPlayer = _entMan.GetComponent<AnimationPlayerComponent>(component.Owner);
            if (!component.TryGetData(DoorVisuals.VisualState, out DoorVisualState state))
            {
                state = DoorVisualState.Closed;
            }

            var unlitVisible = true;
            var boltedVisible = false;
            var weldedVisible = false;

            if (animPlayer.HasRunningAnimation(AnimationKey))
            {
                animPlayer.Stop(AnimationKey);
            }
            switch (state)
            {
                case DoorVisualState.Open:
                    sprite.LayerSetState(DoorVisualLayers.Base, "open");
                    unlitVisible = _openUnlitVisible;
                    if (_openUnlitVisible && !_simpleVisuals)
                    {
                        sprite.LayerSetState(DoorVisualLayers.BaseUnlit, "open_unlit");
                    }
                    break;
                case DoorVisualState.Closed:
                    sprite.LayerSetState(DoorVisualLayers.Base, "closed");
                    if (!_simpleVisuals)
                    {
                        sprite.LayerSetState(DoorVisualLayers.BaseUnlit, "closed_unlit");
                        sprite.LayerSetState(DoorVisualLayers.BaseBolted, "bolted_unlit");
                    }
                    break;
                case DoorVisualState.Opening:
                    animPlayer.Play(OpenAnimation, AnimationKey);
                    break;
                case DoorVisualState.Closing:
                    animPlayer.Play(CloseAnimation, AnimationKey);
                    break;
                case DoorVisualState.Deny:
                    if (!animPlayer.HasRunningAnimation(AnimationKey))
                        animPlayer.Play(DenyAnimation, AnimationKey);
                    break;
                case DoorVisualState.Welded:
                    weldedVisible = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (component.TryGetData(DoorVisuals.Powered, out bool powered) && !powered)
            {
                unlitVisible = false;
            }
            if (component.TryGetData(DoorVisuals.BoltLights, out bool lights) && lights)
            {
                boltedVisible = true;
            }

            if (!_simpleVisuals)
            {
                sprite.LayerSetVisible(DoorVisualLayers.BaseUnlit, unlitVisible && state != DoorVisualState.Closed);
                sprite.LayerSetVisible(DoorVisualLayers.BaseWelded, weldedVisible);
                sprite.LayerSetVisible(DoorVisualLayers.BaseBolted, unlitVisible && boltedVisible);
            }
        }
    }

    public enum DoorVisualLayers : byte
    {
        Base,
        BaseUnlit,
        BaseWelded,
        BaseBolted,
    }
}
