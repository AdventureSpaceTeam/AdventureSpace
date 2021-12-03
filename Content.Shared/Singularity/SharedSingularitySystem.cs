﻿using System;
using Content.Shared.Radiation;
using Content.Shared.Singularity.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;

namespace Content.Shared.Singularity
{
    public abstract class SharedSingularitySystem : EntitySystem
    {
        [Dependency] private readonly FixtureSystem _fixtures = default!;

        public const string DeleteFixture = "DeleteCircle";

        private float GetFalloff(int level)
        {
            return level switch
            {
                0 => 9999f,
                1 => 6.4f,
                2 => 7.0f,
                3 => 8.0f,
                4 => 10.0f,
                5 => 12.0f,
                6 => 12.0f,
                _ => -1.0f
            };
        }

        private float GetIntensity(int level)
        {
            return level switch
            {
                0 => 0.0f,
                1 => 2.7f,
                2 => 14.4f,
                3 => 47.2f,
                4 => 180.0f,
                5 => 600.0f,
                6 => 800.0f,
                _ => -1.0f
            };
        }

        public void ChangeSingularityLevel(SharedSingularityComponent singularity, int value)
        {
            if (value == singularity.Level)
            {
                return;
            }

            value = Math.Clamp(value, 0, 6);

            var physics = singularity.Owner.GetComponentOrNull<PhysicsComponent>();

            if (singularity.Level > 1 && value <= 1)
            {
                // Prevents it getting stuck (see SingularityController.MoveSingulo)
                if (physics != null)
                {
                    physics.LinearVelocity = Vector2.Zero;
                }
            }

            singularity.Level = value;

            if (singularity.Owner.TryGetComponent(out SharedRadiationPulseComponent? pulse))
            {
                pulse.RadsPerSecond = 10 * value;
            }

            if (singularity.Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(SingularityVisuals.Level, value);
            }

            if (physics != null && _fixtures.GetFixtureOrNull(physics, DeleteFixture) is {Shape: PhysShapeCircle circle})
            {
                circle.Radius = value - 0.5f;
            }

            if (singularity.Owner.TryGetComponent(out SingularityDistortionComponent? distortion))
            {
                distortion.Falloff = GetFalloff(value);
                distortion.Intensity = GetIntensity(value);
            }

            singularity.Dirty();
        }

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedSingularityComponent, PreventCollideEvent>(HandleFieldCollision);
        }

        private void HandleFieldCollision(EntityUid uid, SharedSingularityComponent component, PreventCollideEvent args)
        {
            var other = args.BodyB.Owner;

            if ((!IoCManager.Resolve<IEntityManager>().HasComponent<SharedContainmentFieldComponent>(other.Uid) &&
                !IoCManager.Resolve<IEntityManager>().HasComponent<SharedContainmentFieldGeneratorComponent>(other.Uid)) ||
                component.Level >= 4)
            {
                args.Cancel();
                return;
            }
        }
    }
}
