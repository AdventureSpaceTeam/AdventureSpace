﻿using System;
using System.Linq;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Timing;
using Content.Server.Interfaces.GameObjects;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.EntitySystems
{
    /// <summary>
    /// This interface gives components behavior when being clicked on by a user with an object in their hand
    /// who is in range and has unobstructed reach of the target entity (allows inside blockers).
    /// </summary>
    public interface IInteractUsing
    {
        /// <summary>
        /// Called when using one object on another when user is in range of the target entity.
        /// </summary>
        bool InteractUsing(InteractUsingEventArgs eventArgs);
    }

    public class InteractUsingEventArgs : EventArgs, ITargetedInteractEventArgs
    {
        public IEntity User { get; set; }
        public GridCoordinates ClickLocation { get; set; }
        public IEntity Using { get; set; }
        public IEntity Target { get; set; }
    }

    public interface ITargetedInteractEventArgs
    {
        /// <summary>
        /// Performer of the attack
        /// </summary>
        IEntity User { get; }
        /// <summary>
        /// Target of the attack
        /// </summary>
        IEntity Target { get; }

    }

    /// <summary>
    /// This interface gives components behavior when being clicked on by a user with an empty hand
    /// who is in range and has unobstructed reach of the target entity (allows inside blockers).
    /// </summary>
    public interface IInteractHand
    {
        /// <summary>
        /// Called when a player directly interacts with an empty hand when user is in range of the target entity.
        /// </summary>
        bool InteractHand(InteractHandEventArgs eventArgs);
    }

    public class InteractHandEventArgs : EventArgs, ITargetedInteractEventArgs
    {
        public IEntity User { get; set; }
        public IEntity Target { get; set; }
    }

    /// <summary>
    /// This interface gives components behavior when being clicked on by a user with an object
    /// outside the range of direct use
    /// </summary>
    public interface IRangedInteract
    {
        /// <summary>
        /// Called when we try to interact with an entity out of range
        /// </summary>
        /// <returns></returns>
        bool RangedInteract(RangedInteractEventArgs eventArgs);
    }

    [PublicAPI]
    public class RangedInteractEventArgs : EventArgs
    {
        public IEntity User { get; set; }
        public IEntity Using { get; set; }
        public GridCoordinates ClickLocation { get; set; }
    }

    /// <summary>
    /// This interface gives components a behavior when clicking on another object and no interaction occurs,
    /// at any range.
    /// </summary>
    public interface IAfterInteract
    {
        /// <summary>
        /// Called when we interact with nothing, or when we interact with an entity out of range that has no behavior
        /// </summary>
        void AfterInteract(AfterInteractEventArgs eventArgs);
    }

    public class AfterInteractEventArgs : EventArgs
    {
        public IEntity User { get; set; }
        public GridCoordinates ClickLocation { get; set; }
        public IEntity Target { get; set; }
    }

    /// <summary>
    /// This interface gives components behavior when using the entity in your hands
    /// </summary>
    public interface IUse
    {
        /// <summary>
        /// Called when we activate an object we are holding to use it
        /// </summary>
        /// <returns></returns>
        bool UseEntity(UseEntityEventArgs eventArgs);
    }

    public class UseEntityEventArgs : EventArgs
    {
        public IEntity User { get; set; }
    }

    /// <summary>
    ///     This interface gives components behavior when being activated in the world when the user
    ///     is in range and has unobstructed access to the target entity (allows inside blockers).
    /// </summary>
    public interface IActivate
    {
        /// <summary>
        ///     Called when this component is activated by another entity who is in range.
        /// </summary>
        void Activate(ActivateEventArgs eventArgs);
    }

    public class ActivateEventArgs : EventArgs, ITargetedInteractEventArgs
    {
        public IEntity User { get; set; }
        public IEntity Target { get; set; }
    }

    /// <summary>
    ///     This interface gives components behavior when thrown.
    /// </summary>
    public interface IThrown
    {
        void Thrown(ThrownEventArgs eventArgs);
    }

    public class ThrownEventArgs : EventArgs
    {
        public ThrownEventArgs(IEntity user)
        {
            User = user;
        }

        public IEntity User { get; }
    }

    /// <summary>
    ///     This interface gives components behavior when landing after being thrown.
    /// </summary>
    public interface ILand
    {
        void Land(LandEventArgs eventArgs);
    }

    public class LandEventArgs : EventArgs
    {
        public LandEventArgs(IEntity user, GridCoordinates landingLocation)
        {
            User = user;
            LandingLocation = landingLocation;
        }

        public IEntity User { get; }
        public GridCoordinates LandingLocation { get; }
    }

    /// <summary>
    ///     This interface gives components behavior when their owner is put in an inventory slot.
    /// </summary>
    public interface IEquipped
    {
        void Equipped(EquippedEventArgs eventArgs);
    }

    public class EquippedEventArgs : EventArgs
    {
        public EquippedEventArgs(IEntity user, EquipmentSlotDefines.Slots slot)
        {
            User = user;
            Slot = slot;
        }

        public IEntity User { get; }
        public EquipmentSlotDefines.Slots Slot { get; }
    }

    /// <summary>
    ///     This interface gives components behavior when their owner is removed from an inventory slot.
    /// </summary>
    public interface IUnequipped
    {
        void Unequipped(UnequippedEventArgs eventArgs);
    }

    public class UnequippedEventArgs : EventArgs
    {
        public UnequippedEventArgs(IEntity user, EquipmentSlotDefines.Slots slot)
        {
            User = user;
            Slot = slot;
        }

        public IEntity User { get; }
        public EquipmentSlotDefines.Slots Slot { get; }
    }

    /// <summary>
    ///     This interface gives components behavior when being used to "attack".
    /// </summary>
    public interface IAttack
    {
        void Attack(AttackEventArgs eventArgs);
    }

    public class AttackEventArgs : EventArgs
    {
        public AttackEventArgs(IEntity user, GridCoordinates clickLocation)
        {
            User = user;
            ClickLocation = clickLocation;
        }

        public IEntity User { get; }
        public GridCoordinates ClickLocation { get; }
    }

    /// <summary>
    ///     This interface gives components behavior when they're held on the selected hand.
    /// </summary>
    public interface IHandSelected
    {
        void HandSelected(HandSelectedEventArgs eventArgs);
    }

    public class HandSelectedEventArgs : EventArgs
    {
        public HandSelectedEventArgs(IEntity user)
        {
            User = user;
        }

        public IEntity User { get; }
    }

    /// <summary>
    ///     This interface gives components behavior when they're held on a deselected hand.
    /// </summary>
    public interface IHandDeselected
    {
        void HandDeselected(HandDeselectedEventArgs eventArgs);
    }

    public class HandDeselectedEventArgs : EventArgs
    {
        public HandDeselectedEventArgs(IEntity user)
        {
            User = user;
        }

        public IEntity User { get; }
    }

    /// <summary>
    ///     This interface gives components behavior when they're dropped by a mob.
    /// </summary>
    public interface IDropped
    {
        void Dropped(DroppedEventArgs eventArgs);
    }

    public class DroppedEventArgs : EventArgs
    {
        public DroppedEventArgs(IEntity user)
        {
            User = user;
        }

        public IEntity User { get; }
    }

    /// <summary>
    /// Governs interactions during clicking on entities
    /// </summary>
    [UsedImplicitly]
    public sealed class InteractionSystem : SharedInteractionSystem
    {
#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

        public override void Initialize()
        {
            CommandBinds.Builder
                .Bind(EngineKeyFunctions.Use,
                    new PointerInputCmdHandler(HandleClientUseItemInHand))
                .Bind(ContentKeyFunctions.WideAttack,
                    new PointerInputCmdHandler(HandleWideAttack))
                .Bind(ContentKeyFunctions.ActivateItemInWorld,
                    new PointerInputCmdHandler(HandleActivateItemInWorld))
                .Register<InteractionSystem>();
        }

        public override void Shutdown()
        {
            CommandBinds.Unregister<InteractionSystem>();
            base.Shutdown();
        }

        private bool HandleActivateItemInWorld(ICommonSession session, GridCoordinates coords, EntityUid uid)
        {
            if (!EntityManager.TryGetEntity(uid, out var used))
                return false;

            var playerEnt = ((IPlayerSession) session).AttachedEntity;

            if (playerEnt == null || !playerEnt.IsValid())
            {
                return false;
            }

            if (!playerEnt.Transform.GridPosition.InRange(_mapManager, used.Transform.GridPosition, InteractionRange))
            {
                return false;
            }

            InteractionActivate(playerEnt, used);
            return true;
        }

        /// <summary>
        /// Activates the Activate behavior of an object
        /// Verifies that the user is capable of doing the use interaction first
        /// </summary>
        /// <param name="user"></param>
        /// <param name="used"></param>
        public void TryInteractionActivate(IEntity user, IEntity used)
        {
            if (user != null && used != null && ActionBlockerSystem.CanUse(user))
            {
                InteractionActivate(user, used);
            }
        }

        private void InteractionActivate(IEntity user, IEntity used)
        {
            var activateMsg = new ActivateInWorldMessage(user, used);
            RaiseLocalEvent(activateMsg);
            if (activateMsg.Handled)
            {
                return;
            }

            if (!used.TryGetComponent(out IActivate activateComp))
            {
                return;
            }

            // all activates should only fire when in range / unbostructed
            var activateEventArgs = new ActivateEventArgs {User = user, Target = used};
            if (InteractionChecks.InRangeUnobstructed(activateEventArgs))
            {
                activateComp.Activate(activateEventArgs);
            }
        }

        private bool HandleWideAttack(ICommonSession session, GridCoordinates coords, EntityUid uid)
        {
            // client sanitization
            if (!_mapManager.GridExists(coords.GridID))
            {
                Logger.InfoS("system.interaction", $"Invalid Coordinates: client={session}, coords={coords}");
                return true;
            }

            if (uid.IsClientSide())
            {
                Logger.WarningS("system.interaction",
                    $"Client sent attack with client-side entity. Session={session}, Uid={uid}");
                return true;
            }

            var userEntity = ((IPlayerSession) session).AttachedEntity;

            if (userEntity == null || !userEntity.IsValid())
            {
                return true;
            }

            if (userEntity.TryGetComponent(out CombatModeComponent combatMode) && combatMode.IsInCombatMode)
            {
                DoAttack(userEntity, coords);
            }

            return true;
        }

        /// <summary>
        /// Entity will try and use their active hand at the target location.
        /// Don't use for players
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="coords"></param>
        /// <param name="uid"></param>
        internal void UseItemInHand(IEntity entity, GridCoordinates coords, EntityUid uid)
        {
            if (entity.HasComponent<BasicActorComponent>())
            {
                throw new InvalidOperationException();
            }

            if (entity.TryGetComponent(out CombatModeComponent combatMode) && combatMode.IsInCombatMode)
            {
                DoAttack(entity, coords);
            }
            else
            {
                UserInteraction(entity, coords, uid);
            }
        }

        private bool HandleClientUseItemInHand(ICommonSession session, GridCoordinates coords, EntityUid uid)
        {
            // client sanitization
            if (!_mapManager.GridExists(coords.GridID))
            {
                Logger.InfoS("system.interaction", $"Invalid Coordinates: client={session}, coords={coords}");
                return true;
            }

            if (uid.IsClientSide())
            {
                Logger.WarningS("system.interaction",
                    $"Client sent interaction with client-side entity. Session={session}, Uid={uid}");
                return true;
            }

            var userEntity = ((IPlayerSession) session).AttachedEntity;

            if (userEntity == null || !userEntity.IsValid())
            {
                return true;
            }

            UserInteraction(userEntity, coords, uid);

            return true;
        }

        private void UserInteraction(IEntity player, GridCoordinates coordinates, EntityUid clickedUid)
        {
            // Get entity clicked upon from UID if valid UID, if not assume no entity clicked upon and null
            if (!EntityManager.TryGetEntity(clickedUid, out var attacked))
            {
                attacked = null;
            }

            // Verify player has a transform component
            if (!player.TryGetComponent<ITransformComponent>(out var playerTransform))
            {
                return;
            }

            // Verify player is on the same map as the entity he clicked on
            if (_mapManager.GetGrid(coordinates.GridID).ParentMapId != playerTransform.MapID)
            {
                Logger.WarningS("system.interaction",
                    $"Player named {player.Name} clicked on a map he isn't located on");
                return;
            }

            // Verify player has a hand, and find what object he is currently holding in his active hand
            if (!player.TryGetComponent<IHandsComponent>(out var hands))
            {
                return;
            }

            var item = hands.GetActiveHand?.Owner;

            if (ActionBlockerSystem.CanChangeDirection(player))
            {
                var diff = coordinates.ToMapPos(_mapManager) - playerTransform.MapPosition.Position;
                if (diff.LengthSquared > 0.01f)
                {
                    playerTransform.LocalRotation = new Angle(diff);
                }
            }

            if (!ActionBlockerSystem.CanInteract(player))
            {
                return;
            }

            // TODO: Check if client should be able to see that object to click on it in the first place

            // Clicked on empty space behavior, try using ranged attack
            if (attacked == null)
            {
                if (item != null)
                {
                    // After attack: Check if we clicked on an empty location, if so the only interaction we can do is AfterInteract
                    InteractAfter(player, item, coordinates);
                }

                return;
            }

            // Verify attacked object is on the map if we managed to click on it somehow
            if (!attacked.Transform.IsMapTransform)
            {
                Logger.WarningS("system.interaction",
                    $"Player named {player.Name} clicked on object {attacked.Name} that isn't currently on the map somehow");
                return;
            }

            // RangedInteract/AfterInteract: Check distance between user and clicked item, if too large parse it in the ranged function
            // TODO: have range based upon the item being used? or base it upon some variables of the player himself?
            var distance = (playerTransform.WorldPosition - attacked.Transform.WorldPosition).LengthSquared;
            if (distance > InteractionRangeSquared)
            {
                if (item != null)
                {
                    RangedInteraction(player, item, attacked, coordinates);
                    return;
                }

                return; // Add some form of ranged InteractHand here if you need it someday, or perhaps just ways to modify the range of InteractHand
            }

            // We are close to the nearby object and the object isn't contained in our active hand
            // InteractUsing/AfterInteract: We will either use the item on the nearby object
            if (item != null)
            {
                Interaction(player, item, attacked, coordinates);
            }
            // InteractHand/Activate: Since our hand is empty we will use InteractHand/Activate
            else
            {
                Interaction(player, attacked);
            }
        }

        /// <summary>
        ///     We didn't click on any entity, try doing an AfterInteract on the click location
        /// </summary>
        private void InteractAfter(IEntity user, IEntity weapon, GridCoordinates clickLocation)
        {
            var message = new AfterAttackMessage(user, weapon, null, clickLocation);
            RaiseLocalEvent(message);
            if (message.Handled)
            {
                return;
            }

            var afterInteracts = weapon.GetAllComponents<IAfterInteract>().ToList();
            var afterInteractEventArgs = new AfterInteractEventArgs {User = user, ClickLocation = clickLocation};

            foreach (var afterInteract in afterInteracts)
            {
                afterInteract.AfterInteract(afterInteractEventArgs);
            }
        }

        /// <summary>
        /// Uses a weapon/object on an entity
        /// Finds components with the InteractUsing interface and calls their function
        /// </summary>
        public void Interaction(IEntity user, IEntity weapon, IEntity attacked, GridCoordinates clickLocation)
        {
            var attackMsg = new AttackByMessage(user, weapon, attacked, clickLocation);
            RaiseLocalEvent(attackMsg);
            if (attackMsg.Handled)
            {
                return;
            }

            var attackBys = attacked.GetAllComponents<IInteractUsing>().ToList();
            var attackByEventArgs = new InteractUsingEventArgs
            {
                User = user, ClickLocation = clickLocation, Using = weapon, Target = attacked
            };

            // all AttackBys should only happen when in range / unobstructed, so no range check is needed
            if (InteractionChecks.InRangeUnobstructed(attackByEventArgs))
            {
                foreach (var attackBy in attackBys)
                {
                    if (attackBy.InteractUsing(attackByEventArgs))
                    {
                        // If an InteractUsing returns a status completion we finish our attack
                        return;
                    }
                }
            }

            var afterAtkMsg = new AfterAttackMessage(user, weapon, attacked, clickLocation);
            RaiseLocalEvent(afterAtkMsg);
            if (afterAtkMsg.Handled)
            {
                return;
            }

            // If we aren't directly attacking the nearby object, lets see if our item has an after attack we can do
            var afterAttacks = weapon.GetAllComponents<IAfterInteract>().ToList();
            var afterAttackEventArgs = new AfterInteractEventArgs
            {
                User = user, ClickLocation = clickLocation, Target = attacked
            };

            foreach (var afterAttack in afterAttacks)
            {
                afterAttack.AfterInteract(afterAttackEventArgs);
            }
        }

        /// <summary>
        /// Uses an empty hand on an entity
        /// Finds components with the InteractHand interface and calls their function
        /// </summary>
        public void Interaction(IEntity user, IEntity attacked)
        {
            var message = new AttackHandMessage(user, attacked);
            RaiseLocalEvent(message);
            if (message.Handled)
            {
                return;
            }

            var attackHands = attacked.GetAllComponents<IInteractHand>().ToList();
            var attackHandEventArgs = new InteractHandEventArgs {User = user, Target = attacked};

            // all attackHands should only fire when in range / unbostructed
            if (InteractionChecks.InRangeUnobstructed(attackHandEventArgs))
            {
                foreach (var attackHand in attackHands)
                {
                    if (attackHand.InteractHand(attackHandEventArgs))
                    {
                        // If an InteractHand returns a status completion we finish our attack
                        return;
                    }
                }
            }

            // Else we run Activate.
            InteractionActivate(user, attacked);
        }

        /// <summary>
        /// Activates the Use behavior of an object
        /// Verifies that the user is capable of doing the use interaction first
        /// </summary>
        /// <param name="user"></param>
        /// <param name="used"></param>
        public void TryUseInteraction(IEntity user, IEntity used)
        {
            if (user != null && used != null && ActionBlockerSystem.CanUse(user))
            {
                UseInteraction(user, used);
            }
        }

        /// <summary>
        /// Activates/Uses an object in control/possession of a user
        /// If the item has the IUse interface on one of its components we use the object in our hand
        /// </summary>
        public void UseInteraction(IEntity user, IEntity used)
        {
            if (used.TryGetComponent<UseDelayComponent>(out var delayComponent))
            {
                if (delayComponent.ActiveDelay)
                    return;
                else
                    delayComponent.BeginDelay();
            }

            var useMsg = new UseInHandMessage(user, used);
            RaiseLocalEvent(useMsg);
            if (useMsg.Handled)
            {
                return;
            }

            var uses = used.GetAllComponents<IUse>().ToList();

            // Try to use item on any components which have the interface
            foreach (var use in uses)
            {
                if (use.UseEntity(new UseEntityEventArgs {User = user}))
                {
                    // If a Use returns a status completion we finish our attack
                    return;
                }
            }
        }

        /// <summary>
        /// Activates the Throw behavior of an object
        /// Verifies that the user is capable of doing the throw interaction first
        /// </summary>
        public bool TryThrowInteraction(IEntity user, IEntity item)
        {
            if (user == null || item == null || !ActionBlockerSystem.CanThrow(user)) return false;

            ThrownInteraction(user, item);
            return true;
        }

        /// <summary>
        ///     Calls Thrown on all components that implement the IThrown interface
        ///     on an entity that has been thrown.
        /// </summary>
        public void ThrownInteraction(IEntity user, IEntity thrown)
        {
            var throwMsg = new ThrownMessage(user, thrown);
            RaiseLocalEvent(throwMsg);
            if (throwMsg.Handled)
            {
                return;
            }

            var comps = thrown.GetAllComponents<IThrown>().ToList();

            // Call Thrown on all components that implement the interface
            foreach (var comp in comps)
            {
                comp.Thrown(new ThrownEventArgs(user));
            }
        }

        /// <summary>
        ///     Calls Land on all components that implement the ILand interface
        ///     on an entity that has landed after being thrown.
        /// </summary>
        public void LandInteraction(IEntity user, IEntity landing, GridCoordinates landLocation)
        {
            var landMsg = new LandMessage(user, landing, landLocation);
            RaiseLocalEvent(landMsg);
            if (landMsg.Handled)
            {
                return;
            }

            var comps = landing.GetAllComponents<ILand>().ToList();

            // Call Land on all components that implement the interface
            foreach (var comp in comps)
            {
                comp.Land(new LandEventArgs(user, landLocation));
            }
        }

        /// <summary>
        ///     Calls Equipped on all components that implement the IEquipped interface
        ///     on an entity that has been equipped.
        /// </summary>
        public void EquippedInteraction(IEntity user, IEntity equipped, EquipmentSlotDefines.Slots slot)
        {
            var equipMsg = new EquippedMessage(user, equipped, slot);
            RaiseLocalEvent(equipMsg);
            if (equipMsg.Handled)
            {
                return;
            }

            var comps = equipped.GetAllComponents<IEquipped>().ToList();

            // Call Thrown on all components that implement the interface
            foreach (var comp in comps)
            {
                comp.Equipped(new EquippedEventArgs(user, slot));
            }
        }

        /// <summary>
        ///     Calls Unequipped on all components that implement the IUnequipped interface
        ///     on an entity that has been equipped.
        /// </summary>
        public void UnequippedInteraction(IEntity user, IEntity equipped, EquipmentSlotDefines.Slots slot)
        {
            var unequipMsg = new UnequippedMessage(user, equipped, slot);
            RaiseLocalEvent(unequipMsg);
            if (unequipMsg.Handled)
            {
                return;
            }

            var comps = equipped.GetAllComponents<IUnequipped>().ToList();

            // Call Thrown on all components that implement the interface
            foreach (var comp in comps)
            {
                comp.Unequipped(new UnequippedEventArgs(user, slot));
            }
        }

        /// <summary>
        /// Activates the Dropped behavior of an object
        /// Verifies that the user is capable of doing the drop interaction first
        /// </summary>
        public bool TryDroppedInteraction(IEntity user, IEntity item)
        {
            if (user == null || item == null || !ActionBlockerSystem.CanDrop(user)) return false;

            DroppedInteraction(user, item);
            return true;
        }

        /// <summary>
        ///     Calls Dropped on all components that implement the IDropped interface
        ///     on an entity that has been dropped.
        /// </summary>
        public void DroppedInteraction(IEntity user, IEntity item)
        {
            var dropMsg = new DroppedMessage(user, item);
            RaiseLocalEvent(dropMsg);
            if (dropMsg.Handled)
            {
                return;
            }

            var comps = item.GetAllComponents<IDropped>().ToList();

            // Call Land on all components that implement the interface
            foreach (var comp in comps)
            {
                comp.Dropped(new DroppedEventArgs(user));
            }
        }

        /// <summary>
        ///     Calls HandSelected on all components that implement the IHandSelected interface
        ///     on an item entity on a hand that has just been selected.
        /// </summary>
        public void HandSelectedInteraction(IEntity user, IEntity item)
        {
            var handSelectedMsg = new HandSelectedMessage(user, item);
            RaiseLocalEvent(handSelectedMsg);
            if (handSelectedMsg.Handled)
            {
                return;
            }

            var comps = item.GetAllComponents<IHandSelected>().ToList();

            // Call Land on all components that implement the interface
            foreach (var comp in comps)
            {
                comp.HandSelected(new HandSelectedEventArgs(user));
            }
        }

        /// <summary>
        ///     Calls HandDeselected on all components that implement the IHandDeselected interface
        ///     on an item entity on a hand that has just been deselected.
        /// </summary>
        public void HandDeselectedInteraction(IEntity user, IEntity item)
        {
            var handDeselectedMsg = new HandDeselectedMessage(user, item);
            RaiseLocalEvent(handDeselectedMsg);
            if (handDeselectedMsg.Handled)
            {
                return;
            }

            var comps = item.GetAllComponents<IHandDeselected>().ToList();

            // Call Land on all components that implement the interface
            foreach (var comp in comps)
            {
                comp.HandDeselected(new HandDeselectedEventArgs(user));
            }
        }


        /// <summary>
        /// Will have two behaviors, either "uses" the weapon at range on the entity if it is capable of accepting that action
        /// Or it will use the weapon itself on the position clicked, regardless of what was there
        /// </summary>
        public void RangedInteraction(IEntity user, IEntity weapon, IEntity attacked, GridCoordinates clickLocation)
        {
            var rangedMsg = new RangedAttackMessage(user, weapon, attacked, clickLocation);
            RaiseLocalEvent(rangedMsg);
            if (rangedMsg.Handled)
                return;

            var rangedAttackBys = attacked.GetAllComponents<IRangedInteract>().ToList();
            var rangedAttackByEventArgs = new RangedInteractEventArgs
            {
                User = user, Using = weapon, ClickLocation = clickLocation
            };

            // See if we have a ranged attack interaction
            foreach (var t in rangedAttackBys)
            {
                if (t.RangedInteract(rangedAttackByEventArgs))
                {
                    // If an InteractUsing returns a status completion we finish our attack
                    return;
                }
            }

            var afterAtkMsg = new AfterAttackMessage(user, weapon, attacked, clickLocation);
            RaiseLocalEvent(afterAtkMsg);
            if (afterAtkMsg.Handled)
                return;

            var afterAttacks = weapon.GetAllComponents<IAfterInteract>().ToList();
            var afterAttackEventArgs = new AfterInteractEventArgs
            {
                User = user, ClickLocation = clickLocation, Target = attacked
            };

            //See if we have a ranged attack interaction
            foreach (var afterAttack in afterAttacks)
            {
                afterAttack.AfterInteract(afterAttackEventArgs);
            }
        }

        private void DoAttack(IEntity player, GridCoordinates coordinates)
        {
            // Verify player is on the same map as the entity he clicked on
            if (_mapManager.GetGrid(coordinates.GridID).ParentMapId != player.Transform.MapID)
            {
                Logger.WarningS("system.interaction",
                    $"Player named {player.Name} clicked on a map he isn't located on");
                return;
            }

            if (!ActionBlockerSystem.CanAttack(player))
            {
                return;
            }

            var eventArgs = new AttackEventArgs(player, coordinates);

            // Verify player has a hand, and find what object he is currently holding in his active hand
            if (player.TryGetComponent<IHandsComponent>(out var hands))
            {
                var item = hands.GetActiveHand?.Owner;

                if (item != null)
                {
                    var attacked = false;
                    foreach (var attackComponent in item.GetAllComponents<IAttack>())
                    {
                        attackComponent.Attack(eventArgs);
                        attacked = true;
                    }
                    if (attacked)
                    {
                        return;
                    }
                }
            }

            foreach (var attackComponent in player.GetAllComponents<IAttack>())
            {
                attackComponent.Attack(eventArgs);
            }
        }
    }

    /// <summary>
    ///     Raised when being clicked on or "attacked" by a user with an object in their hand
    /// </summary>
    [PublicAPI]
    public class AttackByMessage : EntitySystemMessage
    {
        /// <summary>
        ///     If this message has already been "handled" by a previous system.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     Entity that triggered the attack.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     Entity that the User attacked with.
        /// </summary>
        public IEntity ItemInHand { get; }

        /// <summary>
        ///     Entity that was attacked.
        /// </summary>
        public IEntity Attacked { get; }

        /// <summary>
        ///     The original location that was clicked by the user.
        /// </summary>
        public GridCoordinates ClickLocation { get; }

        public AttackByMessage(IEntity user, IEntity itemInHand, IEntity attacked, GridCoordinates clickLocation)
        {
            User = user;
            ItemInHand = itemInHand;
            Attacked = attacked;
            ClickLocation = clickLocation;
        }
    }

    /// <summary>
    ///      Raised when being clicked on or "attacked" by a user with an empty hand.
    /// </summary>
    [PublicAPI]
    public class AttackHandMessage : EntitySystemMessage
    {
        /// <summary>
        ///     If this message has already been "handled" by a previous system.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     Entity that triggered the attack.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     Entity that was attacked.
        /// </summary>
        public IEntity Attacked { get; }

        public AttackHandMessage(IEntity user, IEntity attacked)
        {
            User = user;
            Attacked = attacked;
        }
    }

    /// <summary>
    ///     Raised when being clicked by objects outside the range of direct use.
    /// </summary>
    [PublicAPI]
    public class RangedAttackMessage : EntitySystemMessage
    {
        /// <summary>
        ///     If this message has already been "handled" by a previous system.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     Entity that triggered the attack.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     Entity that the User attacked with.
        /// </summary>
        public IEntity ItemInHand { get; set; }

        /// <summary>
        ///     Entity that was attacked.
        /// </summary>
        public IEntity Attacked { get; }

        /// <summary>
        ///     Location that the user clicked outside of their interaction range.
        /// </summary>
        public GridCoordinates ClickLocation { get; }

        public RangedAttackMessage(IEntity user, IEntity itemInHand, IEntity attacked, GridCoordinates clickLocation)
        {
            User = user;
            ItemInHand = itemInHand;
            ClickLocation = clickLocation;
            Attacked = attacked;
        }
    }

    /// <summary>
    ///     Raised when clicking on another object and no attack event was handled.
    /// </summary>
    [PublicAPI]
    public class AfterAttackMessage : EntitySystemMessage
    {
        /// <summary>
        ///     If this message has already been "handled" by a previous system.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     Entity that triggered the attack.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     Entity that the User attacked with.
        /// </summary>
        public IEntity ItemInHand { get; set; }

        /// <summary>
        ///     Entity that was attacked. This can be null if the attack did not click on an entity.
        /// </summary>
        public IEntity Attacked { get; }

        /// <summary>
        ///     Location that the user clicked outside of their interaction range.
        /// </summary>
        public GridCoordinates ClickLocation { get; }

        public AfterAttackMessage(IEntity user, IEntity itemInHand, IEntity attacked, GridCoordinates clickLocation)
        {
            User = user;
            Attacked = attacked;
            ClickLocation = clickLocation;
            ItemInHand = itemInHand;
        }
    }

    /// <summary>
    ///     Raised when using the entity in your hands.
    /// </summary>
    [PublicAPI]
    public class UseInHandMessage : EntitySystemMessage
    {
        /// <summary>
        ///     If this message has already been "handled" by a previous system.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     Entity holding the item in their hand.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     Item that was used.
        /// </summary>
        public IEntity Used { get; }

        public UseInHandMessage(IEntity user, IEntity used)
        {
            User = user;
            Used = used;
        }
    }

    /// <summary>
    ///     Raised when throwing the entity in your hands.
    /// </summary>
    [PublicAPI]
    public class ThrownMessage : EntitySystemMessage
    {
        /// <summary>
        ///     If this message has already been "handled" by a previous system.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     Entity that threw the item.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     Item that was thrown.
        /// </summary>
        public IEntity Thrown { get; }

        public ThrownMessage(IEntity user, IEntity thrown)
        {
            User = user;
            Thrown = thrown;
        }
    }

    /// <summary>
    ///     Raised when an entity that was thrown lands.
    /// </summary>
    [PublicAPI]
    public class LandMessage : EntitySystemMessage
    {
        /// <summary>
        ///     If this message has already been "handled" by a previous system.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     Entity that threw the item.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     Item that was thrown.
        /// </summary>
        public IEntity Thrown { get; }

        /// <summary>
        ///     Location where the item landed.
        /// </summary>
        public GridCoordinates LandLocation { get; }

        public LandMessage(IEntity user, IEntity thrown, GridCoordinates landLocation)
        {
            User = user;
            Thrown = thrown;
            LandLocation = landLocation;
        }
    }

    /// <summary>
    ///     Raised when equipping the entity in an inventory slot.
    /// </summary>
    [PublicAPI]
    public class EquippedMessage : EntitySystemMessage
    {
        /// <summary>
        ///     If this message has already been "handled" by a previous system.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     Entity that equipped the item.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     Item that was equipped.
        /// </summary>
        public IEntity Equipped { get; }

        /// <summary>
        ///     Slot where the item was placed.
        /// </summary>
        public EquipmentSlotDefines.Slots Slot { get; }

        public EquippedMessage(IEntity user, IEntity equipped, EquipmentSlotDefines.Slots slot)
        {
            User = user;
            Equipped = equipped;
            Slot = slot;
        }
    }

    /// <summary>
    ///     Raised when removing the entity from an inventory slot.
    /// </summary>
    [PublicAPI]
    public class UnequippedMessage : EntitySystemMessage
    {
        /// <summary>
        ///     If this message has already been "handled" by a previous system.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     Entity that equipped the item.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     Item that was equipped.
        /// </summary>
        public IEntity Equipped { get; }

        /// <summary>
        ///     Slot where the item was removed from.
        /// </summary>
        public EquipmentSlotDefines.Slots Slot { get; }

        public UnequippedMessage(IEntity user, IEntity equipped, EquipmentSlotDefines.Slots slot)
        {
            User = user;
            Equipped = equipped;
            Slot = slot;
        }
    }

    /// <summary>
    ///     Raised when an entity that was thrown lands.
    /// </summary>
    [PublicAPI]
    public class DroppedMessage : EntitySystemMessage
    {
        /// <summary>
        ///     If this message has already been "handled" by a previous system.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     Entity that dropped the item.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     Item that was dropped.
        /// </summary>
        public IEntity Dropped { get; }

        public DroppedMessage(IEntity user, IEntity dropped)
        {
            User = user;
            Dropped = dropped;
        }
    }

    /// <summary>
    ///     Raised when an entity item in a hand is selected.
    /// </summary>
    [PublicAPI]
    public class HandSelectedMessage : EntitySystemMessage
    {
        /// <summary>
        ///     If this message has already been "handled" by a previous system.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     Entity that owns the selected hand.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     The item in question.
        /// </summary>
        public IEntity Item { get; }

        public HandSelectedMessage(IEntity user, IEntity item)
        {
            User = user;
            Item = item;
        }
    }

    /// <summary>
    ///     Raised when an entity item in a hand is deselected.
    /// </summary>
    [PublicAPI]
    public class HandDeselectedMessage : EntitySystemMessage
    {
        /// <summary>
        ///     If this message has already been "handled" by a previous system.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     Entity that owns the deselected hand.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     The item in question.
        /// </summary>
        public IEntity Item { get; }

        public HandDeselectedMessage(IEntity user, IEntity item)
        {
            User = user;
            Item = item;
        }
    }

    /// <summary>
    ///     Raised when an entity is activated in the world.
    /// </summary>
    [PublicAPI]
    public class ActivateInWorldMessage : EntitySystemMessage
    {
        /// <summary>
        ///     If this message has already been "handled" by a previous system.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     Entity that activated the world entity.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     Entity that was activated in the world.
        /// </summary>
        public IEntity Activated { get; }

        public ActivateInWorldMessage(IEntity user, IEntity activated)
        {
            User = user;
            Activated = activated;
        }
    }
}
