﻿#nullable enable
using System;
using Content.Shared.Buckle.Components;
using Content.Shared.Climbing;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Timing;

namespace Content.Server.Climbing.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedClimbingComponent))]
    public class ClimbingComponent : SharedClimbingComponent
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override bool IsClimbing
        {
            get => base.IsClimbing;
            set
            {
                if (_isClimbing == value)
                    return;

                base.IsClimbing = value;

                if (value)
                {
                    StartClimbTime = IoCManager.Resolve<IGameTiming>().CurTime;
                    EntitySystem.Get<ClimbSystem>().AddActiveClimber(this);
                    OwnerIsTransitioning = true;
                }
                else
                {
                    EntitySystem.Get<ClimbSystem>().RemoveActiveClimber(this);
                    OwnerIsTransitioning = false;
                }

                Dirty();
            }
        }

        protected override bool OwnerIsTransitioning
        {
            get => base.OwnerIsTransitioning;
            set
            {
                if (value == base.OwnerIsTransitioning) return;
                base.OwnerIsTransitioning = value;
                Dirty();
            }
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case BuckleMessage msg:
                    if (msg.Buckled)
                        IsClimbing = false;

                    break;
            }
        }

        /// <summary>
        /// Make the owner climb from one point to another
        /// </summary>
        public void TryMoveTo(Vector2 from, Vector2 to)
        {
            if (Body == null) return;

            var velocity = (to - from).Length;

            if (velocity <= 0.0f) return;

            Body.ApplyLinearImpulse((to - from).Normalized * velocity * 400);
            OwnerIsTransitioning = true;

            Owner.SpawnTimer((int) (BufferTime * 1000), () =>
            {
                if (Deleted) return;
                OwnerIsTransitioning = false;
            });
        }

        public void Update()
        {
            if (!IsClimbing || _gameTiming.CurTime < TimeSpan.FromSeconds(BufferTime) + StartClimbTime)
            {
                return;
            }

            if (!IsOnClimbableThisFrame && IsClimbing)
                IsClimbing = false;
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new ClimbModeComponentState(_isClimbing, OwnerIsTransitioning);
        }
    }
}
