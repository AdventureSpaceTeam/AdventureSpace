using Content.Server.Cuffs.Components;
using Content.Server.Hands.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Cuffs;
using Content.Shared.Hands;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Player;
using Content.Shared.Interaction;
using Content.Shared.MobState.EntitySystems;

namespace Content.Server.Cuffs
{
    [UsedImplicitly]
    public sealed class CuffableSystem : SharedCuffableSystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedMobStateSystem _mobState = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HandCountChangedEvent>(OnHandCountChanged);
            SubscribeLocalEvent<UncuffAttemptEvent>(OnUncuffAttempt);
            SubscribeLocalEvent<CuffableComponent, GetVerbsEvent<Verb>>(AddUncuffVerb);
            SubscribeLocalEvent<HandcuffComponent, AfterInteractEvent>(OnCuffAfterInteract);
        }

        private void AddUncuffVerb(EntityUid uid, CuffableComponent component, GetVerbsEvent<Verb> args)
        {
            // Can the user access the cuffs, and is there even anything to uncuff?
            if (!args.CanAccess || component.CuffedHandCount == 0 || args.Hands == null)
                return;

            // We only check can interact if the user is not uncuffing themselves. As a result, the verb will show up
            // when the user is incapacitated & trying to uncuff themselves, but TryUncuff() will still fail when
            // attempted.
            if (args.User != args.Target && !args.CanInteract)
                return;

            Verb verb = new()
            {
                Act = () => component.TryUncuff(args.User),
                Text = Loc.GetString("uncuff-verb-get-data-text")
            };
            //TODO VERB ICON add uncuffing symbol? may re-use the alert symbol showing that you are currently cuffed?
            args.Verbs.Add(verb);
        }

        private void OnCuffAfterInteract(EntityUid uid, HandcuffComponent component, AfterInteractEvent args)
        {
            if (component.Cuffing)
                return;

            if (args.Target is not {Valid: true} target ||
                    !EntityManager.TryGetComponent<CuffableComponent>(args.Target.Value, out var cuffed))
            {
                return;
            }

            if (component.Broken)
            {
                _popup.PopupEntity(Loc.GetString("handcuff-component-cuffs-broken-error"), args.User, Filter.Entities(args.User));
                return;
            }

            if (!EntityManager.TryGetComponent<HandsComponent?>(target, out var hands))
            {
                _popup.PopupEntity(Loc.GetString("handcuff-component-target-has-no-hands-error",("targetName", args.Target)), args.User, Filter.Entities(args.User));
                return;
            }

            if (cuffed.CuffedHandCount >= hands.Count)
            {
                _popup.PopupEntity(Loc.GetString("handcuff-component-target-has-no-free-hands-error",("targetName", args.Target)), args.User, Filter.Entities(args.User));
                return;
            }

            if (!args.CanReach)
            {
                _popup.PopupEntity(Loc.GetString("handcuff-component-too-far-away-error"), args.User, Filter.Entities(args.User));
                return;
            }

            if (args.Target == args.User)
            {
                _popup.PopupEntity(Loc.GetString("handcuff-component-target-self"), args.User, Filter.Entities(args.User));
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("handcuff-component-start-cuffing-target-message",("targetName", args.Target)), args.User, Filter.Entities(args.User));
                _popup.PopupEntity(Loc.GetString("handcuff-component-start-cuffing-by-other-message",("otherName", args.User)), target, Filter.Entities(args.User));
            }

            _audio.PlayPvs(component.StartCuffSound, uid);

            component.TryUpdateCuff(args.User, target, cuffed);
            args.Handled = true;
        }

        private void OnUncuffAttempt(UncuffAttemptEvent args)
        {
            if (args.Cancelled)
            {
                return;
            }
            if (!EntityManager.EntityExists(args.User))
            {
                // Should this even be possible?
                args.Cancel();
                return;
            }
            // If the user is the target, special logic applies.
            // This is because the CanInteract blocking of the cuffs prevents self-uncuff.
            if (args.User == args.Target)
            {
                // This UncuffAttemptEvent check should probably be In MobStateSystem, not here?
                if (_mobState.IsIncapacitated(args.User))
                {
                    args.Cancel();
                }
                else
                {
                    // TODO Find a way for cuffable to check ActionBlockerSystem.CanInteract() without blocking itself
                }
            }
            else
            {
                // Check if the user can interact.
                if (!_actionBlockerSystem.CanInteract(args.User, args.Target))
                {
                    args.Cancel();
                }
            }
            if (args.Cancelled)
            {
                _popup.PopupEntity(Loc.GetString("cuffable-component-cannot-interact-message"), args.Target, Filter.Entities(args.User));
            }
        }

        /// <summary>
        ///     Check the current amount of hands the owner has, and if there's less hands than active cuffs we remove some cuffs.
        /// </summary>
        private void OnHandCountChanged(HandCountChangedEvent message)
        {
            var owner = message.Sender;

            if (!EntityManager.TryGetComponent(owner, out CuffableComponent? cuffable) ||
                !cuffable.Initialized)
            {
                return;
            }

            var dirty = false;
            var handCount = EntityManager.GetComponentOrNull<HandsComponent>(owner)?.Count ?? 0;

            while (cuffable.CuffedHandCount > handCount && cuffable.CuffedHandCount > 0)
            {
                dirty = true;

                var container = cuffable.Container;
                var entity = container.ContainedEntities[^1];

                container.Remove(entity);
                EntityManager.GetComponent<TransformComponent>(entity).WorldPosition = EntityManager.GetComponent<TransformComponent>(owner).WorldPosition;
            }

            if (dirty)
            {
                cuffable.CanStillInteract = handCount > cuffable.CuffedHandCount;
                _actionBlockerSystem.UpdateCanMove(cuffable.Owner);
                cuffable.CuffedStateChanged();
                Dirty(cuffable);
            }
        }
    }

    /// <summary>
    /// Event fired on the User when the User attempts to cuff the Target.
    /// Should generate popups on the User.
    /// </summary>
    public sealed class UncuffAttemptEvent : CancellableEntityEventArgs
    {
        public readonly EntityUid User;
        public readonly EntityUid Target;

        public UncuffAttemptEvent(EntityUid user, EntityUid target)
        {
            User = user;
            Target = target;
        }
    }
}
