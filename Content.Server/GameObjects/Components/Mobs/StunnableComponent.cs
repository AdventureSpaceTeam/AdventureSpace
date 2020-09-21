﻿using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.Interfaces.GameObjects.Components;
using NFluidsynth;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Timers;
using Logger = Robust.Shared.Log.Logger;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedStunnableComponent))]
    public class StunnableComponent : SharedStunnableComponent
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        protected override void OnKnockdown()
        {
            EntitySystem.Get<StandingStateSystem>().Down(Owner);
        }

        public void CancelAll()
        {
            KnockdownTimer = 0f;
            StunnedTimer = 0f;
            Dirty();
        }

        public void ResetStuns()
        {
            StunnedTimer = 0f;
            SlowdownTimer = 0f;

            if (KnockedDown)
            {
                EntitySystem.Get<StandingStateSystem>().Standing(Owner);
            }

            KnockdownTimer = 0f;
        }

        public void Update(float delta)
        {
            if (Stunned)
            {
                StunnedTimer -= delta;

                if (StunnedTimer <= 0)
                {
                    StunnedTimer = 0f;
                    Dirty();
                }
            }

            if (KnockedDown)
            {
                KnockdownTimer -= delta;

                if (KnockdownTimer <= 0f)
                {
                    EntitySystem.Get<StandingStateSystem>().Standing(Owner);

                    KnockdownTimer = 0f;
                    Dirty();
                }
            }

            if (SlowedDown)
            {
                SlowdownTimer -= delta;

                if (SlowdownTimer <= 0f)
                {
                    SlowdownTimer = 0f;

                    if (Owner.TryGetComponent(out MovementSpeedModifierComponent movement))
                    {
                        movement.RefreshMovementSpeedModifiers();
                    }

                    Dirty();
                }
            }

            if (!StunStart.HasValue || !StunEnd.HasValue ||
                !Owner.TryGetComponent(out ServerStatusEffectsComponent status))
            {
                return;
            }

            var start = StunStart.Value;
            var end = StunEnd.Value;

            var length = (end - start).TotalSeconds;
            var progress = (_gameTiming.CurTime - start).TotalSeconds;

            if (progress >= length)
            {
                Timer.Spawn(250, () => status.RemoveStatusEffect(StatusEffect.Stun), StatusRemoveCancellation.Token);
                LastStun = null;
            }
        }

        public override ComponentState GetComponentState()
        {
            return new StunnableComponentState(StunnedTimer, KnockdownTimer, SlowdownTimer, WalkModifierOverride,
                RunModifierOverride);
        }
    }
}
