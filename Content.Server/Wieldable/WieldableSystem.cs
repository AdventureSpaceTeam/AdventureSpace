﻿using Content.Server.DoAfter;
using Content.Server.Hands.Components;
using Content.Server.Hands.Systems;
using Content.Server.Items;
using Content.Server.Weapon.Melee;
using Content.Server.Wieldable.Components;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Server.Wieldable
{
    public class WieldableSystem : EntitySystem
    {
        [Dependency] private readonly DoAfterSystem _doAfter = default!;
        [Dependency] private readonly HandVirtualItemSystem _virtualItemSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<WieldableComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<WieldableComponent, ItemWieldedEvent>(OnItemWielded);
            SubscribeLocalEvent<WieldableComponent, ItemUnwieldedEvent>(OnItemUnwielded);
            SubscribeLocalEvent<WieldableComponent, UnequippedHandEvent>(OnItemLeaveHand);
            SubscribeLocalEvent<WieldableComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);

            SubscribeLocalEvent<IncreaseDamageOnWieldComponent, MeleeHitEvent>(OnMeleeHit);
        }

        private void OnUseInHand(EntityUid uid, WieldableComponent component, UseInHandEvent args)
        {
            if (args.Handled)
                return;
            if(!component.Wielded)
                AttemptWield(uid, component, args.User);
            else
                AttemptUnwield(uid, component, args.User);
        }

        public bool CanWield(EntityUid uid, WieldableComponent component, IEntity user, bool quiet=false)
        {
            // Do they have enough hands free?
            if (!ComponentManager.TryGetComponent<HandsComponent>(user.Uid, out var hands))
            {
                if(!quiet)
                    user.PopupMessage(Loc.GetString("wieldable-component-no-hands"));
                return false;
            }

            if (hands.GetFreeHands() < component.FreeHandsRequired)
            {
                // TODO FLUENT need function to change 'hands' to 'hand' when there's only 1 required
                if (!quiet)
                {
                    user.PopupMessage(Loc.GetString("wieldable-component-not-enough-free-hands",
                        ("number", component.FreeHandsRequired),
                        ("item", EntityManager.GetEntity(uid))));
                }

                return false;
            }

            // Is it.. actually in one of their hands?
            if (!hands.TryGetHandHoldingEntity(EntityManager.GetEntity(uid), out var _))
            {
                if (!quiet)
                {
                    user.PopupMessage(Loc.GetString("wieldable-component-not-in-hands",
                        ("item", EntityManager.GetEntity(uid))));
                }

                return false;
            }

            // Seems legit.
            return true;
        }

        /// <summary>
        ///     Attempts to wield an item, creating a DoAfter..
        /// </summary>
        public void AttemptWield(EntityUid uid, WieldableComponent component, IEntity user)
        {
            if (!CanWield(uid, component, user))
                return;
            var ev = new BeforeWieldEvent();
            RaiseLocalEvent(uid, ev, false);
            var used = EntityManager.GetEntity(uid);

            if (ev.Cancelled) return;

            var doargs = new DoAfterEventArgs(
                user,
                component.WieldTime,
                default,
                used
            )
            {
                BreakOnUserMove = false,
                BreakOnDamage = true,
                BreakOnStun = true,
                BreakOnTargetMove = true,
                TargetFinishedEvent = new ItemWieldedEvent(user),
                UserFinishedEvent = new WieldedItemEvent(used)
            };

            _doAfter.DoAfter(doargs);
        }

        /// <summary>
        ///     Attempts to unwield an item, with no DoAfter.
        /// </summary>
        public void AttemptUnwield(EntityUid uid, WieldableComponent component, IEntity user)
        {
            var ev = new BeforeUnwieldEvent();
            RaiseLocalEvent(uid, ev, false);
            var used = EntityManager.GetEntity(uid);

            if (ev.Cancelled) return;

            var targEv = new ItemUnwieldedEvent(user);
            var userEv = new UnwieldedItemEvent(used);

            RaiseLocalEvent(uid, targEv, false);
            RaiseLocalEvent(user.Uid, userEv, false);
        }

        private void OnItemWielded(EntityUid uid, WieldableComponent component, ItemWieldedEvent args)
        {
            if (args.User == null)
                return;
            if (!CanWield(uid, component, args.User) || component.Wielded)
                return;

            if (ComponentManager.TryGetComponent<ItemComponent>(uid, out var item))
            {
                component.OldInhandPrefix = item.EquippedPrefix;
                item.EquippedPrefix = component.WieldedInhandPrefix;
            }

            component.Wielded = true;

            if (component.WieldSound != null)
            {
                SoundSystem.Play(Filter.Pvs(EntityManager.GetEntity(uid)), component.WieldSound.GetSound());
            }

            for (var i = 0; i < component.FreeHandsRequired; i++)
            {
                _virtualItemSystem.TrySpawnVirtualItemInHand(uid, args.User.Uid);
            }

            args.User.PopupMessage(Loc.GetString("wieldable-component-successful-wield",
                ("item", EntityManager.GetEntity(uid))));
        }

        private void OnItemUnwielded(EntityUid uid, WieldableComponent component, ItemUnwieldedEvent args)
        {
            if (args.User == null)
                return;
            if (!component.Wielded)
                return;

            if (ComponentManager.TryGetComponent<ItemComponent>(uid, out var item))
            {
                item.EquippedPrefix = component.OldInhandPrefix;
            }

            component.Wielded = false;

            if (!args.Force) // don't play sound/popup if this was a forced unwield
            {
                if (component.UnwieldSound != null)
                {
                    SoundSystem.Play(Filter.Pvs(EntityManager.GetEntity(uid)),
                        component.UnwieldSound.GetSound());
                }

                args.User.PopupMessage(Loc.GetString("wieldable-component-failed-wield",
                    ("item", EntityManager.GetEntity(uid))));
            }

            _virtualItemSystem.DeleteInHandsMatching(args.User.Uid, uid);
        }

        private void OnItemLeaveHand(EntityUid uid, WieldableComponent component, UnequippedHandEvent args)
        {
            if (!component.Wielded || component.Owner.Uid != args.Unequipped.Uid)
                return;
            RaiseLocalEvent(uid, new ItemUnwieldedEvent(args.User, force: true));
        }

        private void OnVirtualItemDeleted(EntityUid uid, WieldableComponent component, VirtualItemDeletedEvent args)
        {
            if(args.BlockingEntity == uid && component.Wielded)
                AttemptUnwield(args.BlockingEntity, component, EntityManager.GetEntity(args.User));
        }

        private void OnMeleeHit(EntityUid uid, IncreaseDamageOnWieldComponent component, MeleeHitEvent args)
        {
            if (ComponentManager.TryGetComponent<WieldableComponent>(uid, out var wield))
            {
                if (!wield.Wielded)
                    return;
            }
            if (args.Handled)
                return;

            args.ModifiersList.Add(component.Modifiers);
        }
    }

    #region Events

    public class BeforeWieldEvent : CancellableEntityEventArgs
    {
    }

    /// <summary>
    ///     Raised on the item that has been wielded.
    /// </summary>
    public class ItemWieldedEvent : EntityEventArgs
    {
        public IEntity? User;

        public ItemWieldedEvent(IEntity? user=null)
        {
            User = user;
        }
    }

    /// <summary>
    ///     Raised on the user who wielded the item.
    /// </summary>
    public class WieldedItemEvent : EntityEventArgs
    {
        public IEntity Item;

        public WieldedItemEvent(IEntity item)
        {
            Item = item;
        }
    }

    public class BeforeUnwieldEvent : CancellableEntityEventArgs
    {
    }

    /// <summary>
    ///     Raised on the item that has been unwielded.
    /// </summary>
    public class ItemUnwieldedEvent : EntityEventArgs
    {
        public IEntity? User;
        /// <summary>
        ///     Whether the item is being forced to be unwielded, or if the player chose to unwield it themselves.
        /// </summary>
        public bool Force;

        public ItemUnwieldedEvent(IEntity? user=null, bool force=false)
        {
            User = user;
            Force = force;
        }
    }

    /// <summary>
    ///     Raised on the user who unwielded the item.
    /// </summary>
    public class UnwieldedItemEvent : EntityEventArgs
    {
        public IEntity Item;

        public UnwieldedItemEvent(IEntity item)
        {
            Item = item;
        }
    }

    #endregion
}
