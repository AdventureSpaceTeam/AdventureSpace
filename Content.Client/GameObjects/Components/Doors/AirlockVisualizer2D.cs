using System;
using Content.Shared.GameObjects.Components.Doors;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.Components.Animations;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Doors
{
    public class AirlockVisualizer2D : AppearanceVisualizer
    {
        private const string AnimationKey = "airlock_animation";

        private Animation CloseAnimation;
        private Animation OpenAnimation;
        private Animation DenyAnimation;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            var openSound = node.GetNode("open_sound").AsString();
            var closeSound = node.GetNode("close_sound").AsString();
            var denySound = node.GetNode("deny_sound").AsString();

            CloseAnimation = new Animation {Length = TimeSpan.FromSeconds(1.2f)};
            {
                var flick = new AnimationTrackSpriteFlick();
                CloseAnimation.AnimationTracks.Add(flick);
                flick.LayerKey = DoorVisualLayers.Base;
                flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("closing", 0f));

                var flickUnlit = new AnimationTrackSpriteFlick();
                CloseAnimation.AnimationTracks.Add(flickUnlit);
                flickUnlit.LayerKey = DoorVisualLayers.BaseUnlit;
                flickUnlit.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("closing_unlit", 0f));

                var sound = new AnimationTrackPlaySound();
                CloseAnimation.AnimationTracks.Add(sound);
                sound.KeyFrames.Add(new AnimationTrackPlaySound.KeyFrame(closeSound, 0));
            }

            OpenAnimation = new Animation {Length = TimeSpan.FromSeconds(1.2f)};
            {
                var flick = new AnimationTrackSpriteFlick();
                OpenAnimation.AnimationTracks.Add(flick);
                flick.LayerKey = DoorVisualLayers.Base;
                flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("opening", 0f));

                var flickUnlit = new AnimationTrackSpriteFlick();
                OpenAnimation.AnimationTracks.Add(flickUnlit);
                flickUnlit.LayerKey = DoorVisualLayers.BaseUnlit;
                flickUnlit.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("opening_unlit", 0f));

                var sound = new AnimationTrackPlaySound();
                OpenAnimation.AnimationTracks.Add(sound);
                sound.KeyFrames.Add(new AnimationTrackPlaySound.KeyFrame(openSound, 0));
            }

            DenyAnimation = new Animation {Length = TimeSpan.FromSeconds(0.45f)};
            {
                var flick = new AnimationTrackSpriteFlick();
                DenyAnimation.AnimationTracks.Add(flick);
                flick.LayerKey = DoorVisualLayers.Base;
                flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("deny", 0f));

                var sound = new AnimationTrackPlaySound();
                DenyAnimation.AnimationTracks.Add(sound);
                sound.KeyFrames.Add(new AnimationTrackPlaySound.KeyFrame(denySound, 0));
            }
        }

        public override void InitializeEntity(IEntity entity)
        {
            if (!entity.HasComponent<AnimationPlayerComponent>())
            {
                entity.AddComponent<AnimationPlayerComponent>();
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            var animPlayer = component.Owner.GetComponent<AnimationPlayerComponent>();
            if (!component.TryGetData(DoorVisuals.VisualState, out DoorVisualState state))
            {
                state = DoorVisualState.Closed;
            }

            switch (state)
            {
                case DoorVisualState.Closed:
                    sprite.LayerSetState(DoorVisualLayers.Base, "closed");
                    sprite.LayerSetState(DoorVisualLayers.BaseUnlit, "closed_unlit");
                    sprite.LayerSetVisible(DoorVisualLayers.BaseUnlit, true);
                    break;
                case DoorVisualState.Closing:
                    sprite.LayerSetVisible(DoorVisualLayers.BaseUnlit, true);
                    if (!animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Play(CloseAnimation, AnimationKey);
                    }
                    break;
                case DoorVisualState.Opening:
                    sprite.LayerSetVisible(DoorVisualLayers.BaseUnlit, true);
                    if (!animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Play(OpenAnimation, AnimationKey);
                    }

                    break;
                case DoorVisualState.Open:
                    sprite.LayerSetState(DoorVisualLayers.Base, "open");
                    sprite.LayerSetVisible(DoorVisualLayers.BaseUnlit, false);
                    break;
                case DoorVisualState.Deny:
                    sprite.LayerSetVisible(DoorVisualLayers.BaseUnlit, false);
                    if (!animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Play(DenyAnimation, AnimationKey);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public enum DoorVisualLayers
    {
        Base,
        BaseUnlit
    }
}
