﻿#nullable enable
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds.Triggers
{
    public interface IThresholdTrigger : IExposeData
    {
        /// <summary>
        ///     Checks if this trigger has been reached.
        /// </summary>
        /// <param name="damageable">The damageable component to check with.</param>
        /// <param name="system">
        ///     An instance of <see cref="DestructibleSystem"/> to pull
        ///     dependencies from, if any.
        /// </param>
        /// <returns>true if this trigger has been reached, false otherwise.</returns>
        bool Reached(IDamageableComponent damageable, DestructibleSystem system);
    }
}
