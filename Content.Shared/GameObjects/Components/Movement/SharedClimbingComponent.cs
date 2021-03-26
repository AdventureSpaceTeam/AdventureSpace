#nullable enable
using System;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Movement
{
    public abstract class SharedClimbingComponent : Component, IActionBlocker
    {
        public sealed override string Name => "Climbing";
        public sealed override uint? NetID => ContentNetIDs.CLIMBING;

        protected bool IsOnClimbableThisFrame
        {
            get
            {
                if (Body == null) return false;

                foreach (var entity in Body.GetBodiesIntersecting())
                {
                    if ((entity.CollisionLayer & (int) CollisionGroup.SmallImpassable) != 0) return true;
                }

                return false;
            }
        }

        bool IActionBlocker.CanMove() => !OwnerIsTransitioning;

        [ViewVariables]
        protected virtual bool OwnerIsTransitioning
        {
            get => _ownerIsTransitioning;
            set
            {
                if (_ownerIsTransitioning == value) return;
                _ownerIsTransitioning = value;
                if (Body == null) return;
                if (value)
                {
                    Body.BodyType = BodyType.Dynamic;
                }
                else
                {
                    Body.BodyType = BodyType.KinematicController;
                }
            }
        }

        private bool _ownerIsTransitioning = false;

        [ComponentDependency] protected PhysicsComponent? Body;

        protected TimeSpan StartClimbTime = TimeSpan.Zero;

        /// <summary>
        ///     We'll launch the mob onto the table and give them at least this amount of time to be on it.
        /// </summary>
        protected const float BufferTime = 0.3f;

        public virtual bool IsClimbing
        {
            get => _isClimbing;
            set
            {
                if (_isClimbing == value) return;
                _isClimbing = value;

                ToggleSmallPassable(value);
            }
        }

        protected bool _isClimbing;

        // TODO: Layers need a re-work
        private void ToggleSmallPassable(bool value)
        {
            // Hope the mob has one fixture
            if (Body == null || Body.Deleted) return;

            foreach (var fixture in Body.Fixtures)
            {
                if (value)
                {
                    fixture.CollisionMask &= ~(int) CollisionGroup.SmallImpassable;
                }
                else
                {
                    fixture.CollisionMask |= (int) CollisionGroup.SmallImpassable;
                }
            }
        }

        [Serializable, NetSerializable]
        protected sealed class ClimbModeComponentState : ComponentState
        {
            public ClimbModeComponentState(bool climbing, bool isTransitioning) : base(ContentNetIDs.CLIMBING)
            {
                Climbing = climbing;
                IsTransitioning = isTransitioning;
            }

            public bool Climbing { get; }
            public bool IsTransitioning { get; }
        }
    }
}
