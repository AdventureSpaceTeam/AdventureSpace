﻿#nullable enable
using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Pulling;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Server.Physics.Controllers
{
    public class PullController : VirtualController
    {
        // Parameterization for pulling:
        // Speeds. Note that the speed is mass-independent (multiplied by mass).
        // Instead, tuning to mass is done via the mass values below.
        // Note that setting the speed too high results in overshoots (stabilized by drag, but bad)
        private const float AccelModifierHigh = 15f;
        private const float AccelModifierLow = 60.0f;
        // High/low-mass marks. Curve is constant-lerp-constant, i.e. if you can even pull an item,
        // you'll always get at least AccelModifierLow and no more than AccelModifierHigh.
        private const float AccelModifierHighMass = 70.0f; // roundstart saltern emergency closet
        private const float AccelModifierLowMass = 5.0f; // roundstart saltern emergency crowbar
        // Used to control settling (turns off pulling).
        private const float MaximumSettleVelocity = 0.1f;
        private const float MaximumSettleDistance = 0.01f;
        // Settle shutdown control.
        // Mustn't be too massive, as that causes severe mispredicts *and can prevent it ever resolving*.
        // Exists to bleed off "I pulled my crowbar" overshoots.
        // Minimum velocity for shutdown to be necessary. This prevents stuff getting stuck b/c too much shutdown.
        private const float SettleMinimumShutdownVelocity = 0.25f;
        // Distance in which settle shutdown multiplier is at 0. It then scales upwards linearly with closer distances.
        private const float SettleShutdownDistance = 1.0f;
        // Velocity change of -LinearVelocity * frameTime * this
        private const float SettleShutdownMultiplier = 20.0f;

        private SharedPullingSystem _pullableSystem = default!;

        public override List<Type> UpdatesAfter => new() {typeof(MoverController)};

        public override void Initialize()
        {
            base.Initialize();

            _pullableSystem = EntitySystem.Get<SharedPullingSystem>();
        }

        public override void UpdateBeforeSolve(bool prediction, float frameTime)
        {
            base.UpdateBeforeSolve(prediction, frameTime);

            foreach (var pullable in _pullableSystem.Moving)
            {
                if (pullable.Deleted)
                {
                    continue;
                }

                if (pullable.MovingTo == null)
                {
                    continue;
                }

                DebugTools.AssertNotNull(pullable.Puller);

                var pullerPosition = pullable.Puller!.Transform.MapPosition;
                if (pullable.MovingTo.Value.MapId != pullerPosition.MapId)
                {
                    pullable.MovingTo = null;
                    continue;
                }

                if (!pullable.Owner.TryGetComponent<PhysicsComponent>(out var physics) ||
                    physics.BodyType == BodyType.Static ||
                    pullable.MovingTo.Value.MapId != pullable.Owner.Transform.MapID)
                {
                    pullable.MovingTo = null;
                    continue;
                }

                var movingPosition = pullable.MovingTo.Value.Position;
                var ownerPosition = pullable.Owner.Transform.MapPosition.Position;

                if (movingPosition.EqualsApprox(ownerPosition, MaximumSettleDistance) && (physics.LinearVelocity.Length < MaximumSettleVelocity))
                {
                    physics.LinearVelocity = Vector2.Zero;
                    pullable.MovingTo = null;
                    continue;
                }

                var diff = movingPosition - ownerPosition;
                var diffLength = diff.Length;
                var impulseModifierLerp = Math.Min(1.0f, Math.Max(0.0f, (physics.Mass - AccelModifierLowMass) / (AccelModifierHighMass - AccelModifierLowMass)));
                var impulseModifier = MathHelper.Lerp(AccelModifierLow, AccelModifierHigh, impulseModifierLerp);
                var multiplier = diffLength < 1 ? impulseModifier * diffLength : impulseModifier;
                // Note the implication that the real rules of physics don't apply to pulling control.
                var accel = diff.Normalized * multiplier;
                // Now for the part where velocity gets shutdown...
                if ((diffLength < SettleShutdownDistance) && (physics.LinearVelocity.Length >= SettleMinimumShutdownVelocity))
                {
                    // Shutdown velocity increases as we get closer to centre
                    var scaling = (SettleShutdownDistance - diffLength) / SettleShutdownDistance;
                    accel -= physics.LinearVelocity * SettleShutdownMultiplier * scaling;
                }
                physics.ApplyLinearImpulse(accel * physics.Mass * frameTime);
            }
        }
    }
}
