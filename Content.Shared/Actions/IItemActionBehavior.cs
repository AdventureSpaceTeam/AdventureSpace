﻿using System;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Shared.Actions
{
    /// <summary>
    /// Currently just a marker interface delineating the different possible
    /// types of item action behaviors.
    /// </summary>
    public interface IItemActionBehavior : IExposeData
    {

    }

    /// <summary>
    /// Base class for all item action event args
    /// </summary>
    public abstract class ItemActionEventArgs : EventArgs
    {
        /// <summary>
        /// Entity performing the action.
        /// </summary>
        public readonly IEntity Performer;
        /// <summary>
        /// Item being used to perform the action
        /// </summary>
        public readonly IEntity Item;
        /// <summary>
        /// Action being performed
        /// </summary>
        public readonly ItemActionType ActionType;
        /// <summary>
        /// Item actions component of the item.
        /// </summary>
        public readonly ItemActionsComponent ItemActions;

        public ItemActionEventArgs(IEntity performer, IEntity item, ItemActionType actionType)
        {
            Performer = performer;
            ActionType = actionType;
            Item = item;
            if (!Item.TryGetComponent(out ItemActions))
            {
                throw new InvalidOperationException($"performer {performer.Name} tried to perform item action {actionType} " +
                                                    $" for item {Item.Name} but the item had no ItemActionsComponent," +
                                                    " which should never occur");
            }
        }
    }
}
