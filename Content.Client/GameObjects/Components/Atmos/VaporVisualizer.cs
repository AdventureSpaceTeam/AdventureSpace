﻿using System;
using Content.Shared.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.GameObjects.Components.Atmos
{
    [UsedImplicitly]
    public class VaporVisualizer : AppearanceVisualizer, ISerializationHooks
    {
        private const string AnimationKey = "flick_animation";

        [DataField("animation_time")]
        private float _delay = 0.25f;

        [DataField("animation_state")]
        private string _state = "chempuff";

        private Animation VaporFlick;

        void ISerializationHooks.AfterDeserialization()
        {
            VaporFlick = new Animation {Length = TimeSpan.FromSeconds(_delay)};
            {
                var flick = new AnimationTrackSpriteFlick();
                VaporFlick.AnimationTracks.Add(flick);
                flick.LayerKey = VaporVisualLayers.Base;
                flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(_state, 0f));
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.Deleted)
            {
                return;
            }

            if (component.TryGetData<Angle>(VaporVisuals.Rotation, out var radians))
            {
                SetRotation(component, radians);
            }

            if (component.TryGetData<Color>(VaporVisuals.Color, out var color))
            {
                SetColor(component, color);
            }

            if (component.TryGetData<bool>(VaporVisuals.State, out var state))
            {
                SetState(component, state);
            }
        }

        private void SetState(AppearanceComponent component, bool state)
        {
            if (!state) return;

            var animPlayer = component.Owner.GetComponent<AnimationPlayerComponent>();

            if(!animPlayer.HasRunningAnimation(AnimationKey))
                animPlayer.Play(VaporFlick, AnimationKey);
        }

        private void SetRotation(AppearanceComponent component, Angle rotation)
        {
            var sprite = component.Owner.GetComponent<ISpriteComponent>();

            sprite.Rotation = rotation;
        }

        private void SetColor(AppearanceComponent component, Color color)
        {
            var sprite = component.Owner.GetComponent<ISpriteComponent>();

            sprite.Color = color;
        }
    }

    public enum VaporVisualLayers : byte
    {
        Base
    }
}
