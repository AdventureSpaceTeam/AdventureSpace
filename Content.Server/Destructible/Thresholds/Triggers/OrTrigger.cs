﻿#nullable enable
using System;
using System.Collections.Generic;
using Content.Shared.Damage.Components;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Destructible.Thresholds.Triggers
{
    /// <summary>
    ///     A trigger that will activate when any of its triggers have activated.
    /// </summary>
    [Serializable]
    [DataDefinition]
    public class OrTrigger : IThresholdTrigger
    {
        [DataField("triggers")]
        public List<IThresholdTrigger> Triggers { get; } = new();

        public bool Reached(IDamageableComponent damageable, DestructibleSystem system)
        {
            foreach (var trigger in Triggers)
            {
                if (trigger.Reached(damageable, system))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
