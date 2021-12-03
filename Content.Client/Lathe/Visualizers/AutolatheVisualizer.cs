using System;
using Content.Shared.Lathe;
using Content.Shared.Power;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Lathe.Visualizers
{
    [UsedImplicitly]
    public class AutolatheVisualizer : AppearanceVisualizer
    {
        private const string AnimationKey = "inserting_animation";

        private Animation _buildingAnimation;
        private Animation _insertingMetalAnimation;
        private Animation _insertingGlassAnimation;
        private Animation _insertingGoldAnimation;
        private Animation _insertingPlasmaAnimation;
        private Animation _insertingPlasticAnimation;

        public AutolatheVisualizer()
        {
            _buildingAnimation = PopulateAnimation("building", "building_unlit", 0.5f);
            _insertingMetalAnimation = PopulateAnimation("inserting_metal", "inserting_unlit", 0.5f);
            _insertingGlassAnimation = PopulateAnimation("inserting_glass", "inserting_unlit", 0.5f);
            _insertingGoldAnimation = PopulateAnimation("inserting_gold", "inserting_unlit", 0.5f);
            _insertingPlasmaAnimation = PopulateAnimation("inserting_plasma", "inserting_unlit", 0.5f);
            _insertingPlasticAnimation = PopulateAnimation("inserting_plastic", "inserting_unlit", 0.5f);
        }

        private Animation PopulateAnimation(string sprite, string spriteUnlit, float length)
        {
            var animation = new Animation {Length = TimeSpan.FromSeconds(length)};

            var flick = new AnimationTrackSpriteFlick();
            animation.AnimationTracks.Add(flick);
            flick.LayerKey = AutolatheVisualLayers.Base;
            flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(sprite, 0f));

            var flickUnlit = new AnimationTrackSpriteFlick();
            animation.AnimationTracks.Add(flickUnlit);
            flickUnlit.LayerKey = AutolatheVisualLayers.BaseUnlit;
            flickUnlit.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(spriteUnlit, 0f));

            return animation;
        }

        public override void InitializeEntity(IEntity entity)
        {
            if (!IoCManager.Resolve<IEntityManager>().HasComponent<AnimationPlayerComponent>(entity))
            {
                IoCManager.Resolve<IEntityManager>().AddComponent<AnimationPlayerComponent>(entity);
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(component.Owner);
            var animPlayer = IoCManager.Resolve<IEntityManager>().GetComponent<AnimationPlayerComponent>(component.Owner);
            if (!component.TryGetData(PowerDeviceVisuals.VisualState, out LatheVisualState state))
            {
                state = LatheVisualState.Idle;
            }
            sprite.LayerSetVisible(AutolatheVisualLayers.AnimationLayer, true);
            switch (state)
            {
                case LatheVisualState.Idle:
                    if (animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Stop(AnimationKey);
                    }

                    sprite.LayerSetState(AutolatheVisualLayers.Base, "icon");
                    sprite.LayerSetState(AutolatheVisualLayers.BaseUnlit, "unlit");
                    sprite.LayerSetVisible(AutolatheVisualLayers.AnimationLayer, false);
                    break;
                case LatheVisualState.Producing:
                    if (!animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Play(_buildingAnimation, AnimationKey);
                    }
                    break;
                case LatheVisualState.InsertingMetal:
                    if (!animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Play(_insertingMetalAnimation, AnimationKey);
                    }
                    break;
                case LatheVisualState.InsertingGlass:
                    if (!animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Play(_insertingGlassAnimation, AnimationKey);
                    }
                    break;
                case LatheVisualState.InsertingGold:
                    if (!animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Play(_insertingGoldAnimation, AnimationKey);
                    }
                    break;
                case LatheVisualState.InsertingPlasma:
                    if (!animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Play(_insertingPlasmaAnimation, AnimationKey);
                    }
                    break;
                case LatheVisualState.InsertingPlastic:
                    if (!animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Play(_insertingPlasticAnimation, AnimationKey);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var glowingPartsVisible = !(component.TryGetData(PowerDeviceVisuals.Powered, out bool powered) && !powered);
            sprite.LayerSetVisible(AutolatheVisualLayers.BaseUnlit, glowingPartsVisible);
        }

        public enum AutolatheVisualLayers : byte
        {
            Base,
            BaseUnlit,
            AnimationLayer
        }
    }
}
