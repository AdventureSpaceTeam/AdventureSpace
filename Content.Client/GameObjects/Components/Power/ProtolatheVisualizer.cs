﻿using System;
using Content.Shared.GameObjects.Components.Power;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.Components.Animations;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Power
{
    public class ProtolatheVisualizer : AppearanceVisualizer
    {
        private const string AnimationKey = "protolathe_animation";

        private Animation _buildingAnimation;
        private Animation _insertingMetalAnimation;
        private Animation _insertingGlassAnimation;
        private Animation _insertingGoldAnimation;
        private Animation _insertingPhoronAnimation;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            _buildingAnimation = PopulateAnimation("protolathe_building", 0.9f);
            _insertingMetalAnimation = PopulateAnimation("protolathe_metal", 0.9f);
            _insertingGlassAnimation = PopulateAnimation("protolathe_glass", 0.9f);
            _insertingGoldAnimation = PopulateAnimation("protolathe_gold", 0.9f);
            _insertingPhoronAnimation = PopulateAnimation("protolathe_phoron", 0.9f);
        }

        private Animation PopulateAnimation(string sprite, float length)
        {
            var animation = new Animation {Length = TimeSpan.FromSeconds(length)};

            var flick = new AnimationTrackSpriteFlick();
            animation.AnimationTracks.Add(flick);
            flick.LayerKey = ProtolatheVisualLayers.AnimationLayer;
            flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(sprite, 0f));

            return animation;
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
            base.OnChangeData(component);

            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            var animPlayer = component.Owner.GetComponent<AnimationPlayerComponent>();
            if (!component.TryGetData(PowerDeviceVisuals.VisualState, out LatheVisualState state))
            {
                state = LatheVisualState.Idle;
            }
            sprite.LayerSetVisible(ProtolatheVisualLayers.AnimationLayer, true);
            switch (state)
            {
                case LatheVisualState.Idle:
                    if (animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Stop(AnimationKey);
                    }

                    sprite.LayerSetState(ProtolatheVisualLayers.Base, "protolathe");
                    sprite.LayerSetState(ProtolatheVisualLayers.BaseUnlit, "protolathe_unlit");
                    sprite.LayerSetVisible(ProtolatheVisualLayers.AnimationLayer, false);
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
                case LatheVisualState.InsertingPhoron:
                    if (!animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Play(_insertingPhoronAnimation, AnimationKey);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var glowingPartsVisible = !(component.TryGetData(PowerDeviceVisuals.Powered, out bool powered) && !powered);
            sprite.LayerSetVisible(ProtolatheVisualLayers.BaseUnlit, glowingPartsVisible);
        }
        public enum ProtolatheVisualLayers
        {
            Base,
            BaseUnlit,
            AnimationLayer
        }
    }
}
