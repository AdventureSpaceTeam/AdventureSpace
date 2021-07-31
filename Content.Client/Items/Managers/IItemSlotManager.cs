﻿using System;
using Content.Client.Items.UI;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;

namespace Content.Client.Items.Managers
{
    public interface IItemSlotManager
    {
        bool OnButtonPressed(GUIBoundKeyEventArgs args, IEntity? item);
        void UpdateCooldown(ItemSlotButton? cooldownTexture, IEntity? entity);
        bool SetItemSlot(ItemSlotButton button, IEntity? entity);
        void HoverInSlot(ItemSlotButton button, IEntity? entity, bool fits);
        event Action<EntitySlotHighlightedEventArgs>? EntityHighlightedUpdated;
        bool IsHighlighted(EntityUid uid);

        /// <summary>
        /// Highlight all slot controls that contain the specified entity.
        /// </summary>
        /// <param name="uid">The UID of the entity to highlight.</param>
        /// <seealso cref="UnHighlightEntity"/>
        void HighlightEntity(EntityUid uid);

        /// <summary>
        /// Remove highlighting for the specified entity.
        /// </summary>
        /// <param name="uid">The UID of the entity to unhighlight.</param>
        /// <seealso cref="HighlightEntity"/>
        void UnHighlightEntity(EntityUid uid);
    }
}
