﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Movement;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Shared.GameObjects.Components.Items;
using Content.Server.GameObjects.EntitySystems.Click;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Shared.BodySystem;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystemMessages;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.GUI
{
    [RegisterComponent]
    [ComponentReference(typeof(IHandsComponent))]
    [ComponentReference(typeof(ISharedHandsComponent))]
    public class HandsComponent : SharedHandsComponent, IHandsComponent, IBodyPartAdded, IBodyPartRemoved
    {
#pragma warning disable 649
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
#pragma warning restore 649

        private string? _activeHand;
        private uint _nextHand;

        [ViewVariables(VVAccess.ReadWrite)]
        public string? ActiveHand
        {
            get => _activeHand;
            set
            {
                if (value != null && GetHand(value) == null)
                {
                    throw new ArgumentException($"No hand '{value}'");
                }

                _activeHand = value;
                Dirty();
            }
        }

        [ViewVariables] private readonly List<Hand> _hands = new List<Hand>();

        // Mostly arbitrary.
        public const float PickupRange = 2;

        [ViewVariables] public int Count => _hands.Count;

        // TODO: This does not serialize what objects are held.
        protected override void Startup()
        {
            base.Startup();
            ActiveHand = _hands.LastOrDefault()?.Name;
        }

        public IEnumerable<ItemComponent> GetAllHeldItems()
        {
            foreach (var hand in _hands)
            {
                if (hand.Entity != null)
                {
                    yield return hand.Entity.GetComponent<ItemComponent>();
                }
            }
        }

        public bool IsHolding(IEntity entity)
        {
            foreach (var hand in _hands)
            {
                if (hand.Entity == entity)
                {
                    return true;
                }
            }
            return false;
        }

        private Hand? GetHand(string name)
        {
            return _hands.FirstOrDefault(hand => hand.Name == name);
        }

        public ItemComponent? GetItem(string handName)
        {
            return GetHand(handName)?.Entity?.GetComponent<ItemComponent>();
        }

        public ItemComponent? GetActiveHand => ActiveHand == null
            ? null
            : GetItem(ActiveHand);

        /// <summary>
        ///     Enumerates over the hand keys, returning the active hand first.
        /// </summary>
        public IEnumerable<string> ActivePriorityEnumerable()
        {
            if (ActiveHand != null)
            {
                yield return ActiveHand;
            }

            foreach (var hand in _hands)
            {
                if (hand.Name == ActiveHand)
                {
                    continue;
                }

                yield return hand.Name;
            }
        }

        public bool PutInHand(ItemComponent item)
        {
            foreach (var hand in ActivePriorityEnumerable())
            {
                if (PutInHand(item, hand, false))
                {
                    return true;
                }
            }

            return false;
        }

        public bool PutInHand(ItemComponent item, string index, bool fallback = true)
        {
            var hand = GetHand(index);
            if (!CanPutInHand(item, index) || hand == null)
            {
                return fallback && PutInHand(item);
            }

            Dirty();
            var success = hand.Container.Insert(item.Owner);
            if (success)
            {
                item.Owner.Transform.LocalPosition = Vector2.Zero;
            }

            _entitySystemManager.GetEntitySystem<InteractionSystem>().HandSelectedInteraction(Owner, item.Owner);

            return success;
        }

        public void PutInHandOrDrop(ItemComponent item)
        {
            if (!PutInHand(item))
            {
                item.Owner.Transform.GridPosition = Owner.Transform.GridPosition;
            }
        }

        public bool CanPutInHand(ItemComponent item)
        {
            foreach (var handName in ActivePriorityEnumerable())
            {
                if (CanPutInHand(item, handName))
                {
                    return true;
                }
            }

            return false;
        }

        public bool CanPutInHand(ItemComponent item, string index)
        {
            return GetHand(index)?.Container.CanInsert(item.Owner) == true;
        }

        public bool TryHand(IEntity entity, [MaybeNullWhen(false)] out string handName)
        {
            handName = null;

            foreach (var hand in _hands)
            {
                if (hand.Entity == entity)
                {
                    handName = hand.Name;
                    return true;
                }
            }

            return false;
        }

        public bool Drop(string slot, GridCoordinates coords, bool doMobChecks = true)
        {
            var hand = GetHand(slot);
            if (!CanDrop(slot) || hand?.Entity == null)
            {
                return false;
            }

            var item = hand.Entity.GetComponent<ItemComponent>();

            if (!hand.Container.Remove(hand.Entity))
            {
                return false;
            }

            if (doMobChecks &&
                !_entitySystemManager.GetEntitySystem<InteractionSystem>().TryDroppedInteraction(Owner, item.Owner))
            {
                return false;
            }

            item.RemovedFromSlot();
            item.Owner.Transform.GridPosition = coords;

            if (ContainerHelpers.TryGetContainer(Owner, out var container))
            {
                container.Insert(item.Owner);
            }

            Dirty();
            return true;
        }

        public bool Drop(IEntity entity, GridCoordinates coords, bool doMobChecks = true)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (!TryHand(entity, out var slot))
            {
                throw new ArgumentException("Entity must be held in one of our hands.", nameof(entity));
            }

            return Drop(slot, coords, doMobChecks);
        }

        public bool Drop(string slot, bool doMobChecks = true)
        {
            var hand = GetHand(slot);
            if (!CanDrop(slot) || hand?.Entity == null)
            {
                return false;
            }

            var item = hand.Entity.GetComponent<ItemComponent>();

            if (doMobChecks &&
                !_entitySystemManager.GetEntitySystem<InteractionSystem>().TryDroppedInteraction(Owner, item.Owner))
            {
                return false;
            }

            if (!hand.Container.Remove(hand.Entity))
            {
                return false;
            }

            item.RemovedFromSlot();
            item.Owner.Transform.GridPosition = Owner.Transform.GridPosition;

            if (item.Owner.TryGetComponent<SpriteComponent>(out var spriteComponent))
            {
                spriteComponent.RenderOrder = item.Owner.EntityManager.CurrentTick.Value;
            }

            if (ContainerHelpers.TryGetContainer(Owner, out var container))
            {
                container.Insert(item.Owner);
            }

            Dirty();
            return true;
        }

        public bool Drop(IEntity entity, bool doMobChecks = true)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (!TryHand(entity, out var slot))
            {
                throw new ArgumentException("Entity must be held in one of our hands.", nameof(entity));
            }

            return Drop(slot, doMobChecks);
        }

        public bool Drop(string slot, BaseContainer targetContainer, bool doMobChecks = true)
        {
            if (slot == null)
            {
                throw new ArgumentNullException(nameof(slot));
            }

            if (targetContainer == null)
            {
                throw new ArgumentNullException(nameof(targetContainer));
            }

            var hand = GetHand(slot);
            if (!CanDrop(slot) || hand?.Entity == null)
            {
                return false;
            }

            var item = hand.Entity.GetComponent<ItemComponent>();

            if (doMobChecks && !_entitySystemManager.GetEntitySystem<InteractionSystem>().TryDroppedInteraction(Owner, item.Owner))
            {
                return false;
            }

            if (!hand.Container.CanRemove(hand.Entity))
            {
                return false;
            }

            if (!targetContainer.CanInsert(hand.Entity))
            {
                return false;
            }

            if (!hand.Container.Remove(hand.Entity))
            {
                throw new InvalidOperationException();
            }

            item.RemovedFromSlot();

            if (!targetContainer.Insert(item.Owner))
            {
                throw new InvalidOperationException();
            }

            Dirty();
            return true;
        }

        public bool Drop(IEntity entity, BaseContainer targetContainer, bool doMobChecks = true)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (!TryHand(entity, out var slot))
            {
                throw new ArgumentException("Entity must be held in one of our hands.", nameof(entity));
            }

            return Drop(slot, targetContainer, doMobChecks);
        }

        /// <summary>
        ///     Checks whether an item can be dropped from the specified slot.
        /// </summary>
        /// <param name="name">The slot to check for.</param>
        /// <returns>
        ///     True if there is an item in the slot and it can be dropped, false otherwise.
        /// </returns>
        public bool CanDrop(string name)
        {
            var hand = GetHand(name);
            if (hand?.Entity == null)
            {
                return false;
            }

            return hand.Container.CanRemove(hand.Entity);
        }

        public void AddHand(string name)
        {
            if (HasHand(name))
            {
                throw new InvalidOperationException($"Hand '{name}' already exists.");
            }

            var container = ContainerManagerComponent.Create<ContainerSlot>($"hand {_nextHand++}", Owner);
            var hand = new Hand(name, container);

            _hands.Add(hand);

            ActiveHand ??= name;

            Dirty();
        }

        public void RemoveHand(string name)
        {
            var hand = GetHand(name);
            if (hand == null)
            {
                throw new InvalidOperationException($"Hand '{name}' does not exist.");
            }

            Drop(hand.Name, false);
            hand!.Dispose();
            _hands.Remove(hand);

            if (name == ActiveHand)
            {
                _activeHand = _hands.FirstOrDefault()?.Name;
            }

            Dirty();
        }

        public bool HasHand(string name)
        {
            return _hands.Any(hand => hand.Name == name);
        }

        public override ComponentState GetComponentState()
        {
            var hands = new SharedHand[_hands.Count];

            for (var i = 0; i < _hands.Count; i++)
            {
                var location = i == 0
                    ? HandLocation.Right
                    : i == _hands.Count - 1
                        ? HandLocation.Left
                        : HandLocation.Middle;

                var hand = _hands[i].ToShared(i, location);
                hands[i] = hand;
            }

            return new HandsComponentState(hands, ActiveHand);
        }

        public void SwapHands()
        {
            if (ActiveHand == null)
            {
                return;
            }

            var hand = GetHand(ActiveHand);
            if (hand == null)
            {
                throw new InvalidOperationException($"No hand found with name {ActiveHand}");
            }

            var index = _hands.IndexOf(hand);
            index++;
            if (index == _hands.Count)
            {
                index = 0;
            }

            ActiveHand = _hands[index].Name;
        }

        public void ActivateItem()
        {
            var used = GetActiveHand?.Owner;
            if (used != null)
            {
                var interactionSystem = _entitySystemManager.GetEntitySystem<InteractionSystem>();
                interactionSystem.TryUseInteraction(Owner, used);
            }
        }

        public bool ThrowItem()
        {
            var item = GetActiveHand?.Owner;
            if (item != null)
            {
                var interactionSystem = _entitySystemManager.GetEntitySystem<InteractionSystem>();
                return interactionSystem.TryThrowInteraction(Owner, item);
            }

            return false;
        }

        public void StartPull(PullableComponent pullable)
        {
            if (Owner == pullable.Owner)
            {
                return;
            }

            if (IsPulling)
            {
                StopPull();
            }

            PulledObject = pullable.Owner.GetComponent<ICollidableComponent>();
            var controller = PulledObject!.EnsureController<PullController>();
            controller!.StartPull(Owner.GetComponent<ICollidableComponent>());

            AddPullingStatuses();
        }

        public void MovePulledObject(GridCoordinates puller, GridCoordinates to)
        {
            if (PulledObject != null &&
                PulledObject.TryGetController(out PullController controller))
            {
                controller.TryMoveTo(puller, to);
            }
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel channel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, channel, session);

            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            switch (message)
            {
                case ClientChangedHandMsg msg:
                {
                    var playerEntity = session.AttachedEntity;

                    if (playerEntity == Owner && HasHand(msg.Index))
                    {
                        ActiveHand = msg.Index;
                    }

                    break;
                }

                case ClientAttackByInHandMsg msg:
                {
                    var hand = GetHand(msg.Index);
                    if (hand == null)
                    {
                        Logger.WarningS("go.comp.hands", "Got a ClientAttackByInHandMsg with invalid hand name '{0}'",
                            msg.Index);
                        return;
                    }

                    var playerEntity = session.AttachedEntity;
                    var used = GetActiveHand?.Owner;

                    if (playerEntity == Owner && hand.Entity != null)
                    {
                        var interactionSystem = _entitySystemManager.GetEntitySystem<InteractionSystem>();
                        if (used != null)
                        {
                            interactionSystem.Interaction(Owner, used, hand.Entity,
                                GridCoordinates.InvalidGrid);
                        }
                        else
                        {
                            var entity = hand.Entity;
                            if (!Drop(entity))
                            {
                                break;
                            }

                            interactionSystem.Interaction(Owner, entity);
                        }
                    }

                    break;
                }

                case UseInHandMsg _:
                {
                    var playerEntity = session.AttachedEntity;
                    var used = GetActiveHand?.Owner;

                    if (playerEntity == Owner && used != null)
                    {
                        var interactionSystem = _entitySystemManager.GetEntitySystem<InteractionSystem>();
                        interactionSystem.TryUseInteraction(Owner, used);
                    }

                    break;
                }

                case ActivateInHandMsg msg:
                {
                    var playerEntity = session.AttachedEntity;
                    var used = GetItem(msg.Index)?.Owner;

                    if (playerEntity == Owner && used != null)
                    {
                        var interactionSystem = _entitySystemManager.GetEntitySystem<InteractionSystem>();
                        interactionSystem.TryInteractionActivate(Owner, used);
                    }
                    break;
                }
            }
        }

        public void HandleSlotModifiedMaybe(ContainerModifiedMessage message)
        {
            foreach (var hand in _hands)
            {
                if (hand.Container != message.Container)
                {
                    continue;
                }

                Dirty();

                if (!message.Entity.TryGetComponent(out IPhysicsComponent physics))
                {
                    return;
                }

                // set velocity to zero
                physics.Stop();
                return;
            }
        }

        private void AddPullingStatuses()
        {
            if (PulledObject?.Owner != null &&
                PulledObject.Owner.TryGetComponent(out ServerStatusEffectsComponent pulledStatus))
            {
                pulledStatus.ChangeStatusEffectIcon(StatusEffect.Pulled,
                    "/Textures/Interface/StatusEffects/Pull/pulled.png");
            }

            if (Owner.TryGetComponent(out ServerStatusEffectsComponent ownerStatus))
            {
                ownerStatus.ChangeStatusEffectIcon(StatusEffect.Pulling,
                    "/Textures/Interface/StatusEffects/Pull/pulling.png");
            }
        }

        private void RemovePullingStatuses()
        {
            if (PulledObject?.Owner != null &&
                PulledObject.Owner.TryGetComponent(out ServerStatusEffectsComponent pulledStatus))
            {
                pulledStatus.RemoveStatusEffect(StatusEffect.Pulled);
            }

            if (Owner.TryGetComponent(out ServerStatusEffectsComponent ownerStatus))
            {
                ownerStatus.RemoveStatusEffect(StatusEffect.Pulling);
            }
        }

        public override void StopPull()
        {
            RemovePullingStatuses();
            base.StopPull();
        }

        void IBodyPartAdded.BodyPartAdded(BodyPartAddedEventArgs eventArgs)
        {
            if (eventArgs.Part.PartType != BodyPartType.Hand)
            {
                return;
            }

            AddHand(eventArgs.SlotName);
        }

        void IBodyPartRemoved.BodyPartRemoved(BodyPartRemovedEventArgs eventArgs)
        {
            if (eventArgs.Part.PartType != BodyPartType.Hand)
            {
                return;
            }

            RemoveHand(eventArgs.SlotName);
        }
    }

    public class Hand : IDisposable
    {
        public Hand(string name, ContainerSlot container)
        {
            Name = name;
            Container = container;
        }

        public string Name { get; }
        public IEntity? Entity => Container.ContainedEntity;
        public ContainerSlot Container { get; }

        public void Dispose()
        {
            Container.Shutdown(); // TODO verify this
        }

        public SharedHand ToShared(int index, HandLocation location)
        {
            return new SharedHand(index, Name, Entity?.Uid, location);
        }
    }
}
