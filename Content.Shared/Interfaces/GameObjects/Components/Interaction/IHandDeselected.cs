﻿using System;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.Interfaces.GameObjects.Components
{
    /// <summary>
    ///     This interface gives components behavior when they're held on a deselected hand.
    /// </summary>
    [RequiresExplicitImplementation]
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
}
