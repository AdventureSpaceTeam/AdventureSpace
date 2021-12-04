using System;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Shared.Interaction
{
    /// <summary>
    /// This interface gives components behavior when being clicked on by a user with an empty hand
    /// who is in range and has unobstructed reach of the target entity (allows inside blockers).
    /// </summary>
    [RequiresExplicitImplementation]
    public interface IInteractHand
    {
        /// <summary>
        /// Called when a player directly interacts with an empty hand when user is in range of the target entity.
        /// </summary>
        [Obsolete("Use InteractHandEvent instead")]
        bool InteractHand(InteractHandEventArgs eventArgs);
    }

    public class InteractHandEventArgs : EventArgs, ITargetedInteractEventArgs
    {
        public InteractHandEventArgs(EntityUid user, EntityUid target)
        {
            User = user;
            Target = target;
        }

        public EntityUid User { get; }
        public EntityUid Target { get; }
    }

    /// <summary>
    ///     Raised directed on a target entity when it is interacted with by a user with an empty hand.
    /// </summary>
    [PublicAPI]
    public class InteractHandEvent : HandledEntityEventArgs, ITargetedInteractEventArgs
    {
        /// <summary>
        ///     Entity that triggered the interaction.
        /// </summary>
        public EntityUid User { get; }

        /// <summary>
        ///     Entity that was interacted on.
        /// </summary>
        public EntityUid Target { get; }

        public InteractHandEvent(EntityUid user, EntityUid target)
        {
            User = user;
            Target = target;
        }
    }
}
