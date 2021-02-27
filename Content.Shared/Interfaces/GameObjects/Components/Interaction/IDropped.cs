﻿#nullable enable
using System;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Shared.Interfaces.GameObjects.Components
{
    /// <summary>
    ///     This interface gives components behavior when they're dropped by a mob.
    /// </summary>
    [RequiresExplicitImplementation]
    public interface IDropped
    {
        void Dropped(DroppedEventArgs eventArgs);
    }

    public class DroppedEventArgs : EventArgs
    {
        public DroppedEventArgs(IEntity user, bool intentional)
        {
            User = user;
            Intentional = intentional;
        }

        public IEntity User { get; }

        public bool Intentional { get; }
    }

    /// <summary>
    ///     Raised when an entity is dropped
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

        /// <summary>
        ///     If the item was dropped intentionally.
        /// </summary>
        public bool Intentional { get; }

        public DroppedMessage(IEntity user, IEntity dropped, bool intentional)
        {
            User = user;
            Dropped = dropped;
            Intentional = intentional;
        }
    }
}
