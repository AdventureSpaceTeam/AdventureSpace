﻿using System;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.Components.Animations;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Animations;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components.Mobs
{
    public class SpeciesVisualizer2D : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.TryGetData<SharedSpeciesComponent.MobState>(SharedSpeciesComponent.MobVisuals.RotationState, out var state))
            {
                switch (state)
                {
                    case SharedSpeciesComponent.MobState.Standing:
                        SetRotation(component, 0);
                        break;
                    case SharedSpeciesComponent.MobState.Down:
                        SetRotation(component, Angle.FromDegrees(90));
                        break;
                }
            }
        }

        private void SetRotation(AppearanceComponent component, Angle rotation)
        {
            var sprite = component.Owner.GetComponent<ISpriteComponent>();

            if (!sprite.Owner.TryGetComponent(out AnimationPlayerComponent animation))
            {
                sprite.Rotation = rotation;
                return;
            }

            if (animation.HasRunningAnimation("rotate"))
                animation.Stop("rotate");

            animation.Play(new Animation
            {
                Length = TimeSpan.FromSeconds(0.125),
                AnimationTracks =
                {
                    new AnimationTrackComponentProperty
                    {
                        ComponentType = typeof(ISpriteComponent),
                        Property = nameof(ISpriteComponent.Rotation),
                        InterpolationMode = AnimationInterpolationMode.Linear,
                        KeyFrames =
                        {
                            new AnimationTrackProperty.KeyFrame(sprite.Rotation, 0),
                            new AnimationTrackProperty.KeyFrame(rotation, 0.125f)
                        }
                    }
                }
            }, "rotate");
        }
    }
}
