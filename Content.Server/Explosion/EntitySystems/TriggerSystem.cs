using System;
using Content.Server.Doors.Components;
using Content.Server.Explosion.Components;
using Content.Server.Flash;
using Content.Server.Flash.Components;
using Content.Shared.Audio;
using Content.Shared.Doors;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Explosion.EntitySystems
{
    /// <summary>
    /// Raised whenever something is Triggered on the entity.
    /// </summary>
    public class TriggerEvent : HandledEntityEventArgs
    {
        public EntityUid Triggered { get; }
        public EntityUid User { get; }

        public TriggerEvent(EntityUid triggered, EntityUid user = default)
        {
            Triggered = triggered;
            User = user;
        }
    }

    [UsedImplicitly]
    public sealed class TriggerSystem : EntitySystem
    {
        [Dependency] private readonly ExplosionSystem _explosions = default!;
        [Dependency] private readonly FlashSystem _flashSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TriggerOnCollideComponent, StartCollideEvent>(HandleCollide);

            SubscribeLocalEvent<DeleteOnTriggerComponent, TriggerEvent>(HandleDeleteTrigger);
            SubscribeLocalEvent<SoundOnTriggerComponent, TriggerEvent>(HandleSoundTrigger);
            SubscribeLocalEvent<ExplodeOnTriggerComponent, TriggerEvent>(HandleExplodeTrigger);
            SubscribeLocalEvent<FlashOnTriggerComponent, TriggerEvent>(HandleFlashTrigger);
            SubscribeLocalEvent<ToggleDoorOnTriggerComponent, TriggerEvent>(HandleDoorTrigger);
        }

        #region Explosions
        private void HandleExplodeTrigger(EntityUid uid, ExplodeOnTriggerComponent component, TriggerEvent args)
        {
            if (!EntityManager.TryGetComponent(uid, out ExplosiveComponent? explosiveComponent)) return;

            Explode(uid, explosiveComponent);
        }

        // You really shouldn't call this directly (TODO Change that when ExplosionHelper gets changed).
        public void Explode(EntityUid uid, ExplosiveComponent component)
        {
            if (component.Exploding)
            {
                return;
            }

            component.Exploding = true;
            _explosions.SpawnExplosion(uid, component.DevastationRange, component.HeavyImpactRange, component.LightImpactRange, component.FlashRange);
            EntityManager.QueueDeleteEntity(uid);
        }
        #endregion

        #region Flash
        private void HandleFlashTrigger(EntityUid uid, FlashOnTriggerComponent component, TriggerEvent args)
        {
            if (component.Flashed) return;

            // TODO Make flash durations sane ffs.
            _flashSystem.FlashArea(uid, args.User, component.Range, component.Duration * 1000f);
            component.Flashed = true;
        }
        #endregion

        private void HandleSoundTrigger(EntityUid uid, SoundOnTriggerComponent component, TriggerEvent args)
        {
            if (component.Sound == null) return;
            SoundSystem.Play(Filter.Pvs(component.Owner), component.Sound.GetSound(), AudioHelpers.WithVariation(0.01f));
        }

        private void HandleDeleteTrigger(EntityUid uid, DeleteOnTriggerComponent component, TriggerEvent args)
        {
            EntityManager.QueueDeleteEntity(uid);
        }

        private void HandleDoorTrigger(EntityUid uid, ToggleDoorOnTriggerComponent component, TriggerEvent args)
        {
            if (EntityManager.TryGetComponent<ServerDoorComponent>(uid, out var door))
            {
                switch (door.State)
                {
                    case SharedDoorComponent.DoorState.Open:
                        door.Close();
                        break;
                    case SharedDoorComponent.DoorState.Closed:
                        door.Open();
                        break;
                    case SharedDoorComponent.DoorState.Closing:
                    case SharedDoorComponent.DoorState.Opening:
                        break;
                }
            }
        }

        private void HandleCollide(EntityUid uid, TriggerOnCollideComponent component, StartCollideEvent args)
        {
            Trigger(component.Owner);
        }

        public void Trigger(EntityUid trigger, EntityUid user = default)
        {
            var triggerEvent = new TriggerEvent(trigger, user);
            EntityManager.EventBus.RaiseLocalEvent(trigger, triggerEvent);
        }

        public void HandleTimerTrigger(TimeSpan delay, EntityUid triggered, EntityUid user = default)
        {
            if (delay.TotalSeconds <= 0)
            {
                Trigger(triggered, user);
                return;
            }

            Timer.Spawn(delay, () =>
            {
                if ((!IoCManager.Resolve<IEntityManager>().EntityExists(triggered) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(triggered).EntityLifeStage) >= EntityLifeStage.Deleted) return;
                Trigger(triggered, user);
            });
        }
    }
}
