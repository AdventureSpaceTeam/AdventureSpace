﻿using System;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds.Behaviors
{
    [Serializable]
    [DataDefinition]
    public class DoActsBehavior : IThresholdBehavior
    {
        /// <summary>
        ///     What acts should be triggered upon activation.
        ///     See <see cref="ActSystem"/>.
        /// </summary>
        [DataField("acts")]
        public ThresholdActs Acts { get; set; }

        public bool HasAct(ThresholdActs act)
        {
            return (Acts & act) != 0;
        }

        public void Execute(IEntity owner, DestructibleSystem system)
        {
            if (HasAct(ThresholdActs.Breakage))
            {
                system.ActSystem.HandleBreakage(owner);
            }

            if (HasAct(ThresholdActs.Destruction))
            {
                system.ActSystem.HandleDestruction(owner);
            }
        }
    }
}
