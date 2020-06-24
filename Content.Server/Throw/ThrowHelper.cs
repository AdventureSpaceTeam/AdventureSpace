﻿using Content.Server.GameObjects.Components;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Random;
using System;
using Robust.Shared.Interfaces.Physics;
using CannyFastMath;
using Math = CannyFastMath.Math;
using MathF = CannyFastMath.MathF;

namespace Content.Server.Throw
{
    public static class ThrowHelper
    {
        /// <summary>
        ///     Throw an entity in the direction of <paramref name="targetLoc"/> from <paramref name="sourceLoc"/>.
        /// </summary>
        /// <param name="thrownEnt">The entity to throw.</param>
        /// <param name="throwForce">
        /// The force to throw the entity with.
        /// Total impulse applied is equal to this force applied for one second.
        /// </param>
        /// <param name="targetLoc">
        /// The target location to throw at.
        /// This is only used to calculate a direction,
        /// actual distance is purely determined by <paramref name="throwForce"/>.
        /// </param>
        /// <param name="sourceLoc">
        /// The position to start the throw from.
        /// </param>
        /// <param name="spread">
        /// If true, slightly spread the actual throw angle.
        /// </param>
        /// <param name="throwSourceEnt">
        /// The entity that did the throwing. An opposite impulse will be applied to this entity if passed in.
        /// </param>
        public static void Throw(IEntity thrownEnt, float throwForce, GridCoordinates targetLoc, GridCoordinates sourceLoc, bool spread = false, IEntity throwSourceEnt = null)
        {
            if (!thrownEnt.TryGetComponent(out CollidableComponent colComp))
                return;

            var mapManager = IoCManager.Resolve<IMapManager>();

            colComp.CanCollide = true;
            // I can now collide with player, so that i can do damage.

            if (!thrownEnt.TryGetComponent(out ThrownItemComponent projComp))
            {
                projComp = thrownEnt.AddComponent<ThrownItemComponent>();

                if (colComp.PhysicsShapes.Count == 0)
                    colComp.PhysicsShapes.Add(new PhysShapeAabb());

                colComp.PhysicsShapes[0].CollisionMask |= (int) (CollisionGroup.MobImpassable | CollisionGroup.Impassable);
                colComp.Status = BodyStatus.InAir;
            }
            var angle = new Angle(targetLoc.ToMapPos(mapManager) - sourceLoc.ToMapPos(mapManager));

            if (spread)
            {
                var spreadRandom = IoCManager.Resolve<IRobustRandom>();
                angle += Angle.FromDegrees(spreadRandom.NextGaussian(0, 3));
            }

            if (throwSourceEnt != null)
            {
                projComp.User = throwSourceEnt;
                projComp.IgnoreEntity(throwSourceEnt);

                throwSourceEnt.Transform.LocalRotation = angle.GetCardinalDir().ToAngle();
            }

            if (!thrownEnt.TryGetComponent(out PhysicsComponent physComp))
                physComp = thrownEnt.AddComponent<PhysicsComponent>();

            var timing = IoCManager.Resolve<IGameTiming>();
            var spd = throwForce * 60; // acceleration is applied in 1 tick instead of 1 second, scale appropriately

            physComp.SetController<ThrowController>();
            (physComp.Controller as ThrowController)?.StartThrow(angle.ToVec() * spd);

            if (throwSourceEnt != null && throwSourceEnt.TryGetComponent<PhysicsComponent>(out var physics)
                                       && physics.Controller is MoverController mover)
            {
                var physicsMgr = IoCManager.Resolve<IPhysicsManager>();

                if (physicsMgr.IsWeightless(throwSourceEnt.Transform.GridPosition))
                {
                    // We don't check for surrounding entities,
                    // so you'll still get knocked around if you're hugging the station wall in zero g.
                    // I got kinda lazy is the reason why. Also it makes a bit of sense.
                    // If somebody wants they can come along and make it so magboots completely hold you still.
                    // Would be a cool incentive to use them.
                    const float ThrowFactor = 5.0f; // Break Newton's Third Law for better gameplay
                    mover.Push(-angle.ToVec(), spd * ThrowFactor / physics.Mass);
                }
            }
        }

        /// <summary>
        ///     Throw an entity at the position of <paramref name="targetLoc"/> from <paramref name="sourceLoc"/>,
        ///     without overshooting.
        /// </summary>
        /// <param name="thrownEnt">The entity to throw.</param>
        /// <param name="throwForceMax">
        /// The MAXIMUM force to throw the entity with.
        /// Throw force increases with distance to target, this is the maximum force allowed.
        /// </param>
        /// <param name="targetLoc">
        /// The target location to throw at.
        /// This function will try to land at this exact spot,
        /// if <paramref name="throwForceMax"/> is large enough to allow for it to be reached.
        /// </param>
        /// <param name="sourceLoc">
        /// The position to start the throw from.
        /// </param>
        /// <param name="spread">
        /// If true, slightly spread the actual throw angle.
        /// </param>
        /// <param name="throwSourceEnt">
        /// The entity that did the throwing. An opposite impulse will be applied to this entity if passed in.
        /// </param>
        public static void ThrowTo(IEntity thrownEnt, float throwForceMax, GridCoordinates targetLoc,
            GridCoordinates sourceLoc, bool spread = false, IEntity throwSourceEnt = null)
        {
            var mapManager = IoCManager.Resolve<IMapManager>();
            var timing = IoCManager.Resolve<IGameTiming>();

            // Calculate the force necessary to land a throw based on throw duration, mass and distance.
            var distance = (targetLoc.ToMapPos(mapManager) - sourceLoc.ToMapPos(mapManager)).Length;
            var throwDuration = ThrowController.DefaultThrowTime;
            var mass = 1f;
            if (thrownEnt.TryGetComponent(out PhysicsComponent physicsComponent))
            {
                mass = physicsComponent.Mass;
            }

            var velocityNecessary = distance / throwDuration;
            var impulseNecessary = velocityNecessary * mass;
            var forceNecessary = impulseNecessary * (1f / timing.TickRate);

            // Then clamp it to the max force allowed and call Throw().
            Throw(thrownEnt, MathF.Min(forceNecessary, throwForceMax), targetLoc, sourceLoc, spread, throwSourceEnt);
        }
    }
}
