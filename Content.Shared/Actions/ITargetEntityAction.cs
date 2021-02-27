﻿#nullable enable
using Robust.Shared.GameObjects;

namespace Content.Shared.Actions
{
    /// <summary>
    /// Action which is used on a targeted entity.
    /// </summary>
    public interface ITargetEntityAction : IActionBehavior
    {
        /// <summary>
        /// Invoked when the target entity action should be performed.
        /// Implementation should perform the server side logic of the action.
        /// </summary>
        void DoTargetEntityAction(TargetEntityActionEventArgs args);
    }

    public class TargetEntityActionEventArgs : ActionEventArgs
    {
        /// <summary>
        /// Entity being targeted
        /// </summary>
        public readonly IEntity Target;

        public TargetEntityActionEventArgs(IEntity performer, ActionType actionType, IEntity target) :
            base(performer, actionType)
        {
            Target = target;
        }
    }
}
