﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Movement;
using Content.Server.GameObjects.Components.Timing;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Input;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Physics.Pull;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.EntitySystems.Click
{
    /// <summary>
    /// Governs interactions during clicking on entities
    /// </summary>
    [UsedImplicitly]
    public sealed class InteractionSystem : SharedInteractionSystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override void Initialize()
        {
            SubscribeNetworkEvent<DragDropMessage>(HandleDragDropMessage);

            CommandBinds.Builder
                .Bind(EngineKeyFunctions.Use,
                    new PointerInputCmdHandler(HandleClientUseItemInHand))
                .Bind(ContentKeyFunctions.WideAttack,
                    new PointerInputCmdHandler(HandleWideAttack))
                .Bind(ContentKeyFunctions.ActivateItemInWorld,
                    new PointerInputCmdHandler(HandleActivateItemInWorld))
                .Bind(ContentKeyFunctions.TryPullObject, new PointerInputCmdHandler(HandleTryPullObject))
                .Register<InteractionSystem>();
        }

        public override void Shutdown()
        {
            CommandBinds.Unregister<InteractionSystem>();
            base.Shutdown();
        }

        private void HandleDragDropMessage(DragDropMessage msg, EntitySessionEventArgs args)
        {
            var performer = args.SenderSession.AttachedEntity;
            if (!EntityManager.TryGetEntity(msg.Dropped, out var dropped)) return;
            if (!EntityManager.TryGetEntity(msg.Target, out var target)) return;

            var interactionArgs = new DragDropEventArgs(performer, msg.DropLocation, dropped, target);

            // must be in range of both the target and the object they are drag / dropping
            if (!interactionArgs.InRangeUnobstructed(ignoreInsideBlocker: true, popup: true)) return;

            // trigger dragdrops on the dropped entity
            foreach (var dragDrop in dropped.GetAllComponents<IDragDrop>())
            {
                if (dragDrop.CanDragDrop(interactionArgs) &&
                    dragDrop.DragDrop(interactionArgs))
                {
                    return;
                }
            }

            // trigger dragdropons on the targeted entity
            foreach (var dragDropOn in target.GetAllComponents<IDragDropOn>())
            {
                if (dragDropOn.CanDragDropOn(interactionArgs) &&
                    dragDropOn.DragDropOn(interactionArgs))
                {
                    return;
                }
            }
        }

        private bool HandleActivateItemInWorld(ICommonSession session, EntityCoordinates coords, EntityUid uid)
        {
            if (!EntityManager.TryGetEntity(uid, out var used))
                return false;

            var playerEnt = ((IPlayerSession) session).AttachedEntity;

            if (playerEnt == null || !playerEnt.IsValid())
            {
                return false;
            }

            if (!playerEnt.Transform.Coordinates.InRange(EntityManager, used.Transform.Coordinates, InteractionRange))
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
            var activateEventArgs = new ActivateEventArgs { User = user, Target = used };
            if (activateEventArgs.InRangeUnobstructed(ignoreInsideBlocker: true, popup: true))
            {
                activateComp.Activate(activateEventArgs);
            }
        }

        private bool HandleWideAttack(ICommonSession session, EntityCoordinates coords, EntityUid uid)
        {
            // client sanitization
            if (!_mapManager.GridExists(coords.GetGridId(_entityManager)))
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
                DoAttack(userEntity, coords, true);
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
        internal void UseItemInHand(IEntity entity, EntityCoordinates coords, EntityUid uid)
        {
            if (entity.HasComponent<BasicActorComponent>())
            {
                throw new InvalidOperationException();
            }

            if (entity.TryGetComponent(out CombatModeComponent combatMode) && combatMode.IsInCombatMode)
            {
                DoAttack(entity, coords, false, uid);
            }
            else
            {
                UserInteraction(entity, coords, uid);
            }
        }

        private bool HandleClientUseItemInHand(ICommonSession session, EntityCoordinates coords, EntityUid uid)
        {
            // client sanitization
            if (!_mapManager.GridExists(coords.GetGridId(_entityManager)))
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

            if (userEntity.TryGetComponent(out CombatModeComponent combat) && combat.IsInCombatMode)
                DoAttack(userEntity, coords, false, uid);
            else
                UserInteraction(userEntity, coords, uid);

            return true;
        }

        private bool HandleTryPullObject(ICommonSession session, EntityCoordinates coords, EntityUid uid)
        {
            // client sanitization
            if (!_mapManager.GridExists(coords.GetGridId(_entityManager)))
            {
                Logger.InfoS("system.interaction", $"Invalid Coordinates for pulling: client={session}, coords={coords}");
                return false;
            }

            if (uid.IsClientSide())
            {
                Logger.WarningS("system.interaction",
                    $"Client sent pull interaction with client-side entity. Session={session}, Uid={uid}");
                return false;
            }

            var player = session.AttachedEntity;

            if (player == null)
            {
                Logger.WarningS("system.interaction",
                    $"Client sent pulling interaction with no attached entity. Session={session}, Uid={uid}");
                return false;
            }

            if (!EntityManager.TryGetEntity(uid, out var pulledObject))
            {
                return false;
            }

            if (player == pulledObject)
            {
                return false;
            }

            if (!pulledObject.TryGetComponent<PullableComponent>(out var pull))
            {
                return false;
            }

            if (!player.TryGetComponent<HandsComponent>(out var hands))
            {
                return false;
            }

            var dist = player.Transform.Coordinates.Position - pulledObject.Transform.Coordinates.Position;
            if (dist.LengthSquared > InteractionRangeSquared)
            {
                return false;
            }

            if (!pull.Owner.TryGetComponent(out ICollidableComponent collidable) ||
                collidable.Anchored)
            {
                return false;
            }

            var controller = collidable.EnsureController<PullController>();

            if (controller.GettingPulled)
            {
                hands.StopPull();
            }
            else
            {
                hands.StartPull(pull);
            }

            return false;
        }

        private void UserInteraction(IEntity player, EntityCoordinates coordinates, EntityUid clickedUid)
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
            if (_mapManager.GetGrid(coordinates.GetGridId(EntityManager)).ParentMapId != playerTransform.MapID)
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
                var diff = coordinates.ToMapPos(EntityManager) - playerTransform.MapPosition.Position;
                if (diff.LengthSquared > 0.01f)
                {
                    playerTransform.LocalRotation = new Angle(diff);
                }
            }

            if (!ActionBlockerSystem.CanInteract(player))
            {
                return;
            }

            // If in a container
            if (ContainerHelpers.IsInContainer(player))
            {
                return;
            }


            // In a container where the attacked entity is not the container's owner
            if (ContainerHelpers.TryGetContainer(player, out var playerContainer) &&
                attacked != playerContainer.Owner)
            {
                // Either the attacked entity is null, not contained or in a different container
                if (attacked == null ||
                    !ContainerHelpers.TryGetContainer(attacked, out var attackedContainer) ||
                    attackedContainer != playerContainer)
                {
                    return;
                }
            }

            // TODO: Check if client should be able to see that object to click on it in the first place

            // Clicked on empty space behavior, try using ranged attack
            if (attacked == null)
            {
                if (item != null)
                {
                    // After attack: Check if we clicked on an empty location, if so the only interaction we can do is AfterInteract
                    var distSqrt = (playerTransform.WorldPosition - coordinates.ToMapPos(EntityManager)).LengthSquared;
                    InteractAfter(player, item, coordinates, distSqrt <= InteractionRangeSquared);
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
                _ = Interaction(player, item, attacked, coordinates);
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
        private void InteractAfter(IEntity user, IEntity weapon, EntityCoordinates clickLocation, bool canReach)
        {
            var message = new AfterInteractMessage(user, weapon, null, clickLocation, canReach);
            RaiseLocalEvent(message);
            if (message.Handled)
            {
                return;
            }

            var afterInteracts = weapon.GetAllComponents<IAfterInteract>().ToList();
            var afterInteractEventArgs = new AfterInteractEventArgs { User = user, ClickLocation = clickLocation, CanReach = canReach };

            foreach (var afterInteract in afterInteracts)
            {
                afterInteract.AfterInteract(afterInteractEventArgs);
            }
        }

        /// <summary>
        /// Uses a weapon/object on an entity
        /// Finds components with the InteractUsing interface and calls their function
        /// </summary>
        public async Task Interaction(IEntity user, IEntity weapon, IEntity attacked, EntityCoordinates clickLocation)
        {
            var attackMsg = new InteractUsingMessage(user, weapon, attacked, clickLocation);
            RaiseLocalEvent(attackMsg);
            if (attackMsg.Handled)
            {
                return;
            }

            var attackBys = attacked.GetAllComponents<IInteractUsing>().OrderByDescending(x => x.Priority);
            var attackByEventArgs = new InteractUsingEventArgs
            {
                User = user, ClickLocation = clickLocation, Using = weapon, Target = attacked
            };

            // all AttackBys should only happen when in range / unobstructed, so no range check is needed
            if (attackByEventArgs.InRangeUnobstructed(ignoreInsideBlocker: true, popup: true))
            {
                foreach (var attackBy in attackBys)
                {
                    if (await attackBy.InteractUsing(attackByEventArgs))
                    {
                        // If an InteractUsing returns a status completion we finish our attack
                        return;
                    }
                }
            }

            var afterAtkMsg = new AfterInteractMessage(user, weapon, attacked, clickLocation, true);
            RaiseLocalEvent(afterAtkMsg);
            if (afterAtkMsg.Handled)
            {
                return;
            }

            // If we aren't directly attacking the nearby object, lets see if our item has an after attack we can do
            var afterAttacks = weapon.GetAllComponents<IAfterInteract>().ToList();
            var afterAttackEventArgs = new AfterInteractEventArgs
            {
                User = user, ClickLocation = clickLocation, Target = attacked, CanReach = true
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
            var attackHandEventArgs = new InteractHandEventArgs { User = user, Target = attacked };

            // all attackHands should only fire when in range / unobstructed
            if (attackHandEventArgs.InRangeUnobstructed(ignoreInsideBlocker: true, popup: true))
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
                if (use.UseEntity(new UseEntityEventArgs { User = user }))
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
        public void LandInteraction(IEntity user, IEntity landing, EntityCoordinates landLocation)
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
        ///     Calls ThrowCollide on all components that implement the IThrowCollide interface
        ///     on a thrown entity and the target entity it hit.
        /// </summary>
        public void ThrowCollideInteraction(IEntity user, IEntity thrown, IEntity target, EntityCoordinates location)
        {
            var collideMsg = new ThrowCollideMessage(user, thrown, target, location);
            RaiseLocalEvent(collideMsg);
            if (collideMsg.Handled)
            {
                return;
            }

            var eventArgs = new ThrowCollideEventArgs(user, thrown, target, location);

            foreach (var comp in thrown.GetAllComponents<IThrowCollide>().ToArray())
            {
                comp.DoHit(eventArgs);
            }

            foreach (var comp in target.GetAllComponents<IThrowCollide>().ToArray())
            {
                comp.HitBy(eventArgs);
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
        public void RangedInteraction(IEntity user, IEntity weapon, IEntity attacked, EntityCoordinates clickLocation)
        {
            var rangedMsg = new RangedInteractMessage(user, weapon, attacked, clickLocation);
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

            var afterAtkMsg = new AfterInteractMessage(user, weapon, attacked, clickLocation, false);
            RaiseLocalEvent(afterAtkMsg);
            if (afterAtkMsg.Handled)
                return;

            var afterAttacks = weapon.GetAllComponents<IAfterInteract>().ToList();
            var afterAttackEventArgs = new AfterInteractEventArgs
            {
                User = user, ClickLocation = clickLocation, Target = attacked, CanReach = false
            };

            //See if we have a ranged attack interaction
            foreach (var afterAttack in afterAttacks)
            {
                afterAttack.AfterInteract(afterAttackEventArgs);
            }
        }

        private void DoAttack(IEntity player, EntityCoordinates coordinates, bool wideAttack, EntityUid target = default)
        {
            // Verify player is on the same map as the entity he clicked on
            if (_mapManager.GetGrid(coordinates.GetGridId(_entityManager)).ParentMapId != player.Transform.MapID)
            {
                Logger.WarningS("system.interaction",
                    $"Player named {player.Name} clicked on a map he isn't located on");
                return;
            }

            if (!ActionBlockerSystem.CanAttack(player) ||
                (!wideAttack && !player.InRangeUnobstructed(coordinates, ignoreInsideBlocker: true)))
            {
                return;
            }

            var eventArgs = new AttackEventArgs(player, coordinates, wideAttack, target);

            // Verify player has a hand, and find what object he is currently holding in his active hand
            if (player.TryGetComponent<IHandsComponent>(out var hands))
            {
                var item = hands.GetActiveHand?.Owner;

                if (item != null)
                {
                    foreach (var attackComponent in item.GetAllComponents<IAttack>())
                    {
                        if (wideAttack ? attackComponent.WideAttack(eventArgs) : attackComponent.ClickAttack(eventArgs))
                            return;
                    }
                }
            }

            foreach (var attackComponent in player.GetAllComponents<IAttack>())
            {
                if (wideAttack)
                    attackComponent.WideAttack(eventArgs);
                else
                    attackComponent.ClickAttack(eventArgs);
            }
        }
    }
}
