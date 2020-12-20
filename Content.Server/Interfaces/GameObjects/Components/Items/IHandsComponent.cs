﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystemMessages;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Interfaces.GameObjects.Components.Items
{
    public interface IHandsComponent : ISharedHandsComponent
    {
        /// <summary>
        ///     Invoked when the hand contents changes or when a hand is added/removed.
        /// </summary>
        event Action? OnItemChanged;

        /// <summary>
        ///     The hands in this component.
        /// </summary>
        IEnumerable<string> Hands { get; }

        /// <summary>
        ///     The hand name of the currently active hand.
        /// </summary>
        string? ActiveHand { get; set; }

        /// <summary>
        ///     Enumerates over every held item.
        /// </summary>
        IEnumerable<ItemComponent> GetAllHeldItems();

        /// <summary>
        ///     Gets the item held by a hand.
        /// </summary>
        /// <param name="handName">The name of the hand to get.</param>
        /// <returns>The item in the held, null if no item is held</returns>
        ItemComponent? GetItem(string handName);

        /// <summary>
        ///     Attempts to get an item in a hand.
        /// </summary>
        /// <param name="handName">The name of the hand to get.</param>
        /// <param name="item">The item in the held, null if no item is held</param>
        /// <returns>Whether it was holding an item</returns>
        bool TryGetItem(string handName, [NotNullWhen(true)] out ItemComponent? item);

        /// <summary>
        /// Gets item held by the current active hand
        /// </summary>
        ItemComponent? GetActiveHand { get; }

        /// <summary>
        ///     Puts an item into any empty hand, preferring the active hand.
        /// </summary>
        /// <param name="item">The item to put in a hand.</param>
        /// <param name="mobCheck">Whether to perform an ActionBlocker check to the entity.</param>
        /// <returns>True if the item was inserted, false otherwise.</returns>
        bool PutInHand(ItemComponent item, bool mobCheck = true);

        /// <summary>
        ///     Puts an item into a specific hand.
        /// </summary>
        /// <param name="item">The item to put in the hand.</param>
        /// <param name="index">The name of the hand to put the item into.</param>
        /// <param name="fallback">
        ///     If true and the provided hand is full, the method will fall back to <see cref="PutInHand(ItemComponent)" />
        /// <param name="mobCheck">Whether to perform an ActionBlocker check to the entity.</param>
        /// </param>
        /// <returns>True if the item was inserted into a hand, false otherwise.</returns>
        bool PutInHand(ItemComponent item, string index, bool fallback=true, bool mobCheck = true);

        /// <summary>
        ///     Checks to see if an item can be put in any hand.
        /// </summary>
        /// <param name="item">The item to check for.</param>
        /// <param name="mobCheck">Whether to perform an ActionBlocker check to the entity.</param>
        /// <returns>True if the item can be inserted, false otherwise.</returns>
        bool CanPutInHand(ItemComponent item, bool mobCheck = true);

        /// <summary>
        ///     Checks to see if an item can be put in the specified hand.
        /// </summary>
        /// <param name="item">The item to check for.</param>
        /// <param name="index">The name for the hand to check for.</param>
        /// <param name="mobCheck">Whether to perform an ActionBlocker check to the entity.</param>
        /// <returns>True if the item can be inserted, false otherwise.</returns>
        bool CanPutInHand(ItemComponent item, string index, bool mobCheck = true);

        /// <summary>
        ///     Finds the hand slot holding the specified entity, if any.
        /// </summary>
        /// <param name="entity">The entity to look for in our hands.</param>
        /// <param name="handName">
        ///     The name of the hand slot if the entity is indeed held,
        ///     <see langword="null" /> otherwise.
        /// </param>
        /// <returns>
        ///     true if the entity is held, false otherwise
        /// </returns>
        bool TryHand(IEntity entity, [NotNullWhen(true)] out string? handName);

        /// <summary>
        ///     Drops the item contained in the slot to the same position as our entity.
        /// </summary>
        /// <param name="slot">The slot of which to drop to drop the item.</param>
        /// <param name="mobChecks">Whether to check the <see cref="ActionBlockerSystem.CanDrop()"/> for the mob or not.</param>
        /// <param name="doDropInteraction">Whether to perform Dropped interactions.</param>
        /// <returns>True on success, false if something blocked the drop.</returns>
        bool Drop(string slot, bool mobChecks = true, bool doDropInteraction = true);

        /// <summary>
        ///     Drops an item held by one of our hand slots to the same position as our owning entity.
        /// </summary>
        /// <param name="entity">The item to drop.</param>
        /// <param name="mobChecks">Whether to check the <see cref="ActionBlockerSystem.CanDrop()"/> for the mob or not.</param>
        /// <param name="doDropInteraction">Whether to perform Dropped interactions.</param>
        /// <returns>True on success, false if something blocked the drop.</returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown if <see cref="entity"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown if <see cref="entity"/> is not actually held in any hand.
        /// </exception>
        bool Drop(IEntity entity, bool mobChecks = true, bool doDropInteraction = true);

        /// <summary>
        ///     Drops the item in a slot.
        /// </summary>
        /// <param name="slot">The slot to drop the item from.</param>
        /// <param name="coords"></param>
        /// <param name="doMobChecks">Whether to check the <see cref="ActionBlockerSystem.CanDrop()"/> for the mob or not.</param>
        /// <param name="doDropInteraction">Whether to perform Dropped interactions.</param>
        /// <returns>True if an item was dropped, false otherwise.</returns>
        bool Drop(string slot, EntityCoordinates coords, bool doMobChecks = true, bool doDropInteraction = true);

        /// <summary>
        ///     Drop the specified entity in our hands to a certain position.
        /// </summary>
        /// <remarks>
        ///     There are no checks whether or not the user is within interaction range of the drop location
        ///     or whether the drop location is occupied.
        /// </remarks>
        /// <param name="entity">The entity to drop, must be held in one of the hands.</param>
        /// <param name="coords">The coordinates to drop the entity at.</param>
        /// <param name="doMobChecks">Whether to check the <see cref="ActionBlockerSystem.CanDrop()"/> for the mob or not.</param>
        /// <param name="doDropInteraction">Whether to perform Dropped interactions.</param>
        /// <returns>
        ///     True if the drop succeeded,
        ///     false if it failed (due to failing to eject from our hand slot, etc...)
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown if <see cref="entity"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown if <see cref="entity"/> is not actually held in any hand.
        /// </exception>
        bool Drop(IEntity entity, EntityCoordinates coords, bool doMobChecks = true, bool doDropInteraction = true);

        /// <summary>
        ///     Drop the item contained in a slot into another container.
        /// </summary>
        /// <param name="slot">The slot of which to drop the entity.</param>
        /// <param name="targetContainer">The container to drop into.</param>
        /// <param name="doMobChecks">Whether to check the <see cref="ActionBlockerSystem.CanDrop(IEntity)"/> for the mob or not.</param>
        /// <param name="doDropInteraction">Whether to perform Dropped interactions.</param>
        /// <returns>True on success, false if something was blocked (insertion or removal).</returns>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if dry-run checks reported OK to remove and insert,
        ///     but practical remove or insert returned false anyways.
        ///     This is an edge-case that is currently unhandled.
        /// </exception>
        bool Drop(string slot, BaseContainer targetContainer, bool doMobChecks = true, bool doDropInteraction = true);

        /// <summary>
        ///     Drops an item in one of the hands into a container.
        /// </summary>
        /// <param name="entity">The item to drop.</param>
        /// <param name="targetContainer">The container to drop into.</param>
        /// <param name="doMobChecks">Whether to check the <see cref="ActionBlockerSystem.CanDrop()"/> for the mob or not.</param>
        /// <param name="doDropInteraction">Whether to perform Dropped interactions.</param>
        /// <returns>True on success, false if something was blocked (insertion or removal).</returns>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if dry-run checks reported OK to remove and insert,
        ///     but practical remove or insert returned false anyways.
        ///     This is an edge-case that is currently unhandled.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     Thrown if <see cref="entity"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown if <see cref="entity"/> is not actually held in any hand.
        /// </exception>
        bool Drop(IEntity entity, BaseContainer targetContainer, bool doMobChecks = true, bool doDropInteraction = true);

        /// <summary>
        ///     Checks whether the item in the specified hand can be dropped.
        /// </summary>
        /// <param name="name">The hand to check for.</param>
        /// <param name="mobCheck">Whether to perform an ActionBlocker check to the entity.</param>
        /// <returns>
        ///     True if the item can be dropped, false if the hand is empty or the item in the hand cannot be dropped.
        /// </returns>
        bool CanDrop(string name, bool mobCheck = true);

        /// <summary>
        ///     Adds a new hand to this hands component.
        /// </summary>
        /// <param name="name">The name of the hand to add.</param>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if a hand with specified name already exists.
        /// </exception>
        void AddHand(string name);

        /// <summary>
        ///     Removes a hand from this hands component.
        /// </summary>
        /// <remarks>
        ///     If the hand contains an item, the item is dropped.
        /// </remarks>
        /// <param name="name">The name of the hand to remove.</param>
        void RemoveHand(string name);

        /// <summary>
        ///     Checks whether a hand with the specified name exists.
        /// </summary>
        /// <param name="name">The hand name to check.</param>
        /// <returns>True if the hand exists, false otherwise.</returns>
        bool HasHand(string name);

        void HandleSlotModifiedMaybe(ContainerModifiedMessage message);
    }
}
